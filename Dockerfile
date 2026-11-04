# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj trước để cache layer restore
COPY ECommerce.slnx ./
COPY src/ECommerce.Domain/ECommerce.Domain.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/ECommerce.Application.csproj src/ECommerce.Application/
COPY src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.API/ECommerce.API.csproj src/ECommerce.API/
RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

COPY src/ src/
RUN dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Chạy bằng user không phải root (image aspnet có sẵn user app)
USER $APP_UID
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
