# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Environment variables (you can override in Render)
ENV TELEGRAM_TOKEN=""
ENV GITHUB_TOKEN=""
ENV GIST_ID=""

# Command to run your bot
ENTRYPOINT ["dotnet", "TelegramWalletBot.dll"]
