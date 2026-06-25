using RestoLoc;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestoLoc.Services;

public class SupabaseService
{
    private readonly string _url;
    private readonly string _key;
    private readonly HttpClient _httpClient;
    public string? LastErrorMessage { get; private set; }

    public SupabaseService(IConfiguration configuration, HttpClient httpClient)
    {
        _url = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured");
        _key = configuration["Supabase:Key"] ?? throw new InvalidOperationException("Supabase:Key is not configured");
        
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        // Supabase via HTTP ne nécessite pas d'initialisation
        await Task.CompletedTask;
    }

    public async Task<List<Restaurant>> GetRestaurantsAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_url}/rest/v1/restaurants?select=*");
            AddHeaders(request);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur: {response.StatusCode} - {errorBody}");
                return new List<Restaurant>();
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"GET Response: {json}");
            var restaurants = new List<Restaurant>();
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    int idx = 0;
                    foreach (var el in root.EnumerateArray())
                    {
                        var resto = new Restaurant();
                        
                        // Use index as ID since no id/rowid column exists
                        resto.Id = idx;

                        if (el.TryGetProperty("nom", out var nomEl) && nomEl.ValueKind != JsonValueKind.Null)
                            resto.Nom = nomEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("telephone", out var telEl) && telEl.ValueKind != JsonValueKind.Null)
                            resto.Telephone = telEl.ToString();

                        if (el.TryGetProperty("origine_poulet", out var opEl) && opEl.ValueKind != JsonValueKind.Null)
                            resto.OriginePoulet = opEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("origine_viande", out var ovEl) && ovEl.ValueKind != JsonValueKind.Null)
                            resto.OrigineViande = ovEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("latitude", out var latEl) && latEl.ValueKind == JsonValueKind.Number)
                            resto.Latitude = latEl.GetDouble();

                        if (el.TryGetProperty("longitude", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number)
                            resto.Longitude = lonEl.GetDouble();

                        if (el.TryGetProperty("type", out var typeEl) && typeEl.ValueKind != JsonValueKind.Null)
                        {
                            var typeStr = typeEl.GetString() ?? string.Empty;
                            if (Enum.TryParse<TypeRestaurant>(typeStr, out var parsedType))
                                resto.Type = parsedType;
                        }

                        if (el.TryGetProperty("est_fiable", out var fiEl) && fiEl.ValueKind == JsonValueKind.True)
                            resto.EstFiable = true;
                        else if (el.TryGetProperty("est_fiable", out fiEl) && fiEl.ValueKind == JsonValueKind.False)
                            resto.EstFiable = false;

                        if (el.TryGetProperty("lien_commande", out var lienEl) && lienEl.ValueKind != JsonValueKind.Null)
                            resto.LienCommande = lienEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("restaurant_type", out var rtEl) && rtEl.ValueKind != JsonValueKind.Null)
                            resto.RestaurantType = ParseJsonStringOrArray(rtEl);

                        if (el.TryGetProperty("ville", out var villeEl) && villeEl.ValueKind != JsonValueKind.Null)
                            resto.Ville = villeEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("food", out var foodEl) && foodEl.ValueKind != JsonValueKind.Null)
                            resto.Food = ParseJsonStringOrArray(foodEl);

                        if (el.TryGetProperty("rating", out var ratingEl) && ratingEl.ValueKind == JsonValueKind.Number)
                            resto.Rating = ratingEl.GetInt32();

                        if (el.TryGetProperty("price_level", out var priceLevelEl) && priceLevelEl.ValueKind == JsonValueKind.Number)
                            resto.PriceLevel = priceLevelEl.GetInt32();

                        if (el.TryGetProperty("created_at", out var createdAtEl) && createdAtEl.ValueKind != JsonValueKind.Null)
                        {
                            var rawDate = createdAtEl.GetString();
                            if (!string.IsNullOrWhiteSpace(rawDate) && DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
                                resto.CreatedAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                        }

                        restaurants.Add(resto);
                        idx++;
                    }
                }
            }

            return restaurants;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des restaurants: {ex.Message}");
            return new List<Restaurant>();
        }
    }

    public async Task<bool> AddRestaurantAsync(Restaurant restaurant)
    {
        try
        {
            LastErrorMessage = null;
            var createdAt = restaurant.CreatedAt.HasValue
                ? TruncateToMinute(restaurant.CreatedAt.Value.ToUniversalTime())
                : TruncateToMinute(DateTime.UtcNow);

            var json = JsonSerializer.Serialize(new
            {
                nom = restaurant.Nom,
                telephone = string.IsNullOrWhiteSpace(restaurant.Telephone) ? null : restaurant.Telephone,
                origine_poulet = string.IsNullOrWhiteSpace(restaurant.OriginePoulet) ? null : restaurant.OriginePoulet,
                origine_viande = string.IsNullOrWhiteSpace(restaurant.OrigineViande) ? null : restaurant.OrigineViande,
                type = restaurant.Type.ToString(),
                est_fiable = restaurant.EstFiable,
                lien_commande = string.IsNullOrWhiteSpace(restaurant.LienCommande) ? null : restaurant.LienCommande,
                restaurant_type = string.IsNullOrWhiteSpace(restaurant.RestaurantType) ? null : new[] { restaurant.RestaurantType },
                ville = string.IsNullOrWhiteSpace(restaurant.Ville) ? null : restaurant.Ville,
                food = string.IsNullOrWhiteSpace(restaurant.Food) ? null : new[] { restaurant.Food },
                rating = restaurant.Rating,
                price_level = restaurant.PriceLevel,
                created_at = createdAt
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_url}/rest/v1/restaurants")
            {
                Content = content
            };
            AddHeaders(request);
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                LastErrorMessage = $"Supabase {response.StatusCode} {response.ReasonPhrase}: {responseBody}";
                Console.WriteLine("Supabase AddRestaurant payload: " + json);
                Console.WriteLine($"Erreur lors de l'ajout du restaurant: {LastErrorMessage}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            Console.WriteLine($"Erreur lors de l'ajout du restaurant: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateRestaurantAsync(Restaurant restaurant, string? originalNom = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                nom = restaurant.Nom,
                telephone = string.IsNullOrWhiteSpace(restaurant.Telephone) ? null : restaurant.Telephone,
                origine_poulet = string.IsNullOrWhiteSpace(restaurant.OriginePoulet) ? null : restaurant.OriginePoulet,
                origine_viande = string.IsNullOrWhiteSpace(restaurant.OrigineViande) ? null : restaurant.OrigineViande,
                type = restaurant.Type.ToString(),
                est_fiable = restaurant.EstFiable,
                lien_commande = string.IsNullOrWhiteSpace(restaurant.LienCommande) ? null : restaurant.LienCommande,
                restaurant_type = string.IsNullOrWhiteSpace(restaurant.RestaurantType) ? null : new[] { restaurant.RestaurantType },
                ville = string.IsNullOrWhiteSpace(restaurant.Ville) ? null : restaurant.Ville,
                food = string.IsNullOrWhiteSpace(restaurant.Food) ? null : new[] { restaurant.Food },
                rating = restaurant.Rating,
                price_level = restaurant.PriceLevel
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var nomFilter = Uri.EscapeDataString(originalNom ?? restaurant.Nom ?? "");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_url}/rest/v1/restaurants?nom=eq.{nomFilter}")
            {
                Content = content
            };
            AddHeaders(request);
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur lors de la mise à jour du restaurant: {response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(responseBody);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la mise à jour du restaurant: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteRestaurantAsync(string nom)
    {
        try
        {
            // Use 'nom' (restaurant name) as the unique identifier since 'id' column doesn't exist
            var nomFilter = Uri.EscapeDataString(nom ?? "");
            var deleteUrl = $"{_url}/rest/v1/restaurants?nom=eq.{nomFilter}";
            
            Console.WriteLine($"Tentative de suppression avec l'URL: {deleteUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
            AddHeaders(request);
            request.Headers.Add("Prefer", "return=minimal");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur lors de la suppression du restaurant (Status {response.StatusCode}): {response.ReasonPhrase}");
                Console.WriteLine($"Réponse: {responseBody}");
            }
            else
            {
                Console.WriteLine("Suppression réussie!");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression du restaurant: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Comment>> GetCommentsByRestaurantAsync(string restaurantNom)
    {
        try
        {
            var nomFilter = Uri.EscapeDataString(restaurantNom ?? "");
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_url}/rest/v1/comments?restaurant_nom=eq.{nomFilter}&order=created_at.desc");
            AddHeaders(request);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erreur lors de la récupération des commentaires: {response.StatusCode}");
                return new List<Comment>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var comments = new List<Comment>();
            
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in root.EnumerateArray())
                    {
                        var comment = new Comment();

                        if (el.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                            comment.Id = (int)idEl.GetInt64();

                        if (el.TryGetProperty("restaurant_nom", out var restoEl) && restoEl.ValueKind != JsonValueKind.Null)
                            comment.RestaurantNom = restoEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("texte", out var texteEl) && texteEl.ValueKind != JsonValueKind.Null)
                            comment.Texte = texteEl.GetString() ?? string.Empty;

                        if (el.TryGetProperty("created_at", out var dateEl) && dateEl.ValueKind != JsonValueKind.Null)
                        {
                            var rawDate = dateEl.GetString();
                            if (!string.IsNullOrWhiteSpace(rawDate) && DateTime.TryParse(rawDate, out var date))
                                comment.CreatedAt = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                        }

                        comments.Add(comment);
                    }
                }
            }

            return comments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des commentaires: {ex.Message}");
            return new List<Comment>();
        }
    }

    public async Task<bool> AddCommentAsync(Comment comment)
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                restaurant_nom = comment.RestaurantNom,
                texte = comment.Texte
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_url}/rest/v1/comments")
            {
                Content = content
            };
            AddHeaders(request);
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur lors de l'ajout du commentaire: {response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(responseBody);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'ajout du commentaire: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCommentAsync(int commentId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_url}/rest/v1/comments?id=eq.{commentId}");
            AddHeaders(request);
            request.Headers.Add("Prefer", "return=minimal");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur lors de la suppression du commentaire: {response.StatusCode}");
                Console.WriteLine($"Réponse: {responseBody}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression du commentaire: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateCommentAsync(Comment comment)
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                texte = comment.Texte
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_url}/rest/v1/comments?id=eq.{comment.Id}")
            {
                Content = content
            };
            AddHeaders(request);
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erreur lors de la mise à jour du commentaire: {response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(responseBody);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la mise à jour du commentaire: {ex.Message}");
            return false;
        }
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("apikey", _key);
        request.Headers.Add("Authorization", $"Bearer {_key}");
    }

    private static DateTime TruncateToMinute(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);
    }

    private static string ParseJsonStringOrArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? string.Empty;

        if (element.ValueKind == JsonValueKind.Array)
        {
            var values = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    values.Add(item.GetString() ?? string.Empty);
                else
                    values.Add(item.ToString());
            }
            return string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)));
        }

        return string.Empty;
    }
}
