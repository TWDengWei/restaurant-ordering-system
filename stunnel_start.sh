#!/bin/bash
set -e

# 從 Render 環境變數解析 PostgreSQL 外部 host/port
CONN_STR=$(printenv 'ConnectionStrings__DefaultConnection' 2>/dev/null || echo "")
if [ -z "$CONN_STR" ]; then
  echo "[stunnel] 警告: 找不到 ConnectionStrings__DefaultConnection，直接啟動應用"
  exec dotnet /app/RestaurantApi.dll
fi

PG_EXT_HOST=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^host=' | head -1 | sed 's/^[Hh]ost=//g' | tr -d ' ')
PG_EXT_PORT=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^port=' | head -1 | sed 's/^[Pp]ort=//g' | tr -d ' ')
PG_DATABASE=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^database=' | head -1 | sed 's/^[Dd]atabase=//g' | tr -d ' ')
PG_USER=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^username=' | head -1 | sed 's/^[Uu]sername=//g' | tr -d ' ')
PG_PASS=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^password=' | head -1 | sed 's/^[Pp]assword=//g')

PG_EXT_PORT=${PG_EXT_PORT:-5432}
LOCAL_PORT=5433

echo "[stunnel] 建立 SSL 隧道: localhost:${LOCAL_PORT} → ${PG_EXT_HOST}:${PG_EXT_PORT}"

# 建立 stunnel 設定（protocol=pgsql 支援 PostgreSQL SSL 協商）
cat > /tmp/stunnel.conf << EOF
foreground = no
pid = /tmp/stunnel.pid

[pgsql-tunnel]
client = yes
protocol = pgsql
accept  = 127.0.0.1:${LOCAL_PORT}
connect = ${PG_EXT_HOST}:${PG_EXT_PORT}
verify  = 0
EOF

stunnel4 /tmp/stunnel.conf
sleep 2

# 覆寫連線字串：透過本地 stunnel，無需 SSL
export "ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=${LOCAL_PORT};Database=${PG_DATABASE};Username=${PG_USER};Password=${PG_PASS};SSL Mode=Disable"
echo "[stunnel] 應用將連線至 127.0.0.1:${LOCAL_PORT} (無 SSL，由 stunnel 代理)"

exec dotnet /app/RestaurantApi.dll
