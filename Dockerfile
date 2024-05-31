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

RUN apt-get update \
    && apt-get install -y curl \
    && curl -s https://packagecloud.io/install/repositories/ookla/speedtest-cli/script.deb.sh | bash \
    && apt-get install -y speedtest

RUN apt-get install -y --no-install-recommends alsa-utils && apt-get install -y --no-install-recommends opus-tools

ENTRYPOINT ["dotnet", "TeleBotService.dll"]

