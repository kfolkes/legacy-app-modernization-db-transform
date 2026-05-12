// =============================================================================
// BFF Aggregation Routes — Catalog
// Combines data from CatalogService + InventoryService for frontend consumption
// =============================================================================
package com.eshop.bff.routes

import com.eshop.bff.clients.CatalogServiceClient
import com.eshop.bff.clients.InventoryServiceClient
import com.eshop.bff.models.*
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope

fun Application.configureCatalogRoutes() {
    val catalogClient = CatalogServiceClient()
    val inventoryClient = InventoryServiceClient()

    routing {
        authenticate("azure-entra") {
            route("/api/catalog") {
                // BFF Aggregation: Combine catalog + inventory data
                get {
                    val page = call.parameters["page"]?.toIntOrNull() ?: 1
                    val pageSize = call.parameters["pageSize"]?.toIntOrNull() ?: 10

                    // Parallel downstream calls (coroutine-based)
                    val enrichedItems = coroutineScope {
                        val catalogDeferred = async { catalogClient.getItems(page, pageSize) }
                        val catalogItems = catalogDeferred.await()

                        val inventoryDeferred = catalogItems.map { item ->
                            async {
                                val stock = inventoryClient.getStock(item.id)
                                CatalogItemView(
                                    id = item.id,
                                    name = item.name,
                                    description = item.description,
                                    price = item.price,
                                    pictureUri = item.pictureUri,
                                    brandName = item.brandName,
                                    typeName = item.typeName,
                                    availableStock = stock?.quantityAvailable ?: 0,
                                    isInStock = (stock?.quantityAvailable ?: 0) > 0
                                )
                            }
                        }
                        inventoryDeferred.map { it.await() }
                    }

                    call.respond(HttpStatusCode.OK, enrichedItems)
                }

                get("/{id}") {
                    val id = call.parameters["id"]?.toIntOrNull()
                        ?: return@get call.respond(HttpStatusCode.BadRequest, "Invalid ID")

                    coroutineScope {
                        val catalogDeferred = async { catalogClient.getItem(id) }
                        val stockDeferred = async { inventoryClient.getStock(id) }

                        val item = catalogDeferred.await()
                            ?: return@coroutineScope call.respond(HttpStatusCode.NotFound)
                        val stock = stockDeferred.await()

                        call.respond(HttpStatusCode.OK, CatalogItemView(
                            id = item.id,
                            name = item.name,
                            description = item.description,
                            price = item.price,
                            pictureUri = item.pictureUri,
                            brandName = item.brandName,
                            typeName = item.typeName,
                            availableStock = stock?.quantityAvailable ?: 0,
                            isInStock = (stock?.quantityAvailable ?: 0) > 0
                        ))
                    }
                }
            }
        }
    }
}
