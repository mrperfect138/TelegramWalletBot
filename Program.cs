using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace TelegramWalletBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting Telegram Wallet Bot...");

            var telegramToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
            var githubToken   = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var gistId        = Environment.GetEnvironmentVariable("GIST_ID");

            if (string.IsNullOrWhiteSpace(telegramToken) ||
                string.IsNullOrWhiteSpace(githubToken) ||
                string.IsNullOrWhiteSpace(gistId))
            {
                Console.WriteLine("❌ Missing one or more required environment variables: TELEGRAM_TOKEN, GITHUB_TOKEN, GIST_ID");
                Console.WriteLine("Exiting.");
                return;
            }

            try
            {
                Console.WriteLine("1. Testing Telegram connection...");
                var botClient = new TelegramBotClient(telegramToken);
                var me = await botClient.GetMeAsync();
                Console.WriteLine($"✅ Telegram connection OK: @{me.Username}");

                Console.WriteLine("3. Initializing wallet bot...");
                var bot = new WalletBot(telegramToken, githubToken, gistId);
                await bot.InitializeAsync();

                Console.WriteLine("4. Starting message receiver...");
                await bot.StartReceiving();

                Console.WriteLine("✅ Bot started successfully!");
                Console.WriteLine("💡 Send a message to your bot in Telegram");

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Startup failed: {ex.GetType().Name}");
                Console.WriteLine($"🔴 Error: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"🔴 Inner Error: {ex.InnerException.Message}");
            }
        }
    }
}
