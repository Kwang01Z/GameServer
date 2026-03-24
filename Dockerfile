# Use SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY Server/Server.csproj ./Server/
RUN dotnet restore Server/Server.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish Server/Server.csproj -c Release -o out

# Use runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port
EXPOSE 8080
EXPOSE 443

ENTRYPOINT ["dotnet", "Server.dll"]
