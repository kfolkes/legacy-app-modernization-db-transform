// =============================================================================
// Kotlin BFF (Ktor 3.x) — Application Entry Point
// Follows: kotlin-bff-rules.md, bff-rules.md
// =============================================================================
package com.eshop.bff

import com.eshop.bff.plugins.*
import com.eshop.bff.routes.configureCatalogRoutes
import com.eshop.bff.routes.configureInventoryRoutes
import com.eshop.bff.routes.configureHealthRoutes
import io.ktor.server.application.*
import io.ktor.server.netty.*

fun main(args: Array<String>): Unit = EngineMain.main(args)

fun Application.module() {
    configureSerialization()
    configureSecurity()
    configureMonitoring()
    configureHTTP()
    configureStatusPages()

    // BFF aggregation routes (1 BFF per frontend)
    configureCatalogRoutes()
    configureInventoryRoutes()
    configureHealthRoutes()
}
