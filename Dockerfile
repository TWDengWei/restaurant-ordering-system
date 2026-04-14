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

# 安裝 CA 憑證
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# 建立寬鬆 OpenSSL 設定（SECLEVEL=0, TLSv1+）
# 讓 .NET SslStream 能與 Render PostgreSQL 完成 TLS 握手
RUN cat > /app/openssl-permissive.cnf << 'EOF'
openssl_conf = openssl_init

[openssl_init]
ssl_conf = ssl_sect

[ssl_sect]
system_default = system_default_sect

[system_default_sect]
MinProtocol = TLSv1
CipherString = DEFAULT@SECLEVEL=0
EOF

COPY --from=build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV OPENSSL_CONF=/app/openssl-permissive.cnf
ENTRYPOINT ["dotnet", "RestaurantApi.dll"]
