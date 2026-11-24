FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/PathfinderElements.Api/PathfinderElements.Api.csproj ./
RUN dotnet restore PathfinderElements.Api.csproj
COPY src/PathfinderElements.Api/ ./
RUN dotnet publish PathfinderElements.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /var/lib/pathfinder/keys && chown "$APP_UID" /var/lib/pathfinder/keys && chmod 700 /var/lib/pathfinder/keys
USER $APP_UID
ENTRYPOINT ["dotnet", "PathfinderElements.Api.dll"]
