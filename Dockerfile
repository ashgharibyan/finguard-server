FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Create and set permissions for Data directory
RUN mkdir -p /app/Data && \
    chown -R 1000:1000 /app/Data

COPY --from=build /app/out .

# Ensure the SQLite database directory is mounted as a volume
VOLUME /app/Data

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "finguard-server.dll"]