# Use official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /app

# Copy project files
COPY . ./

# Restore NuGet packages
RUN dotnet restore

# Publish the project to a folder called 'out'
RUN dotnet publish -c Release -o out

# Use the runtime image for smaller final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set working directory
WORKDIR /app

# Copy the published files from build stage
COPY --from=build /app/out ./



# Start the bot
ENTRYPOINT ["dotnet", "TelegramWalletBot.dll"]
