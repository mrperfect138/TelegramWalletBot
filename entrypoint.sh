#!/bin/sh
# start the bot in background
dotnet TelegramWalletBot.dll &

# start a simple HTTP server that binds to $PORT (Render requires this)
# python3 -m http.server takes port argument; default to 10000 if PORT not set
PORT=${PORT:-10000}
python3 -m http.server "$PORT"
