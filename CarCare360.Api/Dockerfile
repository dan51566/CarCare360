# syntax=docker/dockerfile:1
# ============================================================================
#  CarCare360.Api — многоэтапная сборка образа серверного REST API
#  Этап build  — на образе .NET SDK (restore + publish)
#  Этап final  — на образе ASP.NET Core Runtime (без исходного кода и SDK)
# ============================================================================

# ─── Этап сборки ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файл проекта — для кэширования слоя восстановления пакетов.
COPY CarCare360.Api.csproj ./
RUN dotnet restore CarCare360.Api.csproj

# Затем остальной исходный код и публикация в Release.
COPY . ./
RUN dotnet publish CarCare360.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ─── Этап выполнения ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# В контейнере API слушает HTTP на порту 8080 (наружу публикуется через compose).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CarCare360.Api.dll"]
