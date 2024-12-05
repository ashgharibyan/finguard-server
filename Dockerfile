# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy only the csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application code and build
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /app/out .

# Expose the port the application listens on
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Set environment variables for Railway PostgreSQL connection
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# Set the entry point for the container
ENTRYPOINT ["dotnet", "finguard-server.dll"]
