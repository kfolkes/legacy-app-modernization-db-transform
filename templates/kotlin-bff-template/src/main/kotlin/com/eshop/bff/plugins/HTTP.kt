package com.eshop.bff.plugins

import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.plugins.cors.routing.*

fun Application.configureHTTP() {
    install(CORS) {
        allowMethod(HttpMethod.Options)
        allowMethod(HttpMethod.Get)
        allowMethod(HttpMethod.Post)
        allowMethod(HttpMethod.Put)
        allowMethod(HttpMethod.Delete)
        allowHeader(HttpHeaders.Authorization)
        allowHeader(HttpHeaders.ContentType)
        allowHeader("X-Correlation-ID")
        // SECURITY: Configure allowed origins per environment
        val allowedOrigins = environment.config.propertyOrNull("cors.allowedOrigins")
            ?.getList() ?: listOf("http://localhost:3000")
        allowedOrigins.forEach { allowHost(it.removePrefix("http://").removePrefix("https://")) }
    }
}
