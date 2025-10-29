# --------- build stage ---------
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copiamos el .csproj (aprovecha cache en restore)
COPY SegundoEjercicio/SegundoEjercicio.csproj SegundoEjercicio/
# (Opcional) si tenés .sln en la raíz:
COPY SegundoEjercicio.sln ./

RUN dotnet restore SegundoEjercicio/SegundoEjercicio.csproj

# Copiamos todo el código
COPY . .

# Publicamos
RUN dotnet publish SegundoEjercicio/SegundoEjercicio.csproj -c Release -o /app/publish --no-restore

# --------- runtime stage ---------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT} \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "SegundoEjercicio.dll"]

