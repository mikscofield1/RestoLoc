using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;

namespace RestoLoc;

public static class Calculs
{
    // ... Gardez votre fonction CalculerDistance existante ...

    public static async Task<string> ResolveGoogleMapsShortUrlAsync(HttpClient httpClient, string url)
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
            // Some injected HttpClient handlers may have different redirect settings.
            // Create a temporary HttpClientHandler to manually follow redirects and obtain the final URL.
            using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var current = inputUrl;
            for (int i = 0; i < 8; i++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, current);
                req.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                // If response has a Location header, follow it
                if (resp.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    resp.StatusCode == System.Net.HttpStatusCode.Found ||
                    resp.StatusCode == System.Net.HttpStatusCode.SeeOther ||
                    resp.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect ||
                    (int)resp.StatusCode == 308) // Permanent Redirect
                {
                    var loc = resp.Headers.Location;
                    if (loc == null) break;

                    // Build absolute uri
                    var baseUri = new Uri(current);
                    var next = loc.IsAbsoluteUri ? loc.ToString() : new Uri(baseUri, loc).ToString();
                    current = next;
                    continue;
                }

                // If no redirect status, return the final request URI if available.
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
            // Ignore resolution errors and return the original input URL
            return inputUrl;
        }
    }

    public class GoogleMapsAnalysisResult
    {
        public Restaurant Resto { get; set; } = new Restaurant();
        public bool IsConfident => !string.IsNullOrEmpty(Resto.Ville);
        public string? RawUrl { get; set; }
    }

    

    public static GoogleMapsAnalysisResult? AnalyserUrlGoogleMaps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var trimmedUrl = url.Trim();
        var decoded = WebUtility.UrlDecode(trimmedUrl);
        var normalizedUrl = decoded.Replace("https://www.", "https://").Replace("http://www.", "http://");

        var resto = new Restaurant
        {
            Type = TypeRestaurant.Physique,
            EstFiable = true,
            LienCommande = trimmedUrl
        };

        // Try extract name from the URL.
        var nomExtraite = ExtraireNomDepuisGoogleMapsUrl(normalizedUrl);
        if (string.IsNullOrEmpty(nomExtraite))
        {
            nomExtraite = ExtraireNomDepuisGoogleMapsUrl(decoded);
        }

        if (string.IsNullOrEmpty(nomExtraite))
        {
            // Fallback for URLs with /place/<name>/@...
            var fallbackPlace = Regex.Match(normalizedUrl, @"/place/([^/]+)/@", RegexOptions.IgnoreCase);
            if (fallbackPlace.Success)
                nomExtraite = NettoyerNomGoogleMaps(fallbackPlace.Groups[1].Value);
        }

        if (!string.IsNullOrEmpty(nomExtraite))
        {
            resto.Nom = nomExtraite;
        }

        var ville = ExtractCityFromGoogleMapsUrl(normalizedUrl);
        if (!string.IsNullOrEmpty(ville)) resto.Ville = ville;

        return new GoogleMapsAnalysisResult
        {
            Resto = resto,
            RawUrl = trimmedUrl
        };
    }

    private static string ExtractCityFromGoogleMapsUrl(string decodedUrl)
    {
        if (string.IsNullOrEmpty(decodedUrl)) return string.Empty;

        // --- CAS 1 : Format classique "/place/... , City" ---
        var regexPlaceCity = new Regex(@"/place/[^/]+,\s*([^/?]+)", RegexOptions.IgnoreCase);
        var matchPlaceCity = regexPlaceCity.Match(decodedUrl);
        if (matchPlaceCity.Success && matchPlaceCity.Groups.Count > 1)
        {
            return (matchPlaceCity.Groups[1].Value ?? string.Empty).Replace("+", " ").Trim();
        }

        // --- CAS 2 : Format Paramètre "query=" ou "q=" ---
        var regexQueryCity = new Regex(@"[?&](query|q)=[^,]+,\s*([^&]+)", RegexOptions.IgnoreCase);
        var matchQueryCity = regexQueryCity.Match(decodedUrl);
        if (matchQueryCity.Success && matchQueryCity.Groups.Count > 2)
        {
            return (matchQueryCity.Groups[2].Value ?? string.Empty).Replace("+", " ").Trim();
        }

        // --- CAS 3 : Format Itinéraire "/dir/.../Destination,+Ville" ---
        var regexDirCity = new Regex(@"/dir/[^/]+/([^/]+,([^/?]+))", RegexOptions.IgnoreCase);
        var matchDirCity = regexDirCity.Match(decodedUrl);
        if (matchDirCity.Success && matchDirCity.Groups.Count > 2)
        {
            return (matchDirCity.Groups[2].Value ?? string.Empty).Replace("+", " ").Trim();
        }

        // --- CAS 4: Try query string fallback if it contains a comma after q/query.
        var mQ = Regex.Match(decodedUrl, "[?&](?:q|query)=([^&]+)");
        if (mQ.Success)
        {
            var q = NettoyerNomGoogleMaps(mQ.Groups[1].Value);
            if (q.Contains(","))
            {
                var p = q.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (p.Length >= 2)
                {
                    return p.Last();
                }
            }
        }

        return string.Empty;
    }

    private static string ExtraireNomDepuisGoogleMapsUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        string decodedUrl = WebUtility.UrlDecode(url);

        // 1. /place/nom+
        var matchPlace = Regex.Match(decodedUrl, @"/place/([^/]+)");
        if (matchPlace.Success)
        {
            return NettoyerNomGoogleMaps(matchPlace.Groups[1].Value);
        }

        // 2. /search/nom+
        var matchSearch = Regex.Match(decodedUrl, @"/search/([^/]+)/");
        if (matchSearch.Success)
        {
            return NettoyerNomGoogleMaps(matchSearch.Groups[1].Value);
        }

        // 3. query parameters q= or query=
        var matchQuery = Regex.Match(decodedUrl, @"[?&](?:q|query)=([^&]+)");
        if (matchQuery.Success)
        {
            return NettoyerNomGoogleMaps(matchQuery.Groups[1].Value);
        }

        // 4. /maps/search/nom+ without trailing slash
        matchSearch = Regex.Match(decodedUrl, @"/maps/search/([^/?]+)");
        if (matchSearch.Success)
        {
            return NettoyerNomGoogleMaps(matchSearch.Groups[1].Value);
        }

        return null;
    }

    private static string NettoyerNomGoogleMaps(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        var nomNettoye = rawName.Replace('+', ' ').Trim();
        if (nomNettoye.Contains(","))
        {
            nomNettoye = nomNettoye.Split(',')[0].Trim();
        }

        return nomNettoye;
    }
    
}