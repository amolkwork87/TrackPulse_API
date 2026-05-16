# Use the official .NET 10 SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files
COPY ["TrackPulse.slnx", "./"]
COPY ["TrackPulse.API.csproj", "./"]

# Restore dependencies
RUN dotnet restore "TrackPulse.slnx"

# Copy the rest of the code
COPY . .

# Build the API project
RUN dotnet build "TrackPulse.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet publish "TrackPulse.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage - use ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Install required Linux dependency
RUN apt-get update && \
    apt-get install -y libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*

# Copy published application from publish stage
COPY --from=publish /app/publish .


# Run the application
ENTRYPOINT ["dotnet", "TrackPulse.API.dll"]
