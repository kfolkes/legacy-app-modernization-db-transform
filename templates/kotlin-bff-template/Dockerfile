# Multi-stage build for Kotlin BFF (Ktor)
FROM gradle:8.11-jdk21-alpine AS build
WORKDIR /app

COPY build.gradle.kts gradle.properties settings.gradle.kts ./
COPY gradle ./gradle
RUN gradle dependencies --no-daemon || true

COPY src ./src
RUN gradle buildFatJar --no-daemon

FROM eclipse-temurin:21-jre-alpine AS runtime
WORKDIR /app

LABEL maintainer="Microsoft AppMod Team"
LABEL description="eShop Kotlin BFF (Ktor)"

# SECURITY: Run as non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/build/libs/*-all.jar app.jar

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["java", "-jar", "app.jar"]
