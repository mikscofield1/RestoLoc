using System.Text.RegularExpressions;
using System.Net;

namespace RestoLoc;

public static class Calculs
{
    // ... Gardez votre fonction CalculerDistance existante ...

    public static Restaurant? AnalyserUrlGoogleMaps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var resto = new Restaurant
        {
            Type = TypeRestaurant.Physique,
            EstFiable = true // Fiable par défaut comme demandé
        };

        // 1. Extraction des coordonnées GPS (@lat,lon)
        var matchCoords = Regex.Match(url, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
        if (matchCoords.Success && matchCoords.Groups.Count == 3)
        {
            if (double.TryParse(matchCoords.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(matchCoords.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                resto.Latitude = lat;
                resto.Longitude = lon;
            }
        }

        // 2. Extraction du Nom depuis l'URL (ex: maps/place/Nom+Du+Restaurant/...)
        var matchNom = Regex.Match(url, @"/place/([^/]+)/");
        if (matchNom.Success)
        {
            string nomBrut = matchNom.Groups[1].Value;
            // Décode les caractères spéciaux (ex: %20 ou + devenant des espaces)
            string nomNettoye = WebUtility.UrlDecode(nomBrut).Replace("+", " ");
            
            // Si l'URL contient aussi les coordonnées après le nom, on les nettoie
            if (nomNettoye.Contains(","))
            {
                nomNettoye = nomNettoye.Split(',')[0];
            }
            
            resto.Nom = nomNettoye;
        }

        // Note: Le numéro de téléphone n'est presque jamais présent dans l'URL de partage Google Maps.
        // Il restera vide à compléter manuellement si besoin.

        return resto.Latitude != 0 ? resto : null;
    }
}