FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create Data directory and set permissions
RUN mkdir -p /app/Data && \
    chown -R app:app /app/Data && \
    chmod 755 /app/Data

# Ensure migrations are copied
COPY --from=build /app/Migrations ./Migrations/

EXPOSE 80
ENTRYPOINT ["dotnet", "finguard-server.dll"]