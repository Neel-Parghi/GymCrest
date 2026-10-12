# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["GymForge.Api/GymForge.Api.csproj", "GymForge.Api/"]
COPY ["GymForge.Application/GymForge.Application.csproj", "GymForge.Application/"]
COPY ["GymForge.Contracts/GymForge.Contracts.csproj", "GymForge.Contracts/"]
COPY ["GymForge.Domain/GymForge.Domain.csproj", "GymForge.Domain/"]
COPY ["GymForge.Infrastructure/GymForge.Infrastructure.csproj", "GymForge.Infrastructure/"]
COPY ["GymForge.Shared/GymForge.Shared.csproj", "GymForge.Shared/"]

RUN dotnet restore "GymForge.Api/GymForge.Api.csproj"

# Copy the remaining source code and build the app
COPY . .
WORKDIR "/src/GymForge.Api"
RUN dotnet build "GymForge.Api.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "GymForge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install required Kerberos library for Postgres connection
RUN apt-get update \
    && apt-get install -y libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "GymForge.Api.dll"]
