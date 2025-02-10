# Use official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use .NET 8.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["WebjetAPI/WebjetAPI.csproj", "./"]
WORKDIR "/src/WebjetAPI"
RUN dotnet restore "WebjetAPI.csproj"

# Copy the entire project and build the application
COPY . .
RUN dotnet build "WebjetAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WebjetAPI.csproj" -c Release -o /app/publish

# Use runtime image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy .env file if exists (Render will have it in /etc/secrets/.env)
COPY .env /app/.env

# Set environment variable for Render
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
CMD ["dotnet", "WebjetAPI.dll"]
