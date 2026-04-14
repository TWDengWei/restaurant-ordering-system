#!/bin/bash
# 不用 set -e，避免 stunnel 失敗時整個容器退出

# 從環境變數解析 PostgreSQL 連線資訊
CONN_STR=$(printenv 'ConnectionStrings__DefaultConnection' 2>/dev/null || echo "")

if [ -z "$CONN_STR" ]; then
  echo "[start] 無連線字串，直接啟動應用"
  exec dotnet /app/RestaurantApi.dll
fi

PG_EXT_HOST=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^host=' | head -1 | sed 's/^[Hh][Oo][Ss][Tt]=//g' | tr -d ' \r')
PG_EXT_PORT=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^port=' | head -1 | sed 's/^[Pp][Oo][Rr][Tt]=//g' | tr -d ' \r')
PG_DATABASE=$(echo "$CONN_STR" | tr ';' '\n' | grep -i '^database=' | head -1 | sed 's/^[Dd]atabase=//g' | tr -d ' \r')
PG_USER=$(echo "$CONN_STR"     | tr ';' '\n' | grep -i '^username=' | head -1 | sed 's/^[Uu]sername=//g' | tr -d ' \r')
PG_PASS=$(echo "$CONN_STR"     | tr ';' '\n' | grep -i '^password=' | head -1 | sed 's/^[Pp]assword=//g' | tr -d ' \r')

PG_EXT_PORT=${PG_EXT_PORT:-5432}
LOCAL_PORT=5433

echo "[stunnel] Host=${PG_EXT_HOST} Port=${PG_EXT_PORT} DB=${PG_DATABASE} User=${PG_USER}"

# 建立 stunnel 設定
cat > /tmp/stunnel.conf << STUNNEL_EOF
foreground = no
pid = /tmp/stunnel.pid

[pgsql-tunnel]
client = yes
protocol = pgsql
accept  = 127.0.0.1:${LOCAL_PORT}
connect = ${PG_EXT_HOST}:${PG_EXT_PORT}
verify  = 0
STUNNEL_EOF

echo "[stunnel] Config:"
cat /tmp/stunnel.conf

# 嘗試啟動 stunnel（失敗不中斷）
if stunnel4 /tmp/stunnel.conf 2>&1; then
  echo "[stunnel] 啟動成功，等待就緒..."
  sleep 2

  # 測試 stunnel 是否在監聽
  if nc -z 127.0.0.1 $LOCAL_PORT 2>/dev/null; then
    echo "[stunnel] 隧道就緒，使用 localhost:${LOCAL_PORT}"
    export "ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=${LOCAL_PORT};Database=${PG_DATABASE};Username=${PG_USER};Password=${PG_PASS};SSL Mode=Disable"
  else
    echo "[stunnel] 隧道未就緒，使用原始連線字串"
  fi
else
  echo "[stunnel] 啟動失敗 (exit code $?)，使用原始連線字串"
fi

echo "[start] 啟動 RestaurantApi..."
exec dotnet /app/RestaurantApi.dll
