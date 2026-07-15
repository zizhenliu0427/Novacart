# Monolith API (legacy). Default stack uses microservices — see docker-compose.yml.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Novacart.Api.csproj ./
COPY Novacart.Core/ Novacart.Core/
RUN dotnet restore Novacart.Api.csproj

COPY Program.cs ./
COPY Controllers/ Controllers/
COPY appsettings.json appsettings.Development.json ./
RUN dotnet publish Novacart.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Novacart.Api.dll"]
