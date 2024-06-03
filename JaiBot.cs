using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLIB;
using System.Diagnostics;
using AsyncAwaitBestPractices;
using static CommandLIB.TwitchBot;
using System.Runtime.CompilerServices;
using System.Net.Http.Json;
using System.Net;
using System.Collections.Generic;
using System.Data.SQLite;
//Thank you @thisduckisnotentitled_0 for helping debug this code <3

namespace JaiBot
{
    class Program
    {
        private static SQLiteConnection _dbConnection;
        private static SQLiteConnection _deathsDbConnection;
        private static SQLiteConnection _survivedDbConnection;
        private static SQLiteConnection _winsDbConnection;
        private static SQLiteConnection _fourKDbConnection;
        private static SQLiteConnection _lostMoneyDbConnection;
        private static TwitchBot twitchBot;

        private static void UpdateLostMoney(string username, int lostAmount)
        {
            using (var command = new SQLiteCommand("INSERT INTO LostMoney (Username, LostAmount) VALUES (@username, @lostAmount)", _lostMoneyDbConnection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@lostAmount", lostAmount);
                command.ExecuteNonQuery();
            }
        }

        private static int GetUserBalance(string username)
        {
            using (var command = new SQLiteCommand($"SELECT Balance FROM UserBalances WHERE Username = @username", _dbConnection))
            {
                command.Parameters.AddWithValue("@username", username);
                var result = command.ExecuteScalar();
                return result == null ? 0 : Convert.ToInt32(result);
            }
        }
        private static Dictionary<string, DateTime> lastWorkTimestamps = new Dictionary<string, DateTime>();

        // Method to format and display the leaderboard

        private static void UpdateUserBalance(string username, int newBalance)
        {
            using (var command = new SQLiteCommand($"REPLACE INTO UserBalances (Username, Balance) VALUES (@username, @balance)", _dbConnection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@balance", newBalance);
                command.ExecuteNonQuery();
            }
        }

        private static readonly TimeSpan WorkCooldown = TimeSpan.FromHours(1);
        private static readonly TimeSpan ThrowAssCooldown = TimeSpan.FromHours(1);
        private static Dictionary<string, DateTime> lastThrowAssTimestamps = new Dictionary<string, DateTime>();

        private static int survivedCounter = 0;
        private static int deathsCounter = 0;
        private static int winsCounter = 0;
        private static int FourCounter = 0;

        // AUTHORIZED COMMANDS LIST
        private static bool IsCounterCommand(string command)
        {
            return command.StartsWith("!setdeaths") || command.StartsWith("!setsurvived") ||
                   command.EndsWith("!s") || command.StartsWith("!d") ||
                   command.StartsWith("!resetS") || command.StartsWith("!resetD") ||
                   command.StartsWith("!setwins") || command.EndsWith("!w") ||
                   command.StartsWith("!set4k") || command.EndsWith("!4k") ||
                   command.StartsWith("!plus4k") || command.EndsWith("!wins") ||
                   command.StartsWith("!reset4k") || command.StartsWith("!resetW");
        }

        // !GIVE command 24h delay variable
        private static Dictionary<string, DateTime> lastGiveTimestamps = new Dictionary<string, DateTime>();
        private static Dictionary<string, DateTime> lastResetTimestamps = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, int> coinflipBets = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> coinflipChallenges = new Dictionary<string, string>();

        private static (string, int) GetTopJaiCoinHolder()
        {
            using (var command = new SQLiteCommand("SELECT Username, Balance FROM UserBalances ORDER BY Balance DESC LIMIT 1", _dbConnection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (reader.GetString(0), reader.GetInt32(1));
                    }
                }
            }
            // Return default values if no top holder found
            return ("No top holder found", 0);
        }

        private static int GetCounterValue(string counterName)
        {
            int counterValue = 0;

            using (var command = new SQLiteCommand($"SELECT Balance FROM UserBalances WHERE Username = @counterName", _dbConnection))
            {
                command.Parameters.AddWithValue("@counterName", counterName);
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    counterValue = Convert.ToInt32(result);
                }
            }

            return counterValue;
        }

        static async Task Main(string[] args)
        {
            // BOT INITALIZATION
            DatabaseInitializer.InitializeDatabase();
            
            string password = File.ReadAllText("C:\\JaiBot Stuff\\OauthTEXT\\oauth.txt");
            string botUsername = "jais_pocket_dimension";

            twitchBot = new TwitchBot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();
            await twitchBot.JoinChannel("jai0899");

            //VIEWER COMMANDS
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");

                // CURRENCY COMMANDS
                int senderBalance = GetUserBalance(twitchChatMessage.Sender);

                // Handle command to display top JaiCoin holder
                if (twitchChatMessage.Message.StartsWith("!top"))
                {
                    var (topHolder, balance) = GetTopJaiCoinHolder();
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Top JaiCoin holder: @{topHolder}, Balance: {balance} JaiCoins");
                }

                // !work command here
                if (twitchChatMessage.Message.StartsWith("!work"))
                {
                    // Check if the user is on cooldown
                    if (lastWorkTimestamps.TryGetValue(twitchChatMessage.Sender, out DateTime lastWorkTime) &&
                        DateTime.Now - lastWorkTime < WorkCooldown)
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you can only work once per hour.");
                    }
                    else
                    {
                        // Generate a random number between 10 and 100 (inclusive) for the earned coins
                        Random random = new Random();
                        int earnedCoins = random.Next(10, 101); // Generates a random number between 10 and 100 (inclusive)

                        // Award the user with the earned coins
                        UpdateUserBalance(twitchChatMessage.Sender, GetUserBalance(twitchChatMessage.Sender) + earnedCoins);

                        // Update last work timestamp for the user
                        lastWorkTimestamps[twitchChatMessage.Sender] = DateTime.Now;

                        // Send a message indicating the number of coins earned
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender} worked hard in the Aurora Mines and earned {earnedCoins} JaiCoins!");
                    }
                }

                if (twitchChatMessage.Message.StartsWith("!house"))
                {
                    int totalLostMoney = 0;
                    using (var command = new SQLiteCommand("SELECT SUM(LostAmount) FROM LostMoney", _lostMoneyDbConnection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            totalLostMoney = Convert.ToInt32(result);
                        }
                    }
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Total lost to the house: {totalLostMoney} JaiCoins.");
                }

                if (twitchChatMessage.Message.StartsWith("!throwass"))
                {
                    // Check if the user is on cooldown
                    if (lastThrowAssTimestamps.TryGetValue(twitchChatMessage.Sender, out DateTime lastThrowAssTime) &&
                        DateTime.Now - lastThrowAssTime < ThrowAssCooldown)
                    {
                        var throwassCooldownRemaining = (int)(lastThrowAssTime.Add(ThrowAssCooldown) - DateTime.Now).TotalMinutes;
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you can only throw it back once an hour. Cooldown remaining: {throwassCooldownRemaining}m");
                    }
                    else
                    {
                        // Generate a random number between 10 and 100 (inclusive) for the earned coins
                        Random random = new Random();
                        int earnedCoins = random.Next(20, 120);

                        // Award the user with the earned coins
                        UpdateUserBalance(twitchChatMessage.Sender, GetUserBalance(twitchChatMessage.Sender) + earnedCoins);

                        // Update last throwass timestamp for the user
                        lastThrowAssTimestamps[twitchChatMessage.Sender] = DateTime.Now;

                        // Send a message indicating the number of coins earned
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender} threw it back at Ducks Dive Bar, earning {earnedCoins} JaiCoins, the shame is free!");
                    }
                }

                if (twitchChatMessage.Message.StartsWith("!coinflip"))
                {
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    if (parts.Length == 3 && int.TryParse(parts[1], out int betAmount) && betAmount > 0)
                    {
                        string opponent = parts[2].TrimStart('@');
                        if (string.IsNullOrEmpty(opponent))
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you need to mention a user to challenge to a coinflip.");
                        }
                        else if (opponent == twitchChatMessage.Sender)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you can't challenge yourself to a coinflip.");
                        }
                        else if (GetUserBalance(twitchChatMessage.Sender) < betAmount)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you don't have enough JaiCoins to bet.");
                        }
                        else if (GetUserBalance(opponent) < betAmount)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{opponent} doesn't have enough JaiCoins to accept the coinflip.");
                        }
                        else
                        {
                            // Store the coinflip challenge
                            coinflipChallenges[opponent] = twitchChatMessage.Sender;
                            coinflipBets[opponent] = betAmount;

                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{opponent}, you have been challenged to a coinflip by @{twitchChatMessage.Sender} for {betAmount} JaiCoins. Type !accept to join or !decline to reject.");
                        }
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"Invalid command format or amount. Please use !coinflip <bet amount> @user");
                    }
                }

                // Accept command for coinflip
                if (twitchChatMessage.Message.StartsWith("!accept"))
                {
                    if (coinflipChallenges.TryGetValue(twitchChatMessage.Sender, out string challenger))
                    {
                        int betAmount = coinflipBets[twitchChatMessage.Sender];

                        // Check if both users have the required bet amount
                        if (GetUserBalance(twitchChatMessage.Sender) < betAmount)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you don't have enough JaiCoins to accept the coinflip.");
                        }
                        else if (GetUserBalance(challenger) < betAmount)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{challenger} doesn't have enough JaiCoins to accept the coinflip.");
                        }
                        else
                        {
                            // Perform the coinflip
                            Random random = new Random();
                            bool isWin = random.Next(2) == 0; // 50/50 chance of winning

                            if (isWin)
                            {
                                // Transfer the bet amount to the winner
                                int newBalance = GetUserBalance(twitchChatMessage.Sender) + betAmount;
                                UpdateUserBalance(twitchChatMessage.Sender, newBalance);
                                UpdateUserBalance(challenger, GetUserBalance(challenger) - betAmount);

                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} won {betAmount} JaiCoins in the coinflip against @{challenger}!");
                            }
                            else
                            {
                                // Transfer the bet amount to the winner
                                int newBalance = GetUserBalance(challenger) + betAmount;
                                UpdateUserBalance(challenger, newBalance);
                                UpdateUserBalance(twitchChatMessage.Sender, GetUserBalance(twitchChatMessage.Sender) - betAmount);

                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} lost {betAmount} JaiCoins in the coinflip against @{challenger}!");
                            }

                            // Remove the challenge and bet
                            coinflipChallenges.Remove(twitchChatMessage.Sender);
                            coinflipBets.Remove(twitchChatMessage.Sender);
                        }
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you don't have any pending coinflip challenges to accept.");
                    }
                }

                // Decline command for coinflip
                if (twitchChatMessage.Message.StartsWith("!decline"))
                {
                    if (coinflipChallenges.TryGetValue(twitchChatMessage.Sender, out string challenger))
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} declined the coinflip challenge from @{challenger}.");
                        coinflipChallenges.Remove(twitchChatMessage.Sender);
                        coinflipBets.Remove(twitchChatMessage.Sender);
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you don't have any pending coinflip challenges to decline.");
                    }
                }

                if (twitchChatMessage.Message.StartsWith("!cooldown"))
                {
                    string cooldownMessage = $"{twitchChatMessage.Sender}, your cooldowns: ";

                    if (lastWorkTimestamps.TryGetValue(twitchChatMessage.Sender, out DateTime lastWorkTime))
                    {
                        var workCooldownRemaining = (int)(lastWorkTime.Add(WorkCooldown) - DateTime.Now).TotalMinutes;
                        cooldownMessage += $"!work: {workCooldownRemaining}m remaining on cooldown";
                    }
                    else
                    {
                        cooldownMessage += "!work: Ready";
                    }

                    cooldownMessage += " | ";

                    if (lastThrowAssTimestamps.TryGetValue(twitchChatMessage.Sender, out DateTime lastThrowassTime))
                    {
                        var throwassCooldownRemaining = (int)(lastThrowassTime.Add(ThrowAssCooldown) - DateTime.Now).TotalMinutes;
                        cooldownMessage += $"!throwass: {throwassCooldownRemaining}m remaining on cooldown";
                    }
                    else
                    {
                        cooldownMessage += "!throwass: Ready";
                    }

                    await twitchBot.SendMessage(twitchChatMessage.Channel, cooldownMessage);
                }

                if (twitchChatMessage.Message.StartsWith("!give"))
                {
                    // Parse command to get recipient and amount
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int amount) && amount > 0)
                    {
                        string recipient = parts[1].TrimStart('@');
                        if (string.IsNullOrEmpty(recipient))
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you need to mention a user using the @ symbol to give them JaiCoins.");
                        }
                        else
                        {
                            // Ensure the sender has sufficient balance
                            if (senderBalance < amount)
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you don't have enough JaiCoins to give.");
                            }
                            else
                            {
                                // Ensure sender doesn't go below 0 balance
                                int newSenderBalance = Math.Max(senderBalance - amount, 0);

                                // Deduct amount from sender's balance
                                UpdateUserBalance(twitchChatMessage.Sender, newSenderBalance);

                                // Add amount to recipient's balance
                                int recipientBalance = GetUserBalance(recipient);
                                UpdateUserBalance(recipient, recipientBalance + amount);

                                // Update last give timestamp for the sender
                                lastGiveTimestamps[twitchChatMessage.Sender] = DateTime.Now;

                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} has given {amount} JaiCoins to @{recipient}.");
                            }
                        }
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"Invalid command format or amount. Please use !give @user <amount>");
                    }
                }
                if (twitchChatMessage.Message.StartsWith("!balance"))
                {
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    string targetUser = parts.Length > 1 ? parts[1].TrimStart('@') : null;
                    string username = string.IsNullOrEmpty(targetUser) ? twitchChatMessage.Sender : targetUser;
                    int currentBalance = GetUserBalance(username);

                    if (targetUser != null)
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{username}'s balance is {currentBalance} JaiCoins.");
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{username}, your balance is {currentBalance} JaiCoins.");
                    }
                }

                if (twitchChatMessage.Message.StartsWith("!bal"))
                {
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    string targetUser = parts.Length > 1 ? parts[1].TrimStart('@') : null;
                    string username = string.IsNullOrEmpty(targetUser) ? twitchChatMessage.Sender : targetUser;
                    int currentBalance = GetUserBalance(username);

                    if (targetUser != null)
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{username}'s balance is {currentBalance} JaiCoins.");
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{username}, your balance is {currentBalance} JaiCoins.");
                    }
                }
                if (twitchChatMessage.Message.StartsWith("!cf"))
                {
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int betAmount) && betAmount > 0)
                    {
                        if (senderBalance < betAmount)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender}, you don't have enough JaiCoins to bet.");
                        }
                        else
                        {
                            // Perform the coin flip
                            Random random = new Random();
                            bool isWin = random.Next(2) == 0; // 50/50 chance of winning

                            if (isWin)
                            {
                                // Double the bet
                                int newBalance = senderBalance + betAmount;
                                UpdateUserBalance(twitchChatMessage.Sender, newBalance);
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} won {betAmount} JaiCoins! Your balance is now {newBalance} JaiCoins.");
                            }
                            else
                            {
                                // Deduct the bet
                                int newBalance = senderBalance - betAmount;
                                UpdateUserBalance(twitchChatMessage.Sender, newBalance);
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} lost {betAmount} JaiCoins! Your balance is now {newBalance} JaiCoins.");

                                // Record lost money
                                UpdateLostMoney(twitchChatMessage.Sender, betAmount);
                            }
                        }
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"Invalid command format or amount. Please use !cf <bet amount>");
                    }
                }

                if (twitchChatMessage.Message.StartsWith("!deaths"))
                {
                    int deaths = GetCounterValue("deaths");
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has died {deaths} trial(s) in a row! :(");
                }
                if (twitchChatMessage.Message.StartsWith("!survived"))
                {
                    int survived = GetCounterValue("survived");
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has survived {survived} trial(s) in a row!");
                }
                if (twitchChatMessage.Message.StartsWith("!4k"))
                {
                    int fourKs = GetCounterValue("4k");
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has had {fourKs} 4k's in a row!");
                }
                if (twitchChatMessage.Message.EndsWith("!wins"))
                {
                    int wins = GetCounterValue("wins");
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has had {wins} win(s) in a row!");
                }
                if (twitchChatMessage.Message.Contains("Jai go live"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "Jai go live already, the people are getting bored");
                }
                if (twitchChatMessage.Message.StartsWith("!welcome"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "Welcome to the stream! My name is Jai, I mainly play Dead By Daylight, I do custom nights with viewers on Saturday, I main Dwight, and am a loving father and husband!");
                }
                if (twitchChatMessage.Message.StartsWith("!hibaby"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "Hi little Aurora! you look so big and strong! <3 <3 <3");
                }
                if (twitchChatMessage.Message.StartsWith("!dancedance"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "jai089Dancetime jai089Dancetime jai089Dancetime");
                }
                if (twitchChatMessage.Message.StartsWith("!gg"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "jai089GetGud jai089GetGud jai089GetGud");
                }
                if (twitchChatMessage.Message.StartsWith("!ez"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "jai089JaiEZ jai089JaiEZ jai089JaiEZ");
                }
                if (twitchChatMessage.Message.StartsWith("!french"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "The French are assholes Loyde");
                }
                if (twitchChatMessage.Message.EndsWith("!duck"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "If @thisduckisnotentitled_0 has no haters, I am dead");
                }
                if (twitchChatMessage.Message.StartsWith("!duckisdead"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "Thank you killer for killing @thisduckisnotentitled_0");
                }

                // HELP COMMANDS
                if (twitchChatMessage.Message.StartsWith("!help"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "!welcome,!survived, !deaths, !wins, !4k, !hibaby, !dancedance, !gg, !ez, !shoot [@user], !hug [@user]");
                }
                if (twitchChatMessage.Message.StartsWith("!modhelp"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "!w (killer win), !s (survivor win), !plus4k (got a 4k), !d (survivor death), !reset4k, !resetW");
                }

                // @ COMMANDS
                if (twitchChatMessage.Message.StartsWith("!shoot"))
                {
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    if (parts.Length == 2)
                    {
                        string targetUsername = parts[1].TrimStart('@');
                        if (string.IsNullOrEmpty(targetUsername))
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} you have to mention a user using the @ symbol to use this command");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Call an ambulance! Call an ambulance! But not for me! {twitchChatMessage.Sender} shot {targetUsername}... they're okay, right?");
                        }
                    }
                }

                if (twitchChatMessage.Message.StartsWith("!hug"))
                {
                    string[] parts = twitchChatMessage.Message.Split(' ');
                    if (parts.Length == 2)
                    {
                        string targetUsername = parts[1].TrimStart('@');
                        if (string.IsNullOrEmpty(targetUsername))
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} you have to mention a user using the @ symbol to hug them");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"{twitchChatMessage.Sender} hugs {targetUsername} tightly! <3");
                        }
                    }
                }
            };

            //AUTHORIZED USERS COMMANDS
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");

                if (IsCounterCommand(twitchChatMessage.Message))
                {//DONT FUCK WITH THIS

                    int senderBalance = GetUserBalance(twitchChatMessage.Sender);

                    if (twitchChatMessage.Message.StartsWith("!plus4k"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            FourCounter++;
                            UpdateUserBalance("4k", FourCounter); // Update "4k" counter in the database
                            winsCounter++;
                            UpdateUserBalance("wins", winsCounter);
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai got another 4k! He is now at {FourCounter} 4k's in a row!");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    if (twitchChatMessage.Message.EndsWith("!w"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            winsCounter++;
                            UpdateUserBalance("wins", winsCounter);
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai got another win! He is now at {winsCounter} killer wins in a row!");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    if (twitchChatMessage.Message.StartsWith("!s"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            survivedCounter++;
                            UpdateUserBalance("survived", survivedCounter);
                            deathsCounter = 0; // Reset deaths counter when survival is increased
                            UpdateUserBalance("deaths", deathsCounter);
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Trials survived in a row: {survivedCounter} :)");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    if (twitchChatMessage.Message.EndsWith("!d"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            deathsCounter++;
                            UpdateUserBalance("deaths", deathsCounter);
                            survivedCounter = 0; // Reset survival counter when death is increased
                            UpdateUserBalance("survived", survivedCounter);
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Trials died in a row: {deathsCounter} :(");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }
                    if (twitchChatMessage.Message.StartsWith("!resetW"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            // Reset the 4k counter
                            winsCounter = 0;
                            UpdateUserBalance("wins", winsCounter);

                            await twitchBot.SendMessage(twitchChatMessage.Channel, "Win counter have been reset.");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    if (twitchChatMessage.Message.StartsWith("!reset4k"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            // Reset the 4k counter
                            FourCounter = 0;
                            UpdateUserBalance("4k", FourCounter);

                            await twitchBot.SendMessage(twitchChatMessage.Channel, "4k counter have been reset.");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }
                }//DONT FUCK WITH THIS  
            };
            await Task.Delay(-1);
        }

        private static bool IsAuthorized(string username)
        {
            Console.WriteLine($"Checking authorization for user: {username}");
            string[] authorizedUsernames = { "jai0899", "girlnamedluna", "killersnipes273", "pantsshampooman", "jais_pocket_dimension" };
            bool isAuthorized = authorizedUsernames.Contains(username.ToLower());
            Console.WriteLine($"Is authorized: {isAuthorized}");
            return isAuthorized;
        }
    }
}
