package com.eshop.bff.routes

import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import kotlinx.serialization.Serializable

@Serializable
data class HealthResponse(val status: String, val service: String = "kotlin-bff", val version: String = "1.0.0")

fun Application.configureHealthRoutes() {
    routing {
        get("/health") {
            call.respond(HttpStatusCode.OK, HealthResponse(status = "Healthy"))
        }
        get("/ready") {
            // TODO: Add downstream service health checks
            call.respond(HttpStatusCode.OK, HealthResponse(status = "Ready"))
        }
    }
}
