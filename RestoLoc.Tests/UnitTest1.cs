using RestoLoc;

namespace RestoLoc.Tests;

public class CityValidationTests
{
    [Fact]
    public void RejectsCityNamesContainingDigits()
    {
        var result = Calculs.AnalyserUrlGoogleMaps("https://www.google.com/maps/place/Restaurant+Test,+75000+Paris");

        Assert.NotNull(result);
        Assert.True(string.IsNullOrWhiteSpace(result!.Resto.Ville));
    }
}
