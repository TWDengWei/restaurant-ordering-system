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

# 安裝 stunnel4 + netcat + CA 憑證
RUN apt-get update && \
    apt-get install -y --no-install-recommends stunnel4 netcat-openbsd ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# 建立寬鬆 OpenSSL 設定（SECLEVEL=0, TLSv1+）
# 比 sed 修改系統檔更可靠；設定 OPENSSL_CONF 讓所有程序（.NET、stunnel）都使用
RUN cat > /app/openssl-permissive.cnf << 'OPENSSL_CONF_EOF'
openssl_conf = openssl_init

[openssl_init]
ssl_conf = ssl_sect

[ssl_sect]
system_default = system_default_sect

[system_default_sect]
MinProtocol = TLSv1
CipherString = DEFAULT@SECLEVEL=0
OPENSSL_CONF_EOF

COPY stunnel_start.sh /app/stunnel_start.sh
RUN chmod +x /app/stunnel_start.sh

COPY --from=build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# 讓 .NET 和 stunnel 都使用寬鬆的 OpenSSL 設定
ENV OPENSSL_CONF=/app/openssl-permissive.cnf
ENTRYPOINT ["/app/stunnel_start.sh"]
