FROM mcr.microsoft.com/dotnet/sdk:5.0.300-buster-slim AS build-env
WORKDIR /app

COPY ./*sln ./

COPY ./src/RouteNetworkSearchIndexer/*.csproj ./src/RouteNetworkSearchIndexer/

RUN dotnet restore --packages ./packages

COPY . ./
WORKDIR /app/src/RouteNetworkSearchIndexer
RUN dotnet publish -c Release -o out --packages ./packages

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app

COPY --from=build-env /app/src/RouteNetworkSearchIndexer/out .
ENTRYPOINT ["dotnet", "RouteNetworkSearchIndexer.dll"]