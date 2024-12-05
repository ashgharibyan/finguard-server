# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the csproj file(s) and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application code and build the application
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Create the Data directory and set permissions
RUN mkdir -p /app/Data && \
    chown -R 1000:1000 /app/Data

# Copy the built application from the previous stage
COPY --from=build /app/out .

# Expose the port your application listens on
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "finguard-server.dll"]
