FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything first
COPY . ./

# # Restore & publish
# RUN dotnet restore
# RUN dotnet publish -c Release -o out
# Restore dependencies
RUN dotnet restore Telegram_Bot.sln

# Build & publish
RUN dotnet publish TelegramWalletBot.csproj -c Release -o out


# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

# Optional: expose dummy port so Render is happy
ENV PORT 10000
EXPOSE 10000

# Run the bot
CMD ["dotnet", "TelegramWalletBot.dll"]
