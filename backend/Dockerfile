FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/PathfinderElements.Api/PathfinderElements.Api.csproj ./
RUN dotnet restore PathfinderElements.Api.csproj
COPY src/PathfinderElements.Api/ ./
RUN dotnet publish PathfinderElements.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "PathfinderElements.Api.dll"]
