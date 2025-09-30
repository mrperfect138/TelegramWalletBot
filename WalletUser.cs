
using System;

namespace TelegramWalletBot
{
    public class WalletUser
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public decimal Balance { get; set; }
        public string FirstName { get; set; }

        // Add currency info
        public string CurrencyName { get; set; } = "Zephyr";
        public string CurrencySymbol { get; set; } = "✦"; // or 💨, 🍃, etc.

        // Helper for formatted balance
        public string FormattedBalance => $"{Balance} {CurrencySymbol} {CurrencyName}";
        public string FormattedCurrency => $"{CurrencySymbol} {CurrencyName}";

    }

    public class Transaction
    {
        public string Id { get; set; }
        public long FromUserId { get; set; }
        public long ToUserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

    }
}