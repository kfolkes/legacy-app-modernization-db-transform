package com.eshop.bff.clients

import com.eshop.bff.models.StockInfo
import io.github.resilience4j.circuitbreaker.CircuitBreaker
import io.github.resilience4j.circuitbreaker.CircuitBreakerConfig
import io.ktor.client.*
import io.ktor.client.call.*
import io.ktor.client.engine.cio.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.client.request.*
import io.ktor.serialization.kotlinx.json.*
import kotlinx.serialization.json.Json
import java.time.Duration

class InventoryServiceClient {
    private val baseUrl = System.getenv("INVENTORY_SERVICE_URL") ?: "http://localhost:5002"

    private val client = HttpClient(CIO) {
        install(ContentNegotiation) {
            json(Json { ignoreUnknownKeys = true })
        }
        engine {
            requestTimeout = 5_000
        }
    }

    private val circuitBreaker: CircuitBreaker = CircuitBreaker.of(
        "inventory-service",
        CircuitBreakerConfig.custom()
            .failureRateThreshold(50f)
            .waitDurationInOpenState(Duration.ofSeconds(30))
            .slidingWindowSize(10)
            .minimumNumberOfCalls(5)
            .build()
    )

    suspend fun getStock(productId: Int): StockInfo? {
        return circuitBreaker.executeSupplier {
            kotlinx.coroutines.runBlocking {
                try {
                    client.get("$baseUrl/api/inventory/stock/$productId").body()
                } catch (_: Exception) {
                    null
                }
            }
        }
    }
}
