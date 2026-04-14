# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Backend/RestaurantApi/RestaurantApi.csproj ./
RUN dotnet restore
COPY Backend/RestaurantApi/ ./
RUN dotnet publish -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 安裝 CA 憑證並放寬 OpenSSL 安全等級（相容 Render PostgreSQL TLS）
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates openssl && \
    rm -rf /var/lib/apt/lists/* && \
    sed -i 's/SECLEVEL=2/SECLEVEL=1/g' /etc/ssl/openssl.cnf 2>/dev/null || true

COPY --from=build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "RestaurantApi.dll"]
