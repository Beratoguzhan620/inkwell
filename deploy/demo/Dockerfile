# The public demo in one container, for a single free web service (Render): nginx in front of the site and the admin
# panel (under /admin), both on one SQLite file. Every restart starts again from the demo content. Locally the two
# apps run on their own ports on SQL Server instead.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish BlogApp.MVC/BlogApp.MVC.csproj -c Release -o /app/web /p:UseAppHost=false \
 && dotnet publish BlogApp.Admin/BlogApp.Admin.csproj -c Release -o /app/admin /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends nginx && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/web ./web
COPY --from=build /app/admin ./admin
COPY deploy/demo/nginx.conf deploy/demo/start.sh ./
# Cover images are saved under the site's wwwroot.
RUN mkdir -p /app/web/wwwroot/uploads/covers && chown -R "$APP_UID":"$APP_UID" /app/web/wwwroot/uploads
USER $APP_UID
ENTRYPOINT ["sh", "/app/start.sh"]
