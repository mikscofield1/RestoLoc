package com.restoloc.app.data

import com.google.gson.annotations.SerializedName

data class Restaurant(
    @SerializedName("id") val id: Int? = null,
    @SerializedName("nom") val nom: String? = "",
    @SerializedName("telephone") val telephone: String? = "",
    @SerializedName("origine_poulet") val originePoulet: String? = "",
    @SerializedName("origine_viande") val origineViande: String? = "",
    @SerializedName("latitude") val latitude: Double? = 0.0,
    @SerializedName("longitude") val longitude: Double? = 0.0,
    @SerializedName("type") val type: String? = "Physique",
    @SerializedName("est_fiable") val estFiable: Boolean? = true,
    @SerializedName("lien_commande") val lienCommande: String? = "",
    @SerializedName("restaurant_type") val restaurantType: String? = "",
    @SerializedName("food") val food: String? = ""
)
