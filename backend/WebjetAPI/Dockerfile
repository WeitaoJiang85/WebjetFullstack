# Use the official .NET runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["WebjetAPI/WebjetAPI.csproj", "WebjetAPI/"]
RUN dotnet restore "WebjetAPI/WebjetAPI.csproj"

# Copy the entire project and build the application
COPY . .
WORKDIR "/src/WebjetAPI"
RUN dotnet build "WebjetAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WebjetAPI.csproj" -c Release -o /app/publish

# Use the runtime image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the environment variable for Render
ENV ASPNETCORE_URLS=http://+:8080
CMD ["dotnet", "WebjetAPI.dll"]
