package com.restoloc.app.data

import retrofit2.Response
import retrofit2.http.*

interface SupabaseApi {
    @GET("restaurants")
    suspend fun getRestaurants(@Header("apikey") apikey: String, @Header("Authorization") auth: String): List<Restaurant>

    @Headers("Content-Type: application/json")
    @POST("restaurants")
    suspend fun addRestaurant(@Header("apikey") apikey: String, @Header("Authorization") auth: String, @Body body: Map<String, Any>): Response<Void>

    @DELETE("restaurants?id=eq.{id}")
    suspend fun deleteRestaurant(@Header("apikey") apikey: String, @Header("Authorization") auth: String, @Path("id") id: Int): Response<Void>
}
