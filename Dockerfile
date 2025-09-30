 FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
 WORKDIR /app

 # Copy everything first
 COPY . ./

  # Restore & publish
  RUN dotnet restore
  RUN dotnet publish -c Release -o out
 # Restore dependencies
# RUN dotnet restore Telegram_Bot.sln
#
# # Build & publish
# RUN dotnet publish TelegramWalletBot.csproj -c Release -o out


 # Build runtime image
 FROM mcr.microsoft.com/dotnet/runtime:8.0
 WORKDIR /app
 COPY --from=build /app/out .

 # Optional: expose dummy port so Render is happy
 ENV PORT 10000
 EXPOSE 10000

 # Run the bot
 CMD ["dotnet", "TelegramWalletBot.dll"]
# v2 from here
#  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#  WORKDIR /app

#  # Copy everything first
#  COPY . ./

#   # Restore & publish
#   RUN dotnet restore
#   RUN dotnet publish -c Release -o out


# # Install python3 so we can run a tiny HTTP server
# # (the runtime image is Debian-based so apt-get works)
# USER root
# RUN apt-get update && apt-get install -y python3 && rm -rf /var/lib/apt/lists/*

# # Copy entrypoint
# COPY entrypoint.sh /app/entrypoint.sh
# RUN chmod +x /app/entrypoint.sh


#  # Build runtime image
#  FROM mcr.microsoft.com/dotnet/runtime:8.0
#  WORKDIR /app
#  COPY --from=build /app/out .

#  # Optional: expose dummy port so Render is happy
#  ENV PORT 10000
#  EXPOSE 10000

#  # Run the bot and server
#  CMD ["/app/entrypoint.sh"]
# # CMD ["dotnet", "TelegramWalletBot.dll"]
# # v2 from here


