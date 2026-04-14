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

# 安裝 stunnel4（處理 PostgreSQL TLS 協商）和 CA 憑證
RUN apt-get update && \
    apt-get install -y --no-install-recommends stunnel4 ca-certificates && \
    rm -rf /var/lib/apt/lists/*

COPY stunnel_start.sh /app/stunnel_start.sh
RUN chmod +x /app/stunnel_start.sh

COPY --from=build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["/app/stunnel_start.sh"]
