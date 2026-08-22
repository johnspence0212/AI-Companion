FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Build.props ./
COPY aspire/ServiceDefaults/ aspire/ServiceDefaults/
COPY apps/module-abstractions/ apps/module-abstractions/
COPY apps/companion/ apps/companion/
COPY apps/platform/ apps/platform/
COPY apps/api/ apps/api/

RUN dotnet restore apps/api/EnterpriseStarter.Api.csproj
RUN dotnet publish apps/api/EnterpriseStarter.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "EnterpriseStarter.Api.dll"]
