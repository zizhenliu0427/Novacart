FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Novacart.Core/ Novacart.Core/
COPY Novacart.ServiceDefaults/ Novacart.ServiceDefaults/
COPY Services/Novacart.Auth.Api/ Services/Novacart.Auth.Api/
RUN dotnet restore Services/Novacart.Auth.Api/Novacart.Auth.Api.csproj
RUN dotnet publish Services/Novacart.Auth.Api/Novacart.Auth.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Novacart.Auth.Api.dll"]
