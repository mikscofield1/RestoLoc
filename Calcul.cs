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

        // Stocker le lien Google Maps dans LienCommande
        resto.LienCommande = url.Trim();

        // Extraction du Nom depuis l'URL (ex: maps/place/Nom+Du+Restaurant/...)
        var matchNom = Regex.Match(url, @"/place/([^/]+)/");
        if (matchNom.Success)
        {
            string nomBrut = matchNom.Groups[1].Value;
            string nomNettoye = WebUtility.UrlDecode(nomBrut).Replace("+", " ");
            if (nomNettoye.Contains(","))
            {
                nomNettoye = nomNettoye.Split(',')[0];
            }
            resto.Nom = nomNettoye;
        }

        // Si l'URL est bien formée, on retourne l'objet même si le nom doit être saisi manuellement.
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return resto;
        }

        return null;
    }
}