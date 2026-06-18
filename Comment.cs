namespace RestoLoc;

using System.Text.Json.Serialization;

public class Comment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("restaurant_nom")]
    public string RestaurantNom { get; set; } = string.Empty;

    [JsonPropertyName("texte")]
    public string Texte { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
