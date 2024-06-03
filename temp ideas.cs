using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Controller.TwitchBot;

namespace Controller
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

    public class YourClass
    {
        private static Dictionary<string, DateTime> lastWorkTimestamps = new Dictionary<string, DateTime>();
        private static Dictionary<string, DateTime> lastThrowAssTimestamps = new Dictionary<string, DateTime>();
        private static Dictionary<string, DateTime> lastGiveTimestamps = new Dictionary<string, DateTime>();
        private static readonly TimeSpan WorkCooldown = TimeSpan.FromHours(1);
        private static readonly TimeSpan ThrowAssCooldown = TimeSpan.FromHours(1);
        private static int FourCounter = 0;
        private static int winsCounter = 0;
        private static int survivedCounter = 0;
        private static int deathsCounter = 0;

        public async Task YourMethod(TwitchBot twitchBot)
        {
            //AUTHORIZED USERS COMMANDS
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");

                // Your existing logic for handling chat commands
                if (IsCounterCommand(twitchChatMessage.Message))
                {
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

                    // Add your other authorized user commands here
                }
                //DONT FUCK WITH THIS  
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

        private static int GetUserBalance(string username)
        {
            // Implement logic to get user balance from database or elsewhere
            return 0;
        }

        private static void UpdateUserBalance(string username, int newBalance)
        {
            // Implement logic to update user balance in the database or elsewhere
        }

        private static bool IsCounterCommand(string message)
        {
            // Implement logic to check if the message is a counter command
            return true;
        }

        private static async Task DeclareWinner(string side, string channel)
        {
            // Implement logic to declare winner for the specified side
            await Task.CompletedTask;
        }
    }
}
