using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramWalletBot
{
    public class Bank
    {
    
        private readonly Dictionary<long, PendingTransfer> _pendingTransfers = new();

        public class PendingTransfer
        {
            public long SenderId { get; set; }
            public long RecipientId { get; set; }
            public decimal Amount { get; set; }
            public string Description { get; set; }
        }

    
        private Dictionary<long, WalletUser> _users = new();
        private List<Transaction> _transactions = new();

        public Bank(Dictionary<long, WalletUser> users, List<Transaction> transactions)
        {
            _users = users;
            _transactions = transactions;
        }

        public Dictionary<long, WalletUser> GetAllUsers()
        {
            return _users;
        }
        public List<Transaction> GetAllTransactions() => _transactions;

        public string HandleCommand(long userId, string username, string firstName, string command, string[] args)
        {
            // Ensure user exists
            if (!_users.ContainsKey(userId))
            {
                
                _users.Add(userId,new WalletUser
                {
                    UserId = userId,
                    Username = username ?? "unknown",
                    FirstName = firstName,
                    Balance = 0  
                });
            }

            // ✅ Check if user has a pending transfer and respond to Y/N
            if (HasPendingTransfer(userId) && (command.Equals("y", StringComparison.OrdinalIgnoreCase) || command.Equals("n", StringComparison.OrdinalIgnoreCase)))
            {
                return ConfirmTransfer(userId, command);
            }
            switch (command)
            {
                case "/start":
                    return $"💰 *{new WalletUser().FormattedCurrency} Bot* 💰\n\n" +
                           $"Your digital pocket for currency!\n\n" +
                           "*/profile* - shows your profile\n" +
                           "*/balance* - Check your balance\n" +
                           $"*/transfer* @username amount description(optional) - Send {new WalletUser().FormattedCurrency}\n" +
                           "*/history* N - Shows your recent *N* transactions\n" +
                           "*/help* - Show all commands";

                case "/profile":
                    var user = _users[userId];
                    return $"👤 *Profile*\n\n" +
                           $"• Name: {user.FirstName}\n" +
                           $"• Username: @{user.Username}\n" +
                           $"• User ID: {user.UserId}\n" +
                           $"• Balance: {user.FormattedBalance}";

                case "/balance":
                    return $"💳 *Your Balance:* { _users[userId].FormattedBalance }";

                case "/help":
                    return "💡 *Available Commands:*\n\n" +
                           "*/start* - Welcome message\n" +
                           "*/profile* - shows your profile\n" +
                           $"*/balance* - Check your {new WalletUser().FormattedCurrency}\n" +
                           $"*/transfer @username amount description(optional) - Send {new WalletUser().FormattedCurrency}\n" +
                           "*/history* - Shows your recent transactions\n" +
                           "*/help* - This message\n\n" +
                           "Example: `/transfer @john 50`";

                // 👉 Transfer and History you’ll delegate to their own Bank methods
                case "/transfer":
                    return HandleTransferCommand(userId, args);

                case "/history":
                    return GetHistory(userId, args);
                case "/pool":
                    return $"🏦 Total Zephyr in circulation: {_users.Values.Sum(u => u.Balance)} {new WalletUser().FormattedCurrency}";

                default:
                    return "❌ Unknown command. Use /help for available commands.";
            }
        }

        #region Transfer

        private string HandleTransferCommand(long senderId, string[] args)
        {
            if (args.Length < 3)
                return "❌ Usage: `/transfer <recipientId> <amount> <description(optional)>`";

            if (!long.TryParse(args[1], out long recipientId))
                return "❌ Invalid recipient ID.";

            if (!decimal.TryParse(args[2], out decimal amount) || amount <= 0)
                return "❌ Invalid amount.";

            string description = args.Length > 3 
                ? string.Join(' ', args.Skip(3))
                : "(no description)";

            // Step 3: Request transfer, shows transaction details & asks Y/N
            return RequestTransfer(senderId, recipientId, amount, description);
        }


        public string RequestTransfer(long senderId, long recipientId, decimal amount, string description = "(no description)")
        {
            if (!_users.ContainsKey(senderId)) return "❌ Sender not found.";
            if (!_users.ContainsKey(recipientId)) return "❌ Recipient not found.";
            if (senderId == recipientId) return "❌ Cannot send Zephyr to yourself.";
            if (_users[senderId].Balance < amount) return "❌ Insufficient funds.";

            _pendingTransfers[senderId] = new PendingTransfer
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Amount = amount,
                Description = description
            };

            var sender = _users[senderId];
            var recipient = _users[recipientId];

            return
                $"📝 *Transaction Details:*\n\n" +
                $"• Sender: @{sender.Username} ({sender.UserId})\n" +
                $"• Receiver: @{recipient.Username} ({recipient.UserId})\n" +
                $"• Amount: {amount} ✦ Zephyr\n" +
                $"• Note: {description}\n\n" +
                $"Continue? (Reply Y / N)";
        }


        public string ConfirmTransfer(long senderId, string response)
        {
            if (!_pendingTransfers.ContainsKey(senderId)) return "⚠️ No pending transfer found.";

            var pending = _pendingTransfers[senderId];

            if (response.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                var result = Transfer(
                    pending.SenderId,
                    pending.RecipientId,
                    pending.Amount,
                    pending.Description
                );

                _pendingTransfers.Remove(senderId);
                return result;
            }
            else if (response.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                _pendingTransfers.Remove(senderId);
                return "❌ Transaction cancelled.";
            }
            else
            {
                return "⚠️ Please reply with Y or N.";
            }
        }

        public bool HasPendingTransfer(long userId) => _pendingTransfers.ContainsKey(userId);

        public string Transfer(long senderId, long recipientId, decimal amount, string description = "(no description)")
        {
            if (!_users.ContainsKey(senderId)) return "❌ Sender not found!";
            if (!_users.ContainsKey(recipientId)) return "❌ Recipient not found!";

            var sender = _users[senderId];
            var recipient = _users[recipientId];

            if (sender.Balance < amount) return "❌ Insufficient funds!";

            // Perform transfer
            sender.Balance -= amount;
            recipient.Balance += amount;

            // Record transaction
            _transactions.Add(new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                FromUserId = senderId,
                ToUserId = recipientId,
                Amount = amount,
                Description = description,
                Timestamp = DateTime.UtcNow
            });

            return $"✅ Sent {amount} {new WalletUser().FormattedCurrency} to @{recipient.Username}!\n📝 {description}";
        }
        

        #endregion



        private string GetHistory(long userId, string[] args)
        {
            int count = 10;
            if (args.Length > 1 && int.TryParse(args[1], out int customCount))
                count = customCount;

            var txs = _transactions
                .Where(t => t.FromUserId == userId || t.ToUserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Take(count)
                .ToList();

            if (!txs.Any()) return "📭 No transactions yet.";

            string history = "🧾 *Your last transactions:*\n\n";
            foreach (var tx in txs)
            {
                string direction = tx.FromUserId == userId ? "Sent" : "Received";
                string otherUser = tx.FromUserId == userId
                    ? _users[tx.ToUserId].Username
                    : _users[tx.FromUserId].Username;

                history += $"{direction} {tx.Amount} {new WalletUser().FormattedCurrency} " +
                           $"{(direction == "Sent" ? "to" : "from")} @{otherUser} " +
                           $"on {tx.Timestamp:yyyy-MM-dd HH:mm:ss} UTC\n" +
                           $"📝 {tx.Description}\n"+
                           $"With Transaction Id of : _{tx.Id}_\n\n";
            }
            return history;
        }

    }
}
