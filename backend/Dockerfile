# Imagen de la API. Contexto de construcción: la carpeta backend/   (docker build -t medicalhub-api backend)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MedicalHub.sln ./
COPY src/ src/
RUN dotnet publish src/MedicalHub.Api/MedicalHub.Api.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# Sin privilegios de administrador dentro del contenedor
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
# Sondas para el orquestador: GET /health/live (proceso vivo) y GET /health/ready (acceso a la base de datos)
ENTRYPOINT ["dotnet", "MedicalHub.Api.dll"]
