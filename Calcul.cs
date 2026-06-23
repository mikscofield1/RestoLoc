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
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);

            var current = inputUrl;
            for (int i = 0; i < 8; i++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, current);
                // Do not download the whole body, only headers are sufficient to get Location
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

                // If no redirect status, try to inspect headers for final URI
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

        var resto = new Restaurant
        {
            Type = TypeRestaurant.Physique,
            EstFiable = true,
            LienCommande = trimmedUrl
        };

        // Try extract name
        var nomExtraite = ExtraireNomDepuisGoogleMapsUrl(trimmedUrl);
        if (!string.IsNullOrEmpty(nomExtraite))
        {
            resto.Nom = nomExtraite;
        }

        var decoded = WebUtility.UrlDecode(trimmedUrl);

        // Try extract city from name or query
        string? ville = null;
        if (!string.IsNullOrEmpty(resto.Nom) && resto.Nom.Contains(","))
        {
            var parts = resto.Nom.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (parts.Length >= 2)
            {
                ville = parts.Last();
                resto.Nom = parts[0];
            }
        }

        if (string.IsNullOrEmpty(ville))
        {
            var mQ = Regex.Match(decoded, "[?&](?:q|query)=([^&]+)");
            if (mQ.Success)
            {
                var q = NettoyerNomGoogleMaps(mQ.Groups[1].Value);
                if (q.Contains(","))
                {
                    var p = q.Split(',').Select(s => s.Trim()).ToArray();
                    if (p.Length >= 2)
                    {
                        ville = p.Last();
                        if (string.IsNullOrEmpty(resto.Nom)) resto.Nom = p[0];
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(ville)) resto.Ville = ville;

        return new GoogleMapsAnalysisResult
        {
            Resto = resto,
            RawUrl = trimmedUrl
        };
    }

    private static string? ExtraireNomDepuisGoogleMapsUrl(string url)
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