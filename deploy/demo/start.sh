#!/bin/sh
# Starts the demo's two apps on local ports behind nginx (8080, the port Render routes to). The site creates and seeds
# the SQLite database first; the admin panel starts once it is listening, so the two never create it at the same time.
set -eu

export DOTNET_gcServer=0 DOTNET_GCConserveMemory=7
export Database__Provider=Sqlite ConnectionStrings__DefaultConnection="Data Source=/tmp/inkwell.db"

cd /app/web
ASPNETCORE_HTTP_PORTS=8081 Demo__LoginHint="Demo: yazar elif@example.com, okur okur@example.com · şifre Demo123! (yönetim paneli: /admin)" \
  dotnet BlogApp.MVC.dll &
web=$!
until bash -c 'exec 3<>/dev/tcp/127.0.0.1/8081' 2>/dev/null; do sleep 1; done

cd /app/admin
ASPNETCORE_HTTP_PORTS=8082 PathBase=/admin Demo__LoginHint="Demo: admin@example.com · şifre Demo123!" \
  dotnet BlogApp.Admin.dll &
admin=$!

nginx -e stderr -c /app/nginx.conf -g 'daemon off;' &
proxy=$!

trap 'kill 0' TERM INT
wait "$web" "$admin" "$proxy"
