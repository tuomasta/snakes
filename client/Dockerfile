FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
    
# Copy csproj and restore as distinct layers
COPY client/*.fsproj client/
COPY shared/*.fsproj shared/
COPY game/*.fsproj game/

RUN dotnet restore client
    
# Copy everything else and build
COPY ./ ./
RUN dotnet publish client/ -c Release -o out
    
# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "snakes.client.dll"]