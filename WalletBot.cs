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
        private Dictionary<long, WalletUser> _users;
        private List<Transaction> _transactions;

        public WalletBot(string telegramToken, string githubToken, string gistId)
        {
            _botClient = new TelegramBotClient(telegramToken);
            _githubClient = new GitHubClient(new ProductHeaderValue("telegram-wallet-bot"));
            _githubClient.Credentials = new Credentials(githubToken);
            _gistId = gistId;
            _users = new Dictionary<long, WalletUser>();
            _transactions = new List<Transaction>();
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
            Console.WriteLine("Bot initialized successfully!");
            Console.WriteLine($"Loaded {_users.Count} users and {_transactions.Count} transactions");
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var gist = await _githubClient.Gist.Get(_gistId);

                // Load users
                var usersContent = gist.Files["users.json"].Content;
                if (!string.IsNullOrEmpty(usersContent))
                {
                    _users = JsonConvert.DeserializeObject<Dictionary<long, WalletUser>>(usersContent)
                             ?? new Dictionary<long, WalletUser>();
                }

                // Load transactions
                var transactionsContent = gist.Files["transactions.json"].Content;
                if (!string.IsNullOrEmpty(transactionsContent))
                {
                    _transactions = JsonConvert.DeserializeObject<List<Transaction>>(transactionsContent)
                                    ?? new List<Transaction>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                // Initialize empty data
                _users = new Dictionary<long, WalletUser>();
                _transactions = new List<Transaction>();
            }
        }

        private async Task SaveDataAsync()
        {
            try
            {
                var usersJson = JsonConvert.SerializeObject(_users, Formatting.Indented);
                var transactionsJson = JsonConvert.SerializeObject(_transactions, Formatting.Indented);
        
                var updateGist = new GistUpdate();
        
                // Add files using KeyValuePair (as you discovered)
                updateGist.Files.Add(new KeyValuePair<string, GistFileUpdate>(
                    "users.json", 
                    new GistFileUpdate { Content = usersJson }));
            
                updateGist.Files.Add(new KeyValuePair<string, GistFileUpdate>(
                    "transactions.json", 
                    new GistFileUpdate { Content = transactionsJson }));
        
                await _githubClient.Gist.Edit(_gistId, updateGist);
                Console.WriteLine("Data saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

       private async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    try
    {
        if (update.Message == null || update.Message.Text == null)
            return;


        var userId = update.Message.From.Id;
        var chatId =  update.Message.Chat.Id;

        Console.WriteLine($"Received: { update.Message.Text} from { update.Message.From.Username}");

        // Ensure user exists
        if (!_users.ContainsKey(userId))
        {
            _users[userId] = new WalletUser 
            { 
                UserId = userId,
                Username =  update.Message.From.Username ?? "unknown",
                FirstName =  update.Message.From.FirstName,
                Balance = 0
            };
            await SaveDataAsync();
        }

        var args =  update.Message.Text.Split(' ');
        var command = args[0].ToLower();

        switch (command)
        {
            case "/start":
                await bot.SendTextMessageAsync(
                    chatId,
                    "💰 *Wallet Bot* 💰\n\n" +
                    "Your digital pocket for fun currency!\n\n" +
                    "*/balance* - Check your balance\n" +
                    "*/transfer* @username amount - Send coins\n" +
                    "*/help* - Show all commands",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
                break;

            case "/balance":
                var user = _users[userId];
                await bot.SendTextMessageAsync(
                    chatId,
                    $"💳 *Your Balance:* {user.Balance} coins",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
                break;

            case "/transfer":
                await HandleTransfer(bot, update.Message, args, ct);
                break;

            case "/help":
                await bot.SendTextMessageAsync(
                    chatId,
                    "💡 *Available Commands:*\n\n" +
                    "*/start* - Welcome message\n" +
                    "*/balance* - Check your coins\n" +
                    "*/transfer @username amount* - Send coins\n" +
                    
                    "*/history* - Shows your recent transactions\n\n"+
                    "*/help* - This message\n\n" +
                    "Example: `/transfer @john 50`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
                break;
            case "/history":
                await ShowHistory(bot, update.Message, ct);
                break;

            default:
                await bot.SendTextMessageAsync(
                    chatId, 
                    "❌ Unknown command. Use /help for available commands.",
                    cancellationToken: ct);
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling update: {ex.Message}");
    }
}

private async Task HandleTransfer(ITelegramBotClient bot, Message message, string[] args, CancellationToken ct)
{
    if (args.Length != 3)
    {
        await bot.SendTextMessageAsync(
            message.Chat.Id,
            "❌ Usage: `/transfer @username amount`\nExample: `/transfer @john 50`",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
        return;
    }

    var senderId = message.From.Id;
    var recipientUsername = args[1].Replace("@", "").ToLower();
    var amountText = args[2];

    if (!decimal.TryParse(amountText, out decimal amount) || amount <= 0)
    {
        await bot.SendTextMessageAsync(
            message.Chat.Id, 
            "❌ Please enter a valid amount (greater than 0)",
            cancellationToken: ct);
        return;
    }

    var sender = _users[senderId];

    // Check balance
    if (sender.Balance < amount)
    {
        await bot.SendTextMessageAsync(
            message.Chat.Id, 
            "❌ Insufficient funds!",
            cancellationToken: ct);
        return;
    }

    // Find recipient (case-insensitive)
    var recipient = _users.Values.FirstOrDefault(u => 
        u.Username?.ToLower() == recipientUsername);

    if (recipient == null || recipient.UserId == senderId)
    {
        await bot.SendTextMessageAsync(
            message.Chat.Id, 
            "❌ User not found or invalid recipient",
            cancellationToken: ct);
        return;
    }

    // Perform transfer
    sender.Balance -= amount;
    recipient.Balance += amount;

    // Record transaction
    _transactions.Add(new Transaction
    {
        Id = Guid.NewGuid().ToString(),
        FromUserId = senderId,
        ToUserId = recipient.UserId,
        Amount = amount,
        Timestamp = DateTime.UtcNow
    });

    await SaveDataAsync();

    // Notify both users
    await bot.SendTextMessageAsync(
        message.Chat.Id,
        $"✅ Successfully sent {amount} coins to @{recipient.Username}!",
        cancellationToken: ct);

    try
    {
        await bot.SendTextMessageAsync(
            recipient.UserId,
            $"💰 You received {amount} coins from @{sender.Username}!\n" +
            $"New balance: {recipient.Balance} coins",
            cancellationToken: ct);
    }
    catch
    {
        // Recipient might not have started the bot yet
        Console.WriteLine($"Could not notify recipient: {recipient.Username}");
    }
}

private Task ErrorHandler(ITelegramBotClient bot, Exception error, CancellationToken ct)
{
    Console.WriteLine($"Full error: {error}");
    Console.WriteLine($"error type : {error.GetType()}");
    Console.WriteLine($"Inner exception: {error.InnerException?.Message}");


    return Task.CompletedTask;
}
private async Task ShowHistory(ITelegramBotClient bot, Message message, CancellationToken ct)
{
    var userId = message.From.Id;

    var userTxs = _transactions
        .Where(t => t.FromUserId == userId || t.ToUserId == userId)
        .OrderByDescending(t => t.Timestamp)
        .Take(5) // show last 5
        .ToList();

    if (!userTxs.Any())
    {
        await bot.SendTextMessageAsync(
            message.Chat.Id,
            "📭 No transactions yet.",
            cancellationToken: ct);
        return;
    }

    string history = "🧾 *Your last transactions:*\n\n";
    foreach (var tx in userTxs)
    {
        string direction = tx.FromUserId == userId ? "Sent" : "Received";
        string otherUser = tx.FromUserId == userId
            ? _users[tx.ToUserId].Username
            : _users[tx.FromUserId].Username;

        history += $"{direction} {tx.Amount} coins {(direction == "Sent" ? "to" : "from")} @{otherUser} on {tx.Timestamp:yyyy-MM-dd HH:mm} UTC\n";
    }

    await bot.SendTextMessageAsync(
        message.Chat.Id,
        history,
        parseMode: ParseMode.Markdown,
        cancellationToken: ct);
}


public async Task StartReceiving()
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
}
        }

        // Add the command handlers from previous messages here
        // (I'll include the complete class in the next step)
        
    }