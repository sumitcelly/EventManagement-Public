# Use the official .NET 8 SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ../EventDbAccess/EventDbAccess.csproj ../EventDbAccess/
COPY ../EventUtils/EventUtils.csproj ../EventUtils/
COPY ../CreateTicketApi/CreateTicketApi.csproj ./CreateTicketApi/
WORKDIR /src/CreateTicketApi
RUN dotnet restore

# Copy everything else and build
COPY ../EventDbAccess ../EventDbAccess
COPY ../EventUtils ../EventUtils
#COPY . .
COPY ../CreateTicketApi ../CreateTicketApi
WORKDIR /src/CreateTicketApi
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# EXPOSE 5220
# ENV ASPNETCORE_URLS=http://+:5220
# Line below is very much important for swagger to work.
ENV ASPNETCORE_ENVIRONMENT=Development
#ENV ASPNETCORE_ENVIRONMENT=Docker
ENTRYPOINT ["dotnet", "CreateTicketApi.dll"]