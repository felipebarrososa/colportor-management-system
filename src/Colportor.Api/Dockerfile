# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# o context do build é a RAIZ do repo (por causa do compose)
COPY . .
# restaura a solution inteira (garante dependências/versões)
RUN dotnet restore Colportor.sln
# publica apenas a API
RUN dotnet publish src/Colportor.Api/Colportor.Api.csproj -c Release -o /app

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","Colportor.Api.dll"]
