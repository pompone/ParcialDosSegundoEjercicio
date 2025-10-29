# --------- build stage ---------
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copiamos solo el csproj para aprovechar cache en restore
COPY SegundoEjercicio.csproj ./
RUN dotnet restore ./SegundoEjercicio.csproj

# Copiamos todo y publicamos
COPY . .
RUN dotnet publish ./SegundoEjercicio.csproj -c Release -o /app/publish --no-restore

# --------- runtime stage ---------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# Render expone PORT; escuchamos en 0.0.0.0:$PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT} \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

# (Opcional) si usás cultura es-AR y querés ICU completo
# RUN apt-get update && apt-get install -y --no-install-recommends locales && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./
# (No EXPOSE necesario para Render, pero útil localmente)
EXPOSE 8080

ENTRYPOINT ["dotnet", "SegundoEjercicio.dll"]
