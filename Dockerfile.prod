# Multi-stage build para produção
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Colportor.Api/Colportor.Api.csproj", "src/Colportor.Api/"]
COPY ["src/Colportor.Worker/Colportor.Worker.csproj", "src/Colportor.Worker/"]
RUN dotnet restore "src/Colportor.Api/Colportor.Api.csproj"
COPY . .
WORKDIR "/src/src/Colportor.Api"
RUN dotnet build "Colportor.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Colportor.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurações de produção
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Colportor.Api.dll"]
