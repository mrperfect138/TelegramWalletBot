using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using Octokit;
using Telegram.Bot.Polling;

namespace TelegramWalletBot
{

    public class WalletBot
{
    private readonly TelegramBotClient _botClient;
    private readonly GitHubClient _githubClient;
    private readonly string _gistId;

    private Bank _bank;

    public WalletBot(string telegramToken, string githubToken, string gistId)
    {
        _botClient = new TelegramBotClient(telegramToken);
        _githubClient = new GitHubClient(new ProductHeaderValue("telegram-wallet-bot"));
        _githubClient.Credentials = new Credentials(githubToken);
        _gistId = gistId;
    }

    public async Task InitializeAsync()
    {
        // Load existing data from GitHub
        var users = new Dictionary<long, WalletUser>();
        var transactions = new List<Transaction>();

        try
        {
            var gist = await _githubClient.Gist.Get(_gistId);

            var usersContent = gist.Files["users.json"].Content;
            if (!string.IsNullOrEmpty(usersContent))
                users = JsonConvert.DeserializeObject<Dictionary<long, WalletUser>>(usersContent)
                        ?? new Dictionary<long, WalletUser>();

            var txContent = gist.Files["transactions.json"].Content;
            if (!string.IsNullOrEmpty(txContent))
                transactions = JsonConvert.DeserializeObject<List<Transaction>>(txContent)
                               ?? new List<Transaction>();
        }
        catch
        {
            users = new Dictionary<long, WalletUser>();
            transactions = new List<Transaction>();
        }

        _bank = new Bank(users, transactions);

        Console.WriteLine($"Bot initialized. {_bank.GetAllUsers().Count} users, {_bank.GetAllTransactions().Count} transactions.");
    }

    private async Task SaveDataAsync()
    {
        try
        {
            var updateGist = new GistUpdate();

            updateGist.Files.Add(new KeyValuePair<string, GistFileUpdate>(
                "users.json",
                new GistFileUpdate { Content = JsonConvert.SerializeObject(_bank.GetAllUsers(), Formatting.Indented) }
            ));

            updateGist.Files.Add(new KeyValuePair<string, GistFileUpdate>(
                "transactions.json",
                new GistFileUpdate { Content = JsonConvert.SerializeObject(_bank.GetAllTransactions(), Formatting.Indented) }
            ));
            Console.WriteLine($"📊 Saving {_bank.GetAllUsers().Count} users and {_bank.GetAllTransactions().Count} transactions...");
            foreach (var kv in _bank.GetAllUsers())
            {
                Console.WriteLine($"  {kv.Key} -> @{kv.Value.Username}, balance {kv.Value.FormattedBalance}");
            }

            await _githubClient.Gist.Edit(_gistId, updateGist);
            Console.WriteLine("✅ Data saved to GitHub gist.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data: {ex.Message}");
        }
    }


    private async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message == null || update.Message.Text == null)
            return;

        var userId = update.Message.From.Id;
        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text;

        var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = args[0].ToLower();

        if (command is "/start" or "/transfer" or "/confirm" or "/history")
        {
            await SaveDataAsync();
        }
        // 💡 Just call the bank
        var response = _bank.HandleCommand(userId,
            update.Message.From.Username,
            update.Message.From.FirstName,
            command,
            args);

        await bot.SendTextMessageAsync(
            chatId,
            response,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }
    private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        Console.WriteLine($"Full error: {exception}");
        Console.WriteLine($"error type : {exception.GetType()}");
        Console.WriteLine($"Inner exception: {exception.InnerException?.Message}");
        return Task.CompletedTask;
    }

    public Task StartReceiving()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };

        _botClient.StartReceiving(
            updateHandler: UpdateHandler,
            pollingErrorHandler: ErrorHandler,
            receiverOptions: receiverOptions
        );

        Console.WriteLine("Bot is now receiving messages...");

        return Task.CompletedTask; // so caller can await
    }

}


    // Add the command handlers from previous messages here
    // (I'll include the complete class in the next step)

}