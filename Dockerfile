FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY ./*sln ./

COPY ./src/RouteNetworkSearchIndexer/*.csproj ./src/RouteNetworkSearchIndexer/

RUN dotnet restore --packages ./packages

COPY . ./
WORKDIR /app/src/RouteNetworkSearchIndexer
RUN dotnet publish -c Release -o out --packages ./packages

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app

COPY --from=build-env /app/src/RouteNetworkSearchIndexer/out .
ENTRYPOINT ["dotnet", "RouteNetworkSearchIndexer.dll"]