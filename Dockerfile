
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG RUN_TESTS
WORKDIR /src

# Copy everything
COPY . ./
RUN if [ "${RUN_TESTS}" = "yes" ]; then dotnet test; fi
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish /p:Version=$(date "+%y").$(date "+%m%d").$(date "+%H%M").$(date "+%S") -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG INCLUDE_SPEEDTEST
ARG INCLUDE_ALSA
ARG INCLUDE_NETCAT

WORKDIR /App
COPY --from=build /src/out .
COPY --from=build /src/TeleBotService.sh .
RUN chmod +x ./TeleBotService.sh
RUN mkdir ./local-config

RUN if [ "${INCLUDE_SPEEDTEST}" = "yes" ]; then \
        apt-get update \
        && apt-get install -y curl \
        && curl -s https://packagecloud.io/install/repositories/ookla/speedtest-cli/script.deb.sh | bash \
        && apt-get install -y speedtest; \
    fi

RUN if [ "${INCLUDE_ALSA}" = "yes" ]; then \
      apt-get install -y --no-install-recommends alsa-utils && apt-get install -y --no-install-recommends opus-tools && apt-get install -y --no-install-recommends ffmpeg; \
    fi

RUN if [ "${INCLUDE_NETCAT}" = "yes" ]; then \
      apt-get install -y --no-install-recommends netcat-traditional; \
    fi

ENTRYPOINT ["./TeleBotService.sh"]

