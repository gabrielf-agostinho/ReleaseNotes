# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is reused while only source files change.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ReleaseNotes.Core/ReleaseNotes.Core.csproj src/ReleaseNotes.Core/
COPY src/ReleaseNotes.Web/ReleaseNotes.Web.csproj src/ReleaseNotes.Web/
RUN dotnet restore src/ReleaseNotes.Web/ReleaseNotes.Web.csproj

COPY src/ src/
RUN dotnet publish src/ReleaseNotes.Web/ReleaseNotes.Web.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Writable folders for the SQLite database, logs and media. Named volumes mounted here inherit this
# ownership, so the app runs as the image's non-root user without an entrypoint script.
# Umbraco also insists on Views/Partials existing at startup (it never writes there in this site).
RUN mkdir -p umbraco/Data umbraco/Logs wwwroot/media Views/Partials \
    && chown -R "$APP_UID:$APP_UID" umbraco wwwroot/media

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ReleaseNotes.Web.dll"]
