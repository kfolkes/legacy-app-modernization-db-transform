# Multi-stage build for .NET 10 microservice
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ServiceName/ServiceName.csproj", "src/ServiceName/"]
RUN dotnet restore "src/ServiceName/ServiceName.csproj"

COPY . .
WORKDIR /src/src/ServiceName
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

LABEL maintainer="Microsoft AppMod Team"
LABEL description="eShop .NET 10 Microservice"

# SECURITY: Run as non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ServiceName.dll"]
