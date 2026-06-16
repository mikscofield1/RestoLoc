$apiUrl = "https://scciwphmfeztlupwzjwm.supabase.co/rest/v1/restaurants"
$apiKey = "sb_publishable_QFIvkju07Y_YmSlyBTCgSg_JjD9ezg0"

$headers = @{
    "apikey" = $apiKey
    "Authorization" = "Bearer $apiKey"
    "Content-Type" = "application/json"
    "Prefer" = "return=minimal"
}

$restaurants = @(
    @{
        nom = "Le Poulet Doré"
        telephone = "0140203040"
        origine_poulet = "France"
        origine_viande = "Belgique"
        latitude = 48.8566
        longitude = 2.3522
        type = "Physique"
        est_fiable = $true
        lien_commande = ""
    },
    @{
        nom = "Cloud Kitchen Poulet"
        telephone = ""
        origine_poulet = "France"
        origine_viande = "France"
        latitude = 0
        longitude = 0
        type = "EnLigne"
        est_fiable = $false
        lien_commande = "https://example.com"
    },
    @{
        nom = "L'As du Grill"
        telephone = "0190807060"
        origine_poulet = "France"
        origine_viande = "France"
        latitude = 48.8700
        longitude = 2.3600
        type = "Physique"
        est_fiable = $true
        lien_commande = ""
    }
)

Write-Host "Insertion des restaurants dans Supabase..."

foreach ($resto in $restaurants) {
    $json = $resto | ConvertTo-Json
    Write-Host "Insertion de: $($resto.nom)"
    
    try {
        $response = Invoke-WebRequest -Uri $apiUrl `
            -Method Post `
            -Headers $headers `
            -Body $json `
            -ErrorAction Stop
        
        Write-Host "✓ $($resto.nom) inséré avec succès (Status: $($response.StatusCode))" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Erreur pour $($resto.nom): $($_.Exception.Response.StatusCode) - $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Réponse: $($_.Exception.Response.Content)" -ForegroundColor Yellow
    }
    
    Start-Sleep -Milliseconds 500
}

Write-Host "`nInsertion terminée!"
