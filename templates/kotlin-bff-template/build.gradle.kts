val kotlin_version: String by project
val ktor_version: String by project
val logback_version: String by project
val resilience4j_version: String by project

plugins {
    kotlin("jvm") version "2.0.21"
    id("io.ktor.plugin") version "3.0.3"
    kotlin("plugin.serialization") version "2.0.21"
    id("com.google.protobuf") version "0.9.4"
}

group = "com.eshop.bff"
version = "1.0.0"

application {
    mainClass.set("com.eshop.bff.ApplicationKt")

    val isDevelopment: Boolean = project.ext.has("development")
    applicationDefaultJvmArgs = listOf("-Dio.ktor.development=$isDevelopment")
}

repositories {
    mavenCentral()
}

dependencies {
    // Ktor Server
    implementation("io.ktor:ktor-server-core:$ktor_version")
    implementation("io.ktor:ktor-server-netty:$ktor_version")
    implementation("io.ktor:ktor-server-content-negotiation:$ktor_version")
    implementation("io.ktor:ktor-serialization-kotlinx-json:$ktor_version")
    implementation("io.ktor:ktor-server-auth:$ktor_version")
    implementation("io.ktor:ktor-server-auth-jwt:$ktor_version")
    implementation("io.ktor:ktor-server-cors:$ktor_version")
    implementation("io.ktor:ktor-server-call-logging:$ktor_version")
    implementation("io.ktor:ktor-server-status-pages:$ktor_version")
    implementation("io.ktor:ktor-server-metrics-micrometer:$ktor_version")

    // Ktor Client (downstream calls)
    implementation("io.ktor:ktor-client-core:$ktor_version")
    implementation("io.ktor:ktor-client-cio:$ktor_version")
    implementation("io.ktor:ktor-client-content-negotiation:$ktor_version")
    implementation("io.ktor:ktor-client-logging:$ktor_version")

    // Authentication (Azure Entra ID / MSAL)
    implementation("com.microsoft.azure:msal4j:1.17.0")

    // Resilience
    implementation("io.github.resilience4j:resilience4j-kotlin:$resilience4j_version")
    implementation("io.github.resilience4j:resilience4j-circuitbreaker:$resilience4j_version")
    implementation("io.github.resilience4j:resilience4j-retry:$resilience4j_version")

    // Observability
    implementation("io.micrometer:micrometer-registry-otlp:1.14.0")
    implementation("io.opentelemetry:opentelemetry-api:1.44.0")
    implementation("io.opentelemetry:opentelemetry-sdk:1.44.0")
    implementation("io.opentelemetry:opentelemetry-exporter-otlp:1.44.0")

    // Logging
    implementation("ch.qos.logback:logback-classic:$logback_version")
    implementation("net.logstash.logback:logstash-logback-encoder:8.0")

    // gRPC
    implementation("io.grpc:grpc-kotlin-stub:1.4.1")
    implementation("io.grpc:grpc-netty-shaded:1.69.0")
    implementation("com.google.protobuf:protobuf-kotlin:4.29.0")

    // Testing
    testImplementation("io.ktor:ktor-server-test-host:$ktor_version")
    testImplementation("io.mockk:mockk:1.13.13")
    testImplementation("org.jetbrains.kotlin:kotlin-test-junit:$kotlin_version")
}

kotlin {
    jvmToolchain(21)
}
