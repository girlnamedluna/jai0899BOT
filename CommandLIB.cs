using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static CommandLIB.TwitchBot;

namespace CommandLIB
{
    public class TwitchBot
    {
        const string ip = "irc.chat.twitch.tv";
        const int port = 6667;

        private string nick;
        private string password;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private TaskCompletionSource<int> connected = new TaskCompletionSource<int>();

        public event TwitchChatEventHandler OnMessage = delegate { };
        public delegate void TwitchChatEventHandler(object sender, TwitchChatMessage e);

        public class TwitchChatMessage : EventArgs
        {
            public string Sender { get; set; }
            public string Message { get; set; }
            public string Channel { get; set; }
        }

        public TwitchBot(string nick, string password)
        {
            this.nick = nick;
            this.password = password;
        }

        public async Task Start()
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };

            await streamWriter.WriteLineAsync($"PASS {password}");
            await streamWriter.WriteLineAsync($"NICK {nick}");
            connected.SetResult(0);

            while (true)
            {
                string line = await streamReader.ReadLineAsync();
                Console.WriteLine(line);

                string[] split = line.Split(" ");
                if (line.StartsWith("PING"))
                {
                    Console.WriteLine("PING");
                    await streamWriter.WriteLineAsync($"PONG {split[1]}");
                }
                if (split.Length > 1 && split[1] == "PRIVMSG")
                {
                    int exclmationPointPosition = split[0].IndexOf("!");
                    string username = split[0].Substring(1, exclmationPointPosition - 1);
                    int secondColonPosition = line.IndexOf(':', 1);
                    string message = line.Substring(secondColonPosition + 1);
                    string channel = split[2].TrimStart('#');

                    OnMessage(this, new TwitchChatMessage
                    {
                        Message = message,
                        Channel = channel,
                        Sender = username
                    });
                }
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            await connected.Task;
            await streamWriter.WriteLineAsync($"PRIVMSG #{channel} :{message}");
        }

        public async Task JoinChannel(string channel)
        {
            await connected.Task;
            await streamWriter.WriteLineAsync($"JOIN #{channel}");
        }
    }

    public class TwitchCommandHandler
    {
        private static SQLiteConnection _dbConnection;
        private static SQLiteConnection _deathsDbConnection;
        private static SQLiteConnection _survivedDbConnection;
        private static SQLiteConnection _winsDbConnection;
        private static SQLiteConnection _fourKDbConnection;
        private static SQLiteConnection _lostMoneyDbConnection;

        // Method to get all user balances
        private static Dictionary<string, int> GetAllUserBalances(SQLiteConnection dbConnection)
        {
            Dictionary<string, int> userBalances = new Dictionary<string, int>();

            using (var command = new SQLiteCommand("SELECT Username, Balance FROM UserBalances", dbConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string username = reader["Username"].ToString();
                        int balance = Convert.ToInt32(reader["Balance"]);
                        userBalances.Add(username, balance);
                    }
                }
            }

            return userBalances;
        }

        public static void PrintAllUserBalancesCommand(SQLiteConnection dbConnection)
        {
            // Call the method to print all user balances
            PrintAllUserBalances(dbConnection);
        }

        public static void PrintAllUserBalances(SQLiteConnection dbConnection)
        {
            // Get all user balances
            Dictionary<string, int> userBalances = GetAllUserBalances(dbConnection);

            // Print each user's name and balance to the console
            foreach (var userBalance in userBalances)
            {
                Console.WriteLine($"User: {userBalance.Key}, Balance: {userBalance.Value} JaiCoins");
            }
        }

        private static void UpdateLostMoney(string username, int lostAmount, SQLiteConnection lostMoneyDbConnection)
        {
            using (var command = new SQLiteCommand("INSERT INTO LostMoney (Username, LostAmount) VALUES (@username, @lostAmount)", lostMoneyDbConnection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@lostAmount", lostAmount);
                command.ExecuteNonQuery();
            }
        }

        // Initialize database method
        public static void InitializeDatabase()
        {
            _dbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\JaiCoin.db;Version=3;");
            _dbConnection.Open();

            _deathsDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\DeathsCounter.db;Version=3;");
            _deathsDbConnection.Open();
            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _deathsDbConnection))
            {
                command.ExecuteNonQuery();
            }

            _lostMoneyDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\LostMoney.db;Version=3;");
            _lostMoneyDbConnection.Open();
            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS LostMoney (Username TEXT, LostAmount INTEGER)", _lostMoneyDbConnection))
            {
                command.ExecuteNonQuery();
            }

            // Create database file for survived counter
            _survivedDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\SurvivedCounter.db;Version=3;");
            _survivedDbConnection.Open();
            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _survivedDbConnection))
            {
                command.ExecuteNonQuery();
            }

            // Create database file for wins counter
            _winsDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\WinsCounter.db;Version=3;");
            _winsDbConnection.Open();
            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _winsDbConnection))
            {
                command.ExecuteNonQuery();
            }

            // Create database file for 4k counter
            _fourKDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\FourKCounter.db;Version=3;");
            _fourKDbConnection.Open();
            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _fourKDbConnection))
            {
                command.ExecuteNonQuery();
            }

            // Create table if not exists
            using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _dbConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public async Task HandleMessage(TwitchChatMessage twitchChatMessage)
        {
            // Your logic here...
            // Make sure to replace 'twitchBot' and 'twitchChatMessage' with the appropriate parameters passed to this method.
            // Also, ensure any other referenced variables or methods are accessible or passed as parameters.
        }
    }
}
