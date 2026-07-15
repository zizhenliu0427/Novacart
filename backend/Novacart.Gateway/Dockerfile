FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Novacart.ServiceDefaults/ Novacart.ServiceDefaults/
COPY Novacart.Gateway/ Novacart.Gateway/
RUN dotnet restore Novacart.Gateway/Novacart.Gateway.csproj
RUN dotnet publish Novacart.Gateway/Novacart.Gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Novacart.Gateway.dll"]
