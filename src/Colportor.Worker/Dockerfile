# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Colportor.sln
RUN dotnet publish src/Colportor.Worker/Colportor.Worker.csproj -c Release -o /app

# Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Colportor.Worker.dll"]
