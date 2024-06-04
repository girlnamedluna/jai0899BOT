using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Controller;
using System.Diagnostics;
using AsyncAwaitBestPractices;
using static Controller.TwitchBot;
using System.Runtime.CompilerServices;
using System.Net.Http.Json;
using System.Net;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;
//Thank you @thisduckisnotentitled_0 for helping debug this code <3
namespace JaiBot
{
    class Program
    {
        // BLACKJACK START

        private static List<string> deck = new List<string>(); // Deck with 52 cards
        private static Dictionary<string, int> cardCounts = new Dictionary<string, int>(); // Track the count of each card

        private static async Task HandleBlackjackCommand(string sender, string channel, int betAmount)
        {
            await Task.Delay(350);

            // Initialize the deck and card counts
            InitializeDeck();

            // Deal two cards to the player and dealer
            string[] playerHand = { DealCard(), DealCard() };
            string[] dealerHand = { DealCard(), DealCard() };

            // Calculate initial scores
            int playerScore = CalculateScore(playerHand);
            int dealerScore = CalculateScore(dealerHand);

            // Calculate total hand value for the player
            int totalPlayerHandValue = playerScore; // Initialize with the initial score

            // Send messages to Twitch chat
            await twitchBot.SendMessage(channel, $"@{sender}, you were dealt: {string.Join(", ", playerHand)}! Your hand value: {totalPlayerHandValue}");

            // Check for blackjack
            if (playerScore == 21)
            {
                await twitchBot.SendMessage(channel, $"Blackjack! You win {betAmount * 2.0} JaiCoins!");
                UpdateUserBalance(sender, GetUserBalance(sender) + (int)(betAmount * 2.0));
                return;
            }
            else if (dealerScore == 21)
            {
                await twitchBot.SendMessage(channel, $"Dealer has blackjack. You lose {betAmount} JaiCoins.");
                UpdateLostMoney(sender, betAmount);
                return;
            }

            // Allow player to hit or stand
            await twitchBot.SendMessage(channel, $"@{sender}, type !hit to get another card or !stand to keep your cards.");

            bool playerStand = false;
            while (!playerStand)
            {
                // Wait for player response
                TwitchChatMessage playerResponse = await WaitForPlayerResponse(sender, channel);

                // Process player response
                if (playerResponse.Message.StartsWith("!hit", StringComparison.OrdinalIgnoreCase))
                {
                    // Deal another card to the player
                    if (deck.Count < 1)
                    {
                        await twitchBot.SendMessage(channel, "The deck is empty.");
                        return;
                    }

                    string newCardPlayer = DealCard(); // Define a new variable for the player's new card
                    playerHand = playerHand.Concat(new string[] { newCardPlayer }).ToArray(); // Convert the result to an array

                    // And similarly for the dealer's turn
                    while (dealerScore < 17)
                    {
                        if (deck.Count < 1)
                        {
                            await twitchBot.SendMessage(channel, "The deck is empty.");
                            return;
                        }

                        // Deal another card to the dealer
                        string newCardDealer = DealCard(); // Define a new variable for the dealer's new card
                        dealerHand = dealerHand.Concat(new string[] { newCardDealer }).ToArray(); // Convert the result to an array

                        // Calculate new score for dealer
                        dealerScore = CalculateScore(dealerHand);
                    }
                    // Calculate new score
                    playerScore = CalculateScore(playerHand);

                    // Calculate total hand value for the player
                    totalPlayerHandValue = playerScore;

                    // Send message to Twitch chat
                    await twitchBot.SendMessage(channel, $"@{sender}, you were dealt: {string.Join(", ", playerHand)}! Your hand value: {totalPlayerHandValue}");

                    // Check for bust
                    if (playerScore > 21)
                    {
                        int lostAmount = betAmount;
                        await twitchBot.SendMessage(channel, $"Busted! You lose {lostAmount} JaiCoins.");
                        UpdateLostMoney(sender, lostAmount);
                        return;
                    }
                }
                else if (playerResponse.Message.StartsWith("!stand", StringComparison.OrdinalIgnoreCase))
                {
                    playerStand = true;
                }
                else
                {
                    await twitchBot.SendMessage(channel, $"@{sender}, please type !hit or !stand.");
                }
            }

            // Dealer's turn
            while (dealerScore < 17)
            {
                if (deck.Count < 1) // Change from deck.Length to deck.Count
                {
                    await twitchBot.SendMessage(channel, "The deck is empty.");
                    return;
                }

                // Deal another card to the dealer
                string newCard = DealCard(); // Use DealCard method to deal a card
                dealerHand = dealerHand.Concat(new string[] { newCard }).ToArray(); // Convert the result to an array

                // Calculate new score for dealer
                dealerScore = CalculateScore(dealerHand);
            }

            // Send message revealing dealer's hand
            await twitchBot.SendMessage(channel, $"Dealer reveals: {string.Join(", ", dealerHand)}! Dealer's hand value: {dealerScore}");

            // Determine the winner
            if (dealerScore > 21 || playerScore > dealerScore)
            {
                await twitchBot.SendMessage(channel, $"You win {betAmount} JaiCoins!");
                UpdateUserBalance(sender, GetUserBalance(sender) + betAmount * 2); // Payout for winning (double the bet)
            }
            else if (playerScore < dealerScore)
            {
                await twitchBot.SendMessage(channel, $"Dealer wins. You lose {betAmount} JaiCoins.");
                UpdateLostMoney(sender, betAmount);
            }
            else
            {
                await twitchBot.SendMessage(channel, $"It's a tie. Your bet of {betAmount} JaiCoins is returned.");
                UpdateUserBalance(sender, GetUserBalance(sender) + betAmount); // Return the bet amount
            }
        }

        private static void InitializeDeck()
        {
            string[] cardValues = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            foreach (string value in cardValues)
            {
                cardCounts[value] = 0; // Initialize count for each card
                for (int i = 0; i < 4; i++)
                {
                    deck.Add(value); // Add each card to the deck
                }
            }
            ShuffleDeck(); // Shuffle the deck
        }

        private static void ShuffleDeck()
        {
            Random rng = new Random();
            int n = deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = deck[k];
                deck[k] = deck[n];
                deck[n] = value;
            }
        }

        private static string DealCard()
        {
            Random rng = new Random();
            string card;

            do
            {
                int index = rng.Next(deck.Count); // Get a random index from the deck
                card = deck[index]; // Get the card at that index

                if (cardCounts[card] < 4) // Check if the card count is less than 4
                {
                    deck.RemoveAt(index); // Remove the dealt card from the deck
                    cardCounts[card]++; // Increment the count of the dealt card
                    break; // Exit the loop if a valid card is found
                }
            } while (true);

            return card;
        }

        // Helper method to calculate the score of a hand
        private static int CalculateScore(string[] hand)
        {
            int score = 0;
            int numAces = 0;

            foreach (string card in hand)
            {
                if (card == "A")
                {
                    numAces++;
                }
                else if (card == "J" || card == "Q" || card == "K")
                {
                    score += 10;
                }
                else
                {
                    score += int.Parse(card);
                }
            }

            // Calculate score with Aces
            for (int i = 0; i < numAces; i++)
            {
                if (score + 11 <= 21)
                {
                    score += 11;
                }
                else
                {
                    score += 1;
                }
            }

            return score;
        }

        // Helper method to wait for player response
        private static async Task<TwitchChatMessage> WaitForPlayerResponse(string sender, string channel)
        {
            var tcs = new TaskCompletionSource<TwitchChatMessage>();

            // Event handler for incoming messages
            TwitchChatEventHandler messageHandler = null;
            messageHandler = (s, e) =>
            {
                if (e.Sender == sender && e.Channel == channel)
                {
                    twitchBot.OnMessage -= messageHandler; // Remove the event handler
                    tcs.SetResult(e); // Complete the task when the expected message is received
                }
            };

            // Attach the event handler
            twitchBot.OnMessage += messageHandler;

            // Wait for the task to complete
            return await tcs.Task;
        }

        // BLACKJACK END


        private static SQLiteConnection _dbConnection;
        private static SQLiteConnection _lostMoneyDbConnection;
        private static TwitchBot twitchBot;
        private static void UpdateLostMoney(string sender, int lostAmount)
        {
            using (var command = new SQLiteCommand("INSERT INTO LostMoney (Username, LostAmount) VALUES (@Username, @LostAmount)", _lostMoneyDbConnection))
            {
                command.Parameters.AddWithValue("@Username", sender);
                command.Parameters.AddWithValue("@LostAmount", lostAmount);
                command.ExecuteNonQuery();
            }
        }

        private static int GetPoolAmount(string side)
        {
            string getTotalPoolAmountQuery = "SELECT SUM(Amount) FROM Bets WHERE Side = @side";
            using (var command = new SQLiteCommand(getTotalPoolAmountQuery, _dbConnection))
            {
                command.Parameters.AddWithValue("@side", side);
                var result = command.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
        private static async Task DeclareWinner(string winningSide, string channel)
        {
            // Calculate total bet amounts for both sides
            int totalBetAmountSideA = GetTotalBetAmount("sideA");
            int totalBetAmountSideB = GetTotalBetAmount("sideB");

            // Calculate total winnings (original bets returned to winners)
            int totalWinnings = totalBetAmountSideA + totalBetAmountSideB;

            if (totalWinnings == 0)
            {
                await twitchBot.SendMessage(channel, "No bets were placed on either side.");
                return;
            }

            // Calculate total bets lost on the losing side
            int totalBetAmountLosingSide = winningSide == "sideA" ? totalBetAmountSideB : totalBetAmountSideA;

            // Calculate winnings per winner (original bet returned plus share of losing side's bets)
            int winningsPerWinner = totalWinnings / GetTotalWinners(winningSide);

            // Distribute winnings to all winners
            DistributeWinnings(winningSide, winningsPerWinner, totalBetAmountLosingSide);

            // Clear the Bets table for the next round of betting
            ClearBets();

            await twitchBot.SendMessage(channel, $"Bets have been resolved and cleared. Congratulations to the winners!");
        }
        private static int GetTotalBetAmount(string side)
        {
            string getTotalBetAmountQuery = "SELECT SUM(Amount) FROM Bets WHERE Side = @side";
            using (var command = new SQLiteCommand(getTotalBetAmountQuery, _dbConnection))
            {
                command.Parameters.AddWithValue("@side", side);
                var result = command.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
        private static int GetTotalWinners(string winningSide)
        {
            string getTotalWinnersQuery = "SELECT COUNT(DISTINCT Username) FROM Bets WHERE Side = @winningSide";
            using (var command = new SQLiteCommand(getTotalWinnersQuery, _dbConnection))
            {
                command.Parameters.AddWithValue("@winningSide", winningSide);
                var result = command.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
        private static void DistributeWinnings(string winningSide, int winningsPerWinner, int totalBetAmountLosingSide)
        {
            string getUsersOnWinningSideQuery = "SELECT DISTINCT Username FROM Bets WHERE Side = @winningSide";
            using (var command = new SQLiteCommand(getUsersOnWinningSideQuery, _dbConnection))
            {
                command.Parameters.AddWithValue("@winningSide", winningSide);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string username = reader.GetString(0);
                        // Calculate total winnings for the winner
                        int totalWinningsForWinner = winningsPerWinner + (totalBetAmountLosingSide / GetTotalWinners(winningSide));
                        // Update user balance
                        UpdateUserBalance(username, GetUserBalance(username) + totalWinningsForWinner);
                    }
                }
            }
        }
        private static void ClearBets()
        {
            using (var command = new SQLiteCommand("DELETE FROM Bets", _dbConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        // Initialize database method
        private static void InitializeDatabase()
        {
            _dbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\JaiCoin.db;Version=3;");
            _dbConnection.Open();

            _lostMoneyDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\LostMoney.db;Version=3;");
            _lostMoneyDbConnection.Open();

            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS LostMoney (Username TEXT, LostAmount INTEGER)", _lostMoneyDbConnection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT, Balance INTEGER)", _dbConnection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Bets (Username TEXT, Side TEXT, Amount INTEGER)", _dbConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public async Task ChooseSide(string username, string side, int betAmount)
        {
            string insertOrUpdateQuery = "INSERT OR REPLACE INTO UserChoices (Username, Choice, BetAmount) VALUES (@username, @side, @betAmount)";
            using (SQLiteCommand command = new SQLiteCommand(insertOrUpdateQuery, _dbConnection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@side", side);
                command.Parameters.AddWithValue("@betAmount", betAmount);
                command.ExecuteNonQuery();
            }
        }

        public async Task SplitJaicoins(string winningSide)
        {
            string getUsersOnWinningSideQuery = "SELECT Username, BetAmount FROM UserChoices WHERE Choice = @winningSide";
            using (SQLiteCommand command = new SQLiteCommand(getUsersOnWinningSideQuery, _dbConnection))
            {
                command.Parameters.AddWithValue("@winningSide", winningSide);
                List<(string, int)> usersAndBets = new List<(string, int)>();
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string username = reader.GetString(0);
                        int betAmount = reader.GetInt32(1);
                        usersAndBets.Add((username, betAmount));
                    }
                }

                int totalJaicoins = usersAndBets.Sum(x => x.Item2);

                if (totalJaicoins == 0)
                {
                    // No bets placed
                    return;
                }

                foreach ((string username, int betAmount) in usersAndBets)
                {
                    // Calculate Jaicoins to distribute to each user
                    int jaicoinsToDistribute = (int)Math.Floor((double)betAmount / totalJaicoins * 1000);

                    // Update user balances in the database
                    string updateBalanceQuery = "INSERT OR REPLACE INTO UserBalances (Username, Balance) VALUES (@username, COALESCE((SELECT Balance FROM UserBalances WHERE Username = @username), 0) + @jaicoinsToDistribute)";
                    using (SQLiteCommand updateCommand = new SQLiteCommand(updateBalanceQuery, _dbConnection))
                    {
                        updateCommand.Parameters.AddWithValue("@username", username);
                        updateCommand.Parameters.AddWithValue("@jaicoinsToDistribute", jaicoinsToDistribute);
                        updateCommand.ExecuteNonQuery();
                    }
                }

                // Clear the UserChoices table for the next round of betting
                string clearUserChoicesQuery = "DELETE FROM UserChoices";
                using (SQLiteCommand clearCommand = new SQLiteCommand(clearUserChoicesQuery, _dbConnection))
                {
                    clearCommand.ExecuteNonQuery();
                }
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
        private static readonly TimeSpan WorkCooldown = TimeSpan.FromHours(0.5);
        private static readonly TimeSpan ThrowAssCooldown = TimeSpan.FromHours(0.5);
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
                   command.StartsWith("!reset4k") || command.StartsWith("!resetW") ||
                   command.StartsWith("!addcoins") || command.StartsWith("!bet") ||
                   command.StartsWith("!removecoins");
        }
        // !GIVE command 24h delay variable
        private static Dictionary<string, DateTime> lastGiveTimestamps = new Dictionary<string, DateTime>();
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
            InitializeDatabase();

            string password = File.ReadAllText("C:\\JaiBot Stuff\\OauthTEXT\\oauth.txt");
            string botUsername = "jais_pocket_dimension";
            twitchBot = new TwitchBot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();
            await twitchBot.JoinChannel("jai0899");
            //VIEWER COMMANDS
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");
                await Task.Delay(350);
                // CURRENCY COMMANDS
                int senderBalance = GetUserBalance(twitchChatMessage.Sender);

                if (twitchChatMessage.Message.StartsWith("!bj", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse the bet amount from the message
                    string[] splitMessage = twitchChatMessage.Message.Split(' ');
                    if (splitMessage.Length != 2 || !int.TryParse(splitMessage[1], out int betAmount) || betAmount <= 0)
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, please specify a valid bet amount.");
                        return;
                    }

                    // Call the method to handle the blackjack command with the specified bet amount
                    await HandleBlackjackCommand(twitchChatMessage.Sender, twitchChatMessage.Channel, betAmount);
                    return; // Exit the event handler after handling the command
                }

                if (twitchChatMessage.Message.StartsWith("!bet"))
                {
                    string[] splitMessage = twitchChatMessage.Message.Split(' ');
                    if (splitMessage.Length == 3 && (splitMessage[1] == "sideA" || splitMessage[1] == "sideB") && int.TryParse(splitMessage[2], out int betAmount))
                    {
                        if (betAmount > 0 && GetUserBalance(twitchChatMessage.Sender) >= betAmount)
                        {
                            // Deduct the bet amount from the user's balance
                            UpdateUserBalance(twitchChatMessage.Sender, GetUserBalance(twitchChatMessage.Sender) - betAmount);

                            // Store the bet in the database
                            using (var command = new SQLiteCommand("INSERT INTO Bets (Username, Side, Amount) VALUES (@username, @side, @amount)", _dbConnection))
                            {
                                command.Parameters.AddWithValue("@username", twitchChatMessage.Sender);
                                command.Parameters.AddWithValue("@side", splitMessage[1]);
                                command.Parameters.AddWithValue("@amount", betAmount);
                                command.ExecuteNonQuery();
                            }

                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender} placed {betAmount} JaiCoins on {splitMessage[1]}!");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, you don't have enough JaiCoins to place this bet.");
                        }
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, usage: !bet <sideA|sideB> <amount>");
                    }
                }

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
                if (twitchChatMessage.Message.StartsWith("!poolA"))
                {
                    int poolAmountA = GetPoolAmount("sideA");
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Total Jaicoins pooled for side A: {poolAmountA}");
                }
                if (twitchChatMessage.Message.StartsWith("!poolB"))
                {
                    int poolAmountB = GetPoolAmount("sideB");
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Total Jaicoins pooled for side B: {poolAmountB}");
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
                if (twitchChatMessage.Message.StartsWith("!balance") || twitchChatMessage.Message.StartsWith("!bal"))
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

                    if (twitchChatMessage.Message.StartsWith("!declare"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            string[] splitMessage = twitchChatMessage.Message.Split(' ');
                            if (splitMessage.Length == 2 && (splitMessage[1] == "sideA" || splitMessage[1] == "sideB"))
                            {
                                await DeclareWinner(splitMessage[1], twitchChatMessage.Channel);
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, usage: !declare <sideA|sideB>");
                            }
                        }
                    }

                    if (twitchChatMessage.Message.StartsWith("!addcoins"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender) && twitchChatMessage.Sender.ToLower() == "girlnamedluna")
                        {
                            string[] splitMessage = twitchChatMessage.Message.Split(' ');
                            if (splitMessage.Length == 3 && splitMessage[1].StartsWith("@") && int.TryParse(splitMessage[2], out int amount))
                            {
                                string recipient = splitMessage[1].Substring(1); // Remove the "@" symbol
                                                                                 // Check if the mentioned user exists and update their balance
                                int recipientBalance = GetUserBalance(recipient);
                                if (recipientBalance >= 0)
                                {
                                    UpdateUserBalance(recipient, recipientBalance + amount);
                                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, {amount} Jaicoins have been added to {recipient}'s balance.");
                                }
                                else
                                {
                                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, the mentioned user does not exist or has a negative balance.");
                                }
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, usage: !addcoins <@username> <amount>");
                            }
                        }
                    }
                    if (twitchChatMessage.Message.StartsWith("!removecoins"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender) && twitchChatMessage.Sender.ToLower() == "girlnamedluna")
                        {
                            string[] splitMessage = twitchChatMessage.Message.Split(' ');
                            if (splitMessage.Length == 3 && splitMessage[1].StartsWith("@") && int.TryParse(splitMessage[2], out int amount))
                            {
                                string recipient = splitMessage[1].Substring(1); // Remove the "@" symbol
                                                                                 // Check if the mentioned user exists and update their balance
                                int recipientBalance = GetUserBalance(recipient);
                                if (recipientBalance >= amount)
                                {
                                    UpdateUserBalance(recipient, recipientBalance - amount);
                                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, {amount} Jaicoins have been removed from {recipient}'s balance.");
                                }
                                else
                                {
                                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, the mentioned user does not have enough Jaicoins.");
                                }
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{twitchChatMessage.Sender}, usage: !removecoins <@username> <amount>");
                            }
                        }
                    }

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
