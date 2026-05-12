package com.eshop.bff.plugins

import io.ktor.server.application.*
import io.ktor.server.plugins.calllogging.*
import io.ktor.server.metrics.micrometer.*
import io.micrometer.core.instrument.binder.jvm.JvmGcMetrics
import io.micrometer.core.instrument.binder.jvm.JvmMemoryMetrics
import io.micrometer.core.instrument.binder.system.ProcessorMetrics
import io.micrometer.registry.otlp.OtlpMeterRegistry
import io.micrometer.registry.otlp.OtlpConfig
import org.slf4j.event.Level

fun Application.configureMonitoring() {
    install(CallLogging) {
        level = Level.INFO
        filter { call -> call.request.origin.uri.contains("/api/") }
        mdc("correlationId") { call ->
            call.request.headers["X-Correlation-ID"] ?: java.util.UUID.randomUUID().toString()
        }
    }

    install(MicrometerMetrics) {
        registry = OtlpMeterRegistry(OtlpConfig.DEFAULT, io.micrometer.core.instrument.Clock.SYSTEM)
        meterBinders = listOf(
            JvmMemoryMetrics(),
            JvmGcMetrics(),
            ProcessorMetrics()
        )
    }
}
