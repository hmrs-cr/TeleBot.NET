FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish /p:Version=$(date "+%y").$(date "+%m%d").$(date "+%H%M").$(date "+%S") -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build /src/out .
ENTRYPOINT ["dotnet", "TeleBotService.dll"]

