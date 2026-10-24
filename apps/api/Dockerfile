# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["GymCrest.Api/GymCrest.Api.csproj", "GymCrest.Api/"]
COPY ["GymCrest.Application/GymCrest.Application.csproj", "GymCrest.Application/"]
COPY ["GymCrest.Contracts/GymCrest.Contracts.csproj", "GymCrest.Contracts/"]
COPY ["GymCrest.Domain/GymCrest.Domain.csproj", "GymCrest.Domain/"]
COPY ["GymCrest.Infrastructure/GymCrest.Infrastructure.csproj", "GymCrest.Infrastructure/"]
COPY ["GymCrest.Shared/GymCrest.Shared.csproj", "GymCrest.Shared/"]

RUN dotnet restore "GymCrest.Api/GymCrest.Api.csproj"

# Copy the remaining source code and build the app
COPY . .
WORKDIR "/src/GymCrest.Api"
RUN dotnet build "GymCrest.Api.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "GymCrest.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "GymCrest.Api.dll"]
