# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files first to cache the 'restore' layer
COPY ["EmailSchedulerWorker/EmailSchedulerWorker.csproj", "EmailSchedulerWorker/"]
COPY ["EventDbAccess/EventDbAccess.csproj", "EventDbAccess/"]
COPY ["EventUtils/EventUtils.csproj", "EventUtils/"]

# Restore specifically for the API (this pulls dependencies for the others too)
RUN dotnet restore "EmailSchedulerWorker/EmailSchedulerWorker.csproj"

# Copy the rest of the source code
COPY . .

# Build and Publish
WORKDIR "/src/EmailSchedulerWorker"
RUN dotnet publish "EmailSchedulerWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Standardize port to 8080 (the .NET 8 default for non-root)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Keep this for local testing as you mentioned
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "EmailSchedulerWorker.dll"]
