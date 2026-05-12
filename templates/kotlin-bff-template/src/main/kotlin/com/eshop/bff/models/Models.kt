package com.eshop.bff.models

import kotlinx.serialization.Serializable

@Serializable
data class CatalogItem(
    val id: Int,
    val name: String,
    val description: String,
    val price: Double,
    val pictureUri: String? = null,
    val brandName: String? = null,
    val typeName: String? = null
)

@Serializable
data class CatalogItemView(
    val id: Int,
    val name: String,
    val description: String,
    val price: Double,
    val pictureUri: String? = null,
    val brandName: String? = null,
    val typeName: String? = null,
    val availableStock: Int,
    val isInStock: Boolean
)

@Serializable
data class StockInfo(
    val productId: Int,
    val quantityAvailable: Int,
    val warehouseLocation: String? = null,
    val lastRestocked: String? = null
)
