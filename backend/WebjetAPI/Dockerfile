# Use official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use .NET 8.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["WebjetAPI.csproj", "./"]
RUN dotnet restore "WebjetAPI.csproj"

# Copy the entire project and build the application
COPY . .
WORKDIR "/src"
RUN dotnet build "WebjetAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WebjetAPI.csproj" -c Release -o /app/publish

# Use runtime image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy the Render secret file if it exists
# Render provides secrets in `/etc/secrets/.env`
RUN if [ -f /etc/secrets/.env ]; then cp /etc/secrets/.env /app/.env; fi

# Copy appsettings.json if it exists in Render secret files
RUN if [ -f /etc/secrets/appsettings.json ]; then cp /etc/secrets/appsettings.json /app/appsettings.json; fi

# Set environment variable for Render
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
CMD ["dotnet", "WebjetAPI.dll"]
