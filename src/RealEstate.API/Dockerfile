FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY ["src/RealEstate.API/RealEstate.API.csproj", "src/RealEstate.API/"]
COPY ["src/RealEstate.Core/RealEstate.Core.csproj", "src/RealEstate.Core/"]
COPY ["src/RealEstate.Infrastructure/RealEstate.Infrastructure.csproj", "src/RealEstate.Infrastructure/"]
RUN dotnet restore "src/RealEstate.API/RealEstate.API.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "src/RealEstate.API/RealEstate.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "src/RealEstate.API/RealEstate.API.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5268
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5268
ENTRYPOINT ["dotnet", "RealEstate.API.dll"] 