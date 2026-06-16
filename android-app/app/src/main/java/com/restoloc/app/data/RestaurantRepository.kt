package com.restoloc.app.data

import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class RestaurantRepository {
    private val api: SupabaseApi
    private val key = BuildConfig.SUPABASE_KEY
    private val url = BuildConfig.SUPABASE_URL

    init {
        val retrofit = Retrofit.Builder()
            .baseUrl(url)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
        api = retrofit.create(SupabaseApi::class.java)
    }

    suspend fun fetchAll(): List<Restaurant> = api.getRestaurants(key, "Bearer $key")

    suspend fun add(restaurant: Restaurant): Boolean {
        val body = mapOf(
            "nom" to (restaurant.nom ?: ""),
            "telephone" to (restaurant.telephone ?: ""),
            "origine_poulet" to (restaurant.originePoulet ?: ""),
            "origine_viande" to (restaurant.origineViande ?: ""),
            "latitude" to (restaurant.latitude ?: 0.0),
            "longitude" to (restaurant.longitude ?: 0.0),
            "type" to (restaurant.type ?: "Physique"),
            "est_fiable" to (restaurant.estFiable ?: true),
            "lien_commande" to (restaurant.lienCommande ?: "")
        )
        val resp = api.addRestaurant(key, "Bearer $key", body)
        return resp.isSuccessful
    }

    suspend fun delete(id: Int): Boolean {
        // Supabase expects query param - using DELETE with path isn't supported directly here; use workaround
        val retrofit = Retrofit.Builder().baseUrl(url).addConverterFactory(GsonConverterFactory.create()).build()
        val call = retrofit.create(SupabaseApi::class.java)
        val resp = call.deleteRestaurant(key, "Bearer $key", id)
        return resp.isSuccessful
    }
}
