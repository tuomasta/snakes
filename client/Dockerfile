FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
    
# Copy csproj and restore as distinct layers
COPY server/*.fsproj server/
COPY shared/*.fsproj shared/
COPY tests/*.fsproj tests/
COPY client/*.fsproj client/
COPY snakes.sln ./

RUN dotnet restore
    
# Copy everything else and build
COPY ./ ./
RUN dotnet build -c Release
RUN dotnet test --no-build -c Release
RUN dotnet publish --no-build client/ -c Release -o out
    
# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "snakes.client.dll"]