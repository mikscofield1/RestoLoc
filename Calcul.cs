using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Globalization;

// For reverse geocoding HTTP calls
using System.Threading.Tasks;

namespace RestoLoc;

public static class Calculs
{
    // Instance unique pour éviter la saturation des sockets réseau
    private static readonly HttpClient _mapsRedirectClient;
    // Client Http général pour les appels API (Nominatim)
    private static readonly HttpClient _httpClient;

    static Calculs()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        _mapsRedirectClient = new HttpClient(handler);
        _mapsRedirectClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

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

                if (resp.StatusCode == HttpStatusCode.MovedPermanently ||
                    resp.StatusCode == HttpStatusCode.Found ||
                    resp.StatusCode == HttpStatusCode.SeeOther ||
                    resp.StatusCode == HttpStatusCode.TemporaryRedirect ||
                    (int)resp.StatusCode == 308)
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

    public class GoogleMapsAnalysisResult
    {
        public Restaurant Resto { get; set; } = new Restaurant();
        public bool IsConfident => !string.IsNullOrEmpty(Resto.Ville);
        public string? RawUrl { get; set; }
        // When true the UI should ask the user to confirm the suggested city
        public bool NeedsConfirmation { get; set; }
        // If available, a suggested city coming from reverse geocoding
        public string? SuggestedCity { get; set; }
    }

    public static GoogleMapsAnalysisResult? AnalyserUrlGoogleMaps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var trimmedUrl = url.Trim();
        // On décode UNIQUEMENT ici de manière centrale
        var decodedUrl = WebUtility.UrlDecode(trimmedUrl);
        var normalizedUrl = decodedUrl.Replace("https://www.", "https://").Replace("http://www.", "http://");

        var resto = new Restaurant
        {
            Type = TypeRestaurant.Physique,
            EstFiable = true,
            LienCommande = trimmedUrl
        };

        // Extraction du nom
        var nomExtraite = ExtraireNomDepuisGoogleMapsUrl(normalizedUrl);

        if (string.IsNullOrEmpty(nomExtraite))
        {
            // Fallback /place/<name>/@...
            var fallbackPlace = Regex.Match(normalizedUrl, @"/place/([^/]+)/@", RegexOptions.IgnoreCase);
            if (fallbackPlace.Success)
                nomExtraite = NettoyerNomGoogleMaps(fallbackPlace.Groups[1].Value);
        }

        if (!string.IsNullOrEmpty(nomExtraite))
        {
            resto.Nom = nomExtraite;
        }

        // Extraction de la ville
        var ville = ExtractCityFromGoogleMapsUrl(normalizedUrl);
        if (!string.IsNullOrEmpty(ville))
        {
            resto.Ville = ville;
        }

        return new GoogleMapsAnalysisResult
        {
            Resto = resto,
            RawUrl = trimmedUrl
        };
    }

    // Real async analyser: resolves coordinates, may call reverse geocoding
    public static async Task<GoogleMapsAnalysisResult?> AnalyserUrlGoogleMapsAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var trimmedUrl = url.Trim();
        var decodedUrl = WebUtility.UrlDecode(trimmedUrl);
        var normalizedUrl = decodedUrl.Replace("https://www.", "https://").Replace("http://www.", "http://");

        var resto = new Restaurant
        {
            Type = TypeRestaurant.Physique,
            EstFiable = true,
            LienCommande = trimmedUrl
        };

        // Extraction du nom
        var nomExtraite = ExtraireNomDepuisGoogleMapsUrl(normalizedUrl);
        if (string.IsNullOrEmpty(nomExtraite))
        {
            var fallbackPlace = Regex.Match(normalizedUrl, @"/place/([^/]+)/@", RegexOptions.IgnoreCase);
            if (fallbackPlace.Success)
                nomExtraite = NettoyerNomGoogleMaps(fallbackPlace.Groups[1].Value);
        }

        if (!string.IsNullOrEmpty(nomExtraite))
            resto.Nom = nomExtraite;

        // Extraction des coordonnées GPS si présentes
        var coords = ExtraireCoordonneesGps(normalizedUrl);

        string? cityFromUrl = ExtractCityFromGoogleMapsUrl(normalizedUrl);
        string? cityFromGeo = null;
        bool geoConfident = false;

        if (coords != null)
        {
            try
            {
                var geo = await ObtenirVilleParGpsAsync(coords.Value.lat, coords.Value.lon).ConfigureAwait(false);
                cityFromGeo = geo.city;
                geoConfident = geo.confident;
            }
            catch
            {
                cityFromGeo = null;
                geoConfident = false;
            }
        }

        var result = new GoogleMapsAnalysisResult
        {
            Resto = resto,
            RawUrl = trimmedUrl,
            NeedsConfirmation = false,
            SuggestedCity = null
        };

        if (!string.IsNullOrEmpty(cityFromUrl) && !string.IsNullOrEmpty(cityFromGeo))
        {
            // Both present: if they differ, suggest the geocoded city and ask confirmation
            if (!string.Equals(cityFromUrl, cityFromGeo, StringComparison.OrdinalIgnoreCase))
            {
                result.Resto.Ville = cityFromUrl;
                result.SuggestedCity = cityFromGeo;
                result.NeedsConfirmation = true;
            }
            else
            {
                result.Resto.Ville = cityFromUrl;
                result.NeedsConfirmation = false;
            }
        }
        else if (!string.IsNullOrEmpty(cityFromGeo))
        {
                result.Resto.Ville = cityFromGeo?.Trim();
                result.NeedsConfirmation = !geoConfident;
                if (!geoConfident)
                    result.SuggestedCity = cityFromGeo?.Trim();
        }
        else if (!string.IsNullOrEmpty(cityFromUrl))
        {
            result.Resto.Ville = cityFromUrl;
            result.NeedsConfirmation = false;
        }

        return result;
    }

    private static (double lat, double lon)? ExtraireCoordonneesGps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // Pattern précis !3d<lat>!4d<lon>
        var precise = Regex.Match(url, "!3d(?<lat>-?\\d+\\.\\d+)!4d(?<lon>-?\\d+\\.\\d+)", RegexOptions.Compiled);
        if (precise.Success)
        {
            if (double.TryParse(precise.Groups["lat"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(precise.Groups["lon"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }
        }

        // Fallback: rechercher manuellement les marqueurs !3d et !4d si le regex n'a pas fonctionné
        var idx3 = url.IndexOf("!3d", StringComparison.OrdinalIgnoreCase);
        var idx4 = url.IndexOf("!4d", StringComparison.OrdinalIgnoreCase);
        if (idx3 >= 0 && idx4 > idx3)
        {
            try
            {
                var sub3 = url.Substring(idx3 + 3);
                var sub4 = url.Substring(idx4 + 3);
                var mLat = Regex.Match(sub3, "^-?\\d+\\.?\\d+");
                var mLon = Regex.Match(sub4, "^-?\\d+\\.?\\d+");
                if (mLat.Success && mLon.Success)
                {
                    if (double.TryParse(mLat.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                        double.TryParse(mLon.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                    {
                        return (lat, lon);
                    }
                }
            }
            catch
            {
                // ignore and continue
            }
        }

        // Pattern classique @lat,lon
        var m = Regex.Match(url, "@(?<lat>-?\\d+\\.\\d+),(?<lon>-?\\d+\\.\\d+)", RegexOptions.Compiled);
        if (m.Success)
        {
            if (double.TryParse(m.Groups["lat"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(m.Groups["lon"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }
        }

        return null;
    }

    // Calls Nominatim reverse geocoding and returns (city, confident)
    private static async Task<(string? city, bool confident)> ObtenirVilleParGpsAsync(double lat, double lon)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}&accept-language=fr";
            using var resp = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return (null, false);

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("address", out var addr)) return (null, false);

            string? city = null;
            bool confident = false;
            if (addr.TryGetProperty("city", out var v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
                confident = true;
            }
            else if (addr.TryGetProperty("town", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
                confident = true;
            }
            else if (addr.TryGetProperty("village", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
                confident = true;
            }
            else if (addr.TryGetProperty("hamlet", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
                confident = true;
            }
            else if (addr.TryGetProperty("county", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
                confident = true;
            }
            else if (addr.TryGetProperty("suburb", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
            }
            else if (addr.TryGetProperty("neighbourhood", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
            }
            else if (addr.TryGetProperty("province", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
            }
            else if (addr.TryGetProperty("state_district", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
            }
            else if (addr.TryGetProperty("state", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
            }
            else if (addr.TryGetProperty("country", out v) && v.ValueKind == JsonValueKind.String)
            {
                city = v.GetString();
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                if (doc.RootElement.TryGetProperty("display_name", out var displayName) && displayName.ValueKind == JsonValueKind.String)
                {
                    var fallback = displayName.GetString();
                    if (!string.IsNullOrWhiteSpace(fallback))
                    {
                        var parts = fallback.Split(',').Select(part => part.Trim()).Where(part => !string.IsNullOrEmpty(part)).ToArray();
                        if (parts.Length >= 2)
                        {
                            city = parts[^2];
                        }
                        else if (parts.Length == 1)
                        {
                            city = parts[0];
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(city)) return (null, false);

            return (city, confident);
        }
        catch
        {
            return (null, false);
        }
    }

    private static string ExtractCityFromGoogleMapsUrl(string decodedUrl)
    {
        if (string.IsNullOrEmpty(decodedUrl)) return string.Empty;

        // --- CAS 1 : Format classique "/place/... , City" ---
        var regexPlaceCity = new Regex(@"/place/[^/]+,\s*([^/?]+)", RegexOptions.IgnoreCase);
        var matchPlaceCity = regexPlaceCity.Match(decodedUrl);
        if (matchPlaceCity.Success && matchPlaceCity.Groups.Count > 1)
        {
            return matchPlaceCity.Groups[1].Value.Replace("+", " ").Trim();
        }

        // --- CAS 2 : Format Paramètre "query=" ou "q=" ---
        var regexQueryCity = new Regex(@"[?&](query|q)=[^,]+,\s*([^&]+)", RegexOptions.IgnoreCase);
        var matchQueryCity = regexQueryCity.Match(decodedUrl);
        if (matchQueryCity.Success && matchQueryCity.Groups.Count > 2)
        {
            return matchQueryCity.Groups[2].Value.Replace("+", " ").Trim();
        }

        // --- CAS 3 : Format Itinéraire "/dir/.../Destination,+Ville" ---
        var regexDirCity = new Regex(@"/dir/[^/]+/([^/]+,([^/?]+))", RegexOptions.IgnoreCase);
        var matchDirCity = regexDirCity.Match(decodedUrl);
        if (matchDirCity.Success && matchDirCity.Groups.Count > 2)
        {
            return matchDirCity.Groups[2].Value.Replace("+", " ").Trim();
        }

        // --- CAS 4 : Fallback Query String avec virgules ---
        var mQ = Regex.Match(decodedUrl, "[?&](?:q|query)=([^&]+)");
        if (mQ.Success)
        {
            var q = NettoyerNomGoogleMaps(mQ.Groups[1].Value);
            if (q.Contains(","))
            {
                var p = q.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (p.Length >= 2)
                {
                    // Si le dernier élément est un pays (ex: France), prendre l'avant-dernier pour la ville
                    return p.Length > 2 ? p[p.Length - 2] : p.Last();
                }
            }
        }

        return string.Empty;
    }

    private static string? ExtraireNomDepuisGoogleMapsUrl(string decodedUrl)
    {
        if (string.IsNullOrWhiteSpace(decodedUrl)) return null;

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
