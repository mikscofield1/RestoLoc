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
            using var request = new HttpRequestMessage(HttpMethod.Get, inputUrl);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var finalUri = response.RequestMessage?.RequestUri;
            if (finalUri != null)
            {
                return finalUri.ToString();
            }
        }
        catch
        {
            // On ignore les erreurs de résolution et on retourne l'URL originale.
        }

        return inputUrl;
    }

    public static Restaurant? AnalyserUrlGoogleMaps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var resto = new Restaurant
        {
            Type = TypeRestaurant.Physique,
            EstFiable = true // Fiable par défaut comme demandé
        };

        var trimmedUrl = url.Trim();
        resto.LienCommande = trimmedUrl;

        var nomExtraite = ExtraireNomDepuisGoogleMapsUrl(trimmedUrl);
        if (!string.IsNullOrEmpty(nomExtraite))
        {
            resto.Nom = nomExtraite;
        }

        // Si l'URL est bien formée, on retourne l'objet même si le nom doit être saisi manuellement.
        if (Uri.IsWellFormedUriString(trimmedUrl, UriKind.Absolute))
        {
            return resto;
        }

        return null;
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