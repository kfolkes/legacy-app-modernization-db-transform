package com.eshop.bff.routes

import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.response.*
import io.ktor.server.routing.*

fun Application.configureInventoryRoutes() {
    routing {
        route("/api/inventory") {
            get("/stock/{productId}") {
                val productId = call.parameters["productId"]?.toIntOrNull()
                    ?: return@get call.respond(HttpStatusCode.BadRequest, "Invalid product ID")

                // Direct pass-through to inventory service (no aggregation needed)
                call.respond(HttpStatusCode.OK, mapOf("productId" to productId, "message" to "Inventory route placeholder"))
            }
        }
    }
}
