
using System;

namespace TelegramWalletBot
{
    public class WalletUser
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public decimal Balance { get; set; }
        public string FirstName { get; set; }
    }

    public class Transaction
    {
        public string Id { get; set; }
        public long FromUserId { get; set; }
        public long ToUserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}