# Use official .NET 8.0 runtime as the base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use .NET 8.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY backend/WebjetAPI/WebjetAPI.csproj WebjetAPI/
WORKDIR /src/WebjetAPI
RUN dotnet restore "WebjetAPI.csproj"

# Copy the entire WebjetAPI directory and build the application
COPY backend/WebjetAPI/. .
RUN dotnet build "WebjetAPI.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "WebjetAPI.csproj" -c Release -o /app/publish

# Use runtime image to run the published application
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ensure ASP.NET Core listens on all network interfaces for Render deployment
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Run the application
CMD ["dotnet", "WebjetAPI.dll"]
