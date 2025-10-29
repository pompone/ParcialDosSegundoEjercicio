# --------- build stage ---------
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY SegundoEjercicio/SegundoEjercicio.csproj SegundoEjercicio/
COPY SegundoEjercicio.sln ./
RUN dotnet restore SegundoEjercicio/SegundoEjercicio.csproj

COPY . .
RUN dotnet publish SegundoEjercicio/SegundoEjercicio.csproj -c Release -o /app/publish --no-restore

# --------- runtime stage ---------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT} \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "SegundoEjercicio.dll"]


