using System;
using System.Threading.Tasks;
using RestoLoc;

class Program
{
    static async Task<int> Main(string[] args)
    {
        string input = args.Length > 0 ? args[0] : "https://maps.app.goo.gl/ZfEACc2qV8MVeBBc8";
        Console.WriteLine("Input: " + input);
        var longUrl = await Calculs.ResolveGoogleMapsShortUrlAsync(input);
        Console.WriteLine("Resolved: " + longUrl);
        var result = await Calculs.AnalyserUrlGoogleMapsAsync(longUrl);
        if (result == null)
        {
            Console.WriteLine("Result: null");
            return 1;
        }
        Console.WriteLine("--- Analysis ---");
        Console.WriteLine("Nom: " + (result.Resto.Nom ?? "<null>"));
        Console.WriteLine("Ville: " + (result.Resto.Ville ?? "<null>"));
        Console.WriteLine("EstFiable: " + result.Resto.EstFiable);
        Console.WriteLine("RawUrl: " + (result.RawUrl ?? "<null>"));
        return 0;
    }
}
