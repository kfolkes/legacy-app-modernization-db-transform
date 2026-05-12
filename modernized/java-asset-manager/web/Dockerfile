# syntax=docker/dockerfile:1
# MIGRATION: Multi-stage Dockerfile added in Phase 4. Base image: eclipse-temurin 21 (JDK 21 LTS).
# SECURITY FIX: Non-root user (65532:65532) for the runtime layer.
FROM eclipse-temurin:21-jdk-jammy AS build
WORKDIR /workspace
COPY ../pom.xml /workspace/../pom.xml
COPY pom.xml ./
COPY src ./src
RUN --mount=type=cache,target=/root/.m2 \
    if [ -x ./mvnw ]; then ./mvnw -B -DskipTests package; \
    else mvn -B -DskipTests package; fi

FROM eclipse-temurin:21-jre-jammy
WORKDIR /app
COPY --from=build /workspace/target/*.jar /app/app.jar
USER 65532:65532
EXPOSE 8080
ENTRYPOINT ["java","-XX:+UseContainerSupport","-jar","/app/app.jar"]
