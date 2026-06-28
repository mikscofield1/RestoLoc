using System;
using System.Reflection;
using System.Threading.Tasks;
using RestoLoc;

class Program
{
    static async Task Main()
    {
        var url = "https://maps.app.goo.gl/ZfEACc2qV8MVeBBc8";
        var resolved = await Calculs.ResolveGoogleMapsShortUrlAsync(url);
        Console.WriteLine("Resolved: " + resolved);

        var type = typeof(Calculs);
        var coordsMethod = type.GetMethod("ExtraireCoordonneesGps", BindingFlags.NonPublic | BindingFlags.Static);
        var geoMethod = type.GetMethod("ObtenirVilleParGpsAsync", BindingFlags.NonPublic | BindingFlags.Static);
        if (coordsMethod == null || geoMethod == null)
        {
            Console.WriteLine("Private methods not found.");
            return;
        }

        var coords = coordsMethod.Invoke(null, new object[] { resolved });
        Console.WriteLine("Coords: " + (coords?.ToString() ?? "null"));

        if (coords != null)
        {
            Console.WriteLine("  Type: " + coords.GetType().FullName);
            Console.WriteLine("  Fields:");
            foreach (var field in coords.GetType().GetFields())
            {
                Console.WriteLine($"    Field {field.Name} = {field.GetValue(coords)}");
            }
            Console.WriteLine("  Properties:");
            foreach (var prop in coords.GetType().GetProperties())
            {
                Console.WriteLine($"    Prop {prop.Name} = {prop.GetValue(coords)}");
            }

            var latValue = coords.GetType().GetField("lat")?.GetValue(coords) ?? coords.GetType().GetField("Item1")?.GetValue(coords);
            var lonValue = coords.GetType().GetField("lon")?.GetValue(coords) ?? coords.GetType().GetField("Item2")?.GetValue(coords);
            Console.WriteLine($"lat={latValue} lon={lonValue}");

            var task = (Task)geoMethod.Invoke(null, new object[] { latValue, lonValue });
            await task.ConfigureAwait(false);
            var resultProp = task.GetType().GetProperty("Result");
            var geoResult = resultProp?.GetValue(task);
            if (geoResult != null)
            {
                Console.WriteLine("Geo result type: " + geoResult.GetType().FullName);
                foreach (var field in geoResult.GetType().GetFields())
                {
                    Console.WriteLine($"  field {field.Name} = {field.GetValue(geoResult)}");
                }
                foreach (var prop in geoResult.GetType().GetProperties())
                {
                    Console.WriteLine($"  prop {prop.Name} = {prop.GetValue(geoResult)}");
                }
            }
        }

        var analysis = await Calculs.AnalyserUrlGoogleMapsAsync(resolved);
        Console.WriteLine("Analysis:");
        Console.WriteLine("  Nom: " + analysis?.Resto.Nom);
        Console.WriteLine("  Ville: " + analysis?.Resto.Ville);
        Console.WriteLine("  SuggestedCity: " + analysis?.SuggestedCity);
        Console.WriteLine("  NeedsConfirmation: " + analysis?.NeedsConfirmation);
    }
}
