namespace RestoLoc;

using System.Text.Json.Serialization;

public enum TypeRestaurant { Physique, EnLigne }

public class Restaurant
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("telephone")]
    public string Telephone { get; set; } = string.Empty;

    [JsonPropertyName("origine_poulet")]
    public string OriginePoulet { get; set; } = string.Empty;

    [JsonPropertyName("origine_viande")]
    public string OrigineViande { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
    
    [JsonPropertyName("type")]
    public TypeRestaurant Type { get; set; } = TypeRestaurant.Physique;

    [JsonPropertyName("est_fiable")]
    public bool EstFiable { get; set; } = true;

    [JsonPropertyName("lien_commande")]
    public string LienCommande { get; set; } = string.Empty;

    [JsonPropertyName("restaurant_type")]
    public string RestaurantType { get; set; } = string.Empty;

    [JsonPropertyName("food")]
    public string Food { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}