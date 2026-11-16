FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TengriTrails.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
# Hosting platforms (Render, Railway...) pass the port in $PORT
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet TengriTrails.dll"]
