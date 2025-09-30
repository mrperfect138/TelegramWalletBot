using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TelegramWalletBot
{
    public class Bank
    {
        private readonly string usersFilePath;
        private readonly string transactionsFilePath;

        private Dictionary<long, WalletUser> _users;
        private List<Transaction> _transactions;
        private readonly object _lock = new(); // for thread safety

        public Bank(string usersJsonPath = "users.json", string transactionsJsonPath = "transactions.json")
        {
            usersFilePath = usersJsonPath;
            transactionsFilePath = transactionsJsonPath;

            _users = new Dictionary<long, WalletUser>();
            _transactions = new List<Transaction>();
        }

        // Load data from disk
        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            lock (_lock)
            {
                if (File.Exists(usersFilePath))
                    _users = JsonConvert.DeserializeObject<Dictionary<long, WalletUser>>(File.ReadAllText(usersFilePath))
                             ?? new Dictionary<long, WalletUser>();

                if (File.Exists(transactionsFilePath))
                    _transactions = JsonConvert.DeserializeObject<List<Transaction>>(File.ReadAllText(transactionsFilePath))
                                    ?? new List<Transaction>();
            }
        }

        private async Task SaveDataAsync()
        {
            lock (_lock)
            {
                File.WriteAllText(usersFilePath, JsonConvert.SerializeObject(_users, Formatting.Indented));
                File.WriteAllText(transactionsFilePath, JsonConvert.SerializeObject(_transactions, Formatting.Indented));
            }
        }

        public WalletUser GetOrCreateUser(long userId, string username, string firstName)
        {
            lock (_lock)
            {
                if (!_users.ContainsKey(userId))
                {
                    _users[userId] = new WalletUser
                    {
                        UserId = userId,
                        Username = username,
                        FirstName = firstName,
                        Balance = 1000 // starting balance
                    };
                    SaveDataAsync().Wait();
                }

                return _users[userId];
            }
        }

        public string GetProfile(long userId)
        {
            lock (_lock)
            {
                if (!_users.ContainsKey(userId)) return "User not found!";
                var user = _users[userId];
                return $"👤 Profile\nName: {user.FirstName}\nUsername: @{user.Username}\nUser ID: ||{user.UserId}||\nBalance: {user.FormattedBalance}";
            }
        }

        public string GetTotalPool()
        {
            lock (_lock)
            {
                var total = _users.Values.Sum(u => u.Balance);
                return $"💰 Total Zephyr in circulation: {total} Zephyr";
            }
        }

        public async Task<string> TransferAsync(long fromUserId, long toUserId, decimal amount, string description)
        {
            lock (_lock)
            {
                if (!_users.ContainsKey(fromUserId) || !_users.ContainsKey(toUserId))
                    return "❌ Sender or recipient not found.";

                var sender = _users[fromUserId];
                var recipient = _users[toUserId];

                if (sender.Balance < amount)
                    return "❌ Insufficient funds!";

                sender.Balance -= amount;
                recipient.Balance += amount;

                var tx = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow,
                    Description = description
                };

                _transactions.Add(tx);
                SaveDataAsync().Wait();

                return $"✅ Sent {amount} Zephyr to @{recipient.Username}\nDescription: {description}";
            }
        }

        public string GetTransactionHistory(long userId, int count = 50)
        {
            lock (_lock)
            {
                var userTxs = _transactions
                    .Where(t => t.FromUserId == userId || t.ToUserId == userId)
                    .OrderByDescending(t => t.Timestamp)
                    .Take(count)
                    .ToList();

                if (!userTxs.Any()) return "No transactions yet.";

                return string.Join("\n\n", userTxs.Select(t =>
                    $"{t.Timestamp:yyyy-MM-dd HH:mm} | {(t.FromUserId == userId ? "Sent" : "Received")} {t.Amount} Zephyr\nDesc: {t.Description}"));
            }
        }
    }
}
