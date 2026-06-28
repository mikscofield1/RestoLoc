using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleAnalyzer;

public class Restaurant
{
    public string Nom { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public bool EstFiable { get; set; } = true;
}

public static class Calculs
{
    private static readonly HttpClient _mapsRedirectClient;
    static Calculs()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        _mapsRedirectClient = new HttpClient(handler);
        _mapsRedirectClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
    }

    public class GoogleMapsAnalysisResult
    {
        public Restaurant Resto { get; set; } = new Restaurant();
        public bool IsConfident => !string.IsNullOrEmpty(Resto.Ville);
        public string? RawUrl { get; set; }
    }

    public static async Task<string> ResolveGoogleMapsShortUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var inputUrl = url.Trim();
        if (!inputUrl.Contains("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase) &&
            !inputUrl.Contains("goo.gl/maps", StringComparison.OrdinalIgnoreCase))
        {
            return inputUrl;
        }
        try
        {
            var current = inputUrl;
            for (int i = 0; i < 8; i++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, current);
                req.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                using var resp = await _mapsRedirectClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (resp.StatusCode == HttpStatusCode.MovedPermanently || resp.StatusCode == HttpStatusCode.Found || resp.StatusCode == HttpStatusCode.SeeOther || resp.StatusCode == HttpStatusCode.TemporaryRedirect || (int)resp.StatusCode == 308)
                {
                    var loc = resp.Headers.Location;
                    if (loc == null) break;
                    var baseUri = new Uri(current);
                    current = loc.IsAbsoluteUri ? loc.ToString() : new Uri(baseUri, loc).ToString();
                    continue;
                }
                if (resp.RequestMessage?.RequestUri != null)
                {
                    return resp.RequestMessage.RequestUri.ToString();
                }
                break;
            }
            return current;
        }
        catch
        {
            return inputUrl;
        }
    }

    public static Task<GoogleMapsAnalysisResult?> AnalyserUrlGoogleMapsAsync(string url)
    {
        return Task.FromResult(AnalyserUrlGoogleMaps(url));
    }

    public static GoogleMapsAnalysisResult? AnalyserUrlGoogleMaps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmedUrl = url.Trim();
        var decodedUrl = WebUtility.UrlDecode(trimmedUrl);
        var normalizedUrl = decodedUrl.Replace("https://www.", "https://").Replace("http://www.", "http://");
        var resto = new Restaurant { EstFiable = true };
        var nomExtraite = ExtraireNomDepuisGoogleMapsUrl(normalizedUrl);
        if (string.IsNullOrEmpty(nomExtraite))
        {
            var fallbackPlace = Regex.Match(normalizedUrl, @"/place/([^/]+)/@", RegexOptions.IgnoreCase);
            if (fallbackPlace.Success) nomExtraite = NettoyerNomGoogleMaps(fallbackPlace.Groups[1].Value);
        }
        if (!string.IsNullOrEmpty(nomExtraite)) resto.Nom = nomExtraite;
        var ville = ExtractCityFromGoogleMapsUrl(normalizedUrl);
        if (!string.IsNullOrEmpty(ville)) resto.Ville = ville;
        return new GoogleMapsAnalysisResult { Resto = resto, RawUrl = trimmedUrl };
    }

    private static string ExtractCityFromGoogleMapsUrl(string decodedUrl)
    {
        if (string.IsNullOrEmpty(decodedUrl)) return string.Empty;
        var regexPlaceCity = new Regex(@"/place/[^/]+,\s*([^/?]+)", RegexOptions.IgnoreCase);
        var matchPlaceCity = regexPlaceCity.Match(decodedUrl);
        if (matchPlaceCity.Success && matchPlaceCity.Groups.Count > 1) return matchPlaceCity.Groups[1].Value.Replace("+", " ").Trim();
        var regexQueryCity = new Regex(@"[?&](query|q)=[^,]+,\s*([^&]+)", RegexOptions.IgnoreCase);
        var matchQueryCity = regexQueryCity.Match(decodedUrl);
        if (matchQueryCity.Success && matchQueryCity.Groups.Count > 2) return matchQueryCity.Groups[2].Value.Replace("+", " ").Trim();
        var regexDirCity = new Regex(@"/dir/[^/]+/([^/]+,([^/?]+))", RegexOptions.IgnoreCase);
        var matchDirCity = regexDirCity.Match(decodedUrl);
        if (matchDirCity.Success && matchDirCity.Groups.Count > 2) return matchDirCity.Groups[2].Value.Replace("+", " ").Trim();
        var mQ = Regex.Match(decodedUrl, "[?&](?:q|query)=([^&]+)");
        if (mQ.Success)
        {
            var q = NettoyerNomGoogleMaps(mQ.Groups[1].Value);
            if (q.Contains(","))
            {
                var p = q.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (p.Length >= 2) return p.Last();
            }
        }
        return string.Empty;
    }

    private static string? ExtraireNomDepuisGoogleMapsUrl(string decodedUrl)
    {
        if (string.IsNullOrWhiteSpace(decodedUrl)) return null;
        var matchPlace = Regex.Match(decodedUrl, @"/place/([^/]+)");
        if (matchPlace.Success) return NettoyerNomGoogleMaps(matchPlace.Groups[1].Value);
        var matchSearch = Regex.Match(decodedUrl, @"/search/([^/]+)/");
        if (matchSearch.Success) return NettoyerNomGoogleMaps(matchSearch.Groups[1].Value);
        var matchQuery = Regex.Match(decodedUrl, @"[?&](?:q|query)=([^&]+)");
        if (matchQuery.Success) return NettoyerNomGoogleMaps(matchQuery.Groups[1].Value);
        matchSearch = Regex.Match(decodedUrl, @"/maps/search/([^/?]+)");
        if (matchSearch.Success) return NettoyerNomGoogleMaps(matchSearch.Groups[1].Value);
        return null;
    }

    private static string NettoyerNomGoogleMaps(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
        var nomNettoye = rawName.Replace('+', ' ').Trim();
        if (nomNettoye.Contains(",")) nomNettoye = nomNettoye.Split(',')[0].Trim();
        return nomNettoye;
    }
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        var input = args.Length > 0 ? args[0] : "https://maps.app.goo.gl/ZfEACc2qV8MVeBBc8";
        Console.WriteLine("Input: " + input);
        var longUrl = await Calculs.ResolveGoogleMapsShortUrlAsync(input);
        Console.WriteLine("Resolved: " + longUrl);
        var res = await Calculs.AnalyserUrlGoogleMapsAsync(longUrl);
        if (res == null) { Console.WriteLine("Analysis null"); return 1; }
        Console.WriteLine("Nom: " + (res.Resto.Nom ?? "<null>"));
        Console.WriteLine("Ville: " + (res.Resto.Ville ?? "<null>"));
        return 0;
    }
}
