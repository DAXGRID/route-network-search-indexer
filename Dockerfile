FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-env
WORKDIR /app

COPY ./*sln ./

COPY ./src/RouteNetworkSearchIndexer/*.csproj ./src/RouteNetworkSearchIndexer/

RUN dotnet restore --packages ./packages

COPY . ./
WORKDIR /app/src/RouteNetworkSearchIndexer
RUN dotnet publish -c Release -o out --packages ./packages

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
WORKDIR /app

RUN apk add --no-cache icu-libs krb5-libs

COPY --from=build-env /app/src/RouteNetworkSearchIndexer/out .
ENTRYPOINT ["dotnet", "RouteNetworkSearchIndexer.dll"]
