#!/bin/sh
LOCAL_REMOTE_CONFIG='appsettings.Remote.json'
if [ "$REMOTE_CONFIG_URL" != "" ]
then
    echo "Loading remote config..."
    curl "$REMOTE_CONFIG_URL" -s -f -o "$LOCAL_REMOTE_CONFIG.temp" -w "Remote config server response: %{http_code}\n" \
         -H 'Cache-Control: no-cache' \
         -H "Authorization: Token $REMOTE_CONFIG_AUTH_TOKEN"  \
         -H "User-Agent: TelebotService Remote Config Script"
         && mv "$LOCAL_REMOTE_CONFIG.temp" $LOCAL_REMOTE_CONFIG

    if [ "$?" != "0" ]
    then
        echo "Failed to load remote config file: $REMOTE_CONFIG_URL"
        cat "$LOCAL_REMOTE_CONFIG.temp" 2> /dev/null
        echo
        rm "$LOCAL_REMOTE_CONFIG.temp" 2> /dev/null
    else
        echo "Succesfully loaded '$LOCAL_REMOTE_CONFIG' from $REMOTE_CONFIG_URL"
    fi
else
    echo "No remote config specified."
fi

dotnet TeleBotService.dll "$@"