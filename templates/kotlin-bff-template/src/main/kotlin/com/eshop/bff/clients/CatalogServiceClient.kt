// Downstream client for Catalog microservice with circuit breaker
package com.eshop.bff.clients

import com.eshop.bff.models.CatalogItem
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

class CatalogServiceClient {
    private val baseUrl = System.getenv("CATALOG_SERVICE_URL") ?: "http://localhost:5001"

    private val client = HttpClient(CIO) {
        install(ContentNegotiation) {
            json(Json { ignoreUnknownKeys = true })
        }
        engine {
            requestTimeout = 5_000
        }
    }

    private val circuitBreaker: CircuitBreaker = CircuitBreaker.of(
        "catalog-service",
        CircuitBreakerConfig.custom()
            .failureRateThreshold(50f)
            .waitDurationInOpenState(Duration.ofSeconds(30))
            .slidingWindowSize(10)
            .minimumNumberOfCalls(5)
            .build()
    )

    suspend fun getItems(page: Int, pageSize: Int): List<CatalogItem> {
        return circuitBreaker.executeSupplier {
            kotlinx.coroutines.runBlocking {
                client.get("$baseUrl/api/catalog") {
                    parameter("page", page)
                    parameter("pageSize", pageSize)
                }.body()
            }
        }
    }

    suspend fun getItem(id: Int): CatalogItem? {
        return circuitBreaker.executeSupplier {
            kotlinx.coroutines.runBlocking {
                try {
                    client.get("$baseUrl/api/catalog/$id").body()
                } catch (_: Exception) {
                    null
                }
            }
        }
    }
}
