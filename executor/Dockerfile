FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
    
# Copy csproj and restore as distinct layers
COPY executor/*.fsproj executor/
COPY shared/*.fsproj shared/
COPY game/*.fsproj game/

RUN dotnet restore executor
    
# Copy everything else and build
COPY ./ ./
RUN dotnet publish executor/ -c Release -o out
    
# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "snakes.executor.dll"]