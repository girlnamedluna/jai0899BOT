using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using System.Diagnostics;

namespace JaiBot
{
    //StopWatchDebugging
    class StopWatch
    {
        public StopWatch()
        {
            Process currentProcess = Process.GetCurrentProcess();
            DateTime startedAt = currentProcess.StartTime;
            DateTime stopAt = currentProcess.ExitTime;
            Console.WriteLine("Process started at: " + startedAt);
            Console.WriteLine("Process stopped at: " + stopAt);
            Console.ReadLine();
        }
    }

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
        public delegate void TwitchChatEventHandler (object sender, TwitchChatMessage e);
        
        public class TwitchChatMessage : EventArgs
        {
            public string Sender { get; set;}
            public string Message { get; set;}
            public string Channel { get; set;}
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
            streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n" , AutoFlush = true };

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

    class Program
    {
        static async Task Main(string[] args)
        {
            string password = File.ReadAllText("C:\\OauthTEXT\\oauth.txt");
            string botUsername = "jais_pocket_dimension";

            var twitchBot = new TwitchBot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();
            await twitchBot.JoinChannel("jai0899");
            //await twitchBot.SendMessage("jai0899", "Bot initalized");

            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");
                if (twitchChatMessage.Message.StartsWith("!hey"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Hey there {twitchChatMessage.Sender}");
                }
                if (twitchChatMessage.Message.StartsWith("!welcome"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "Welcome to the stream! My name is Jai, I mainly play Dead By Daylight");
                }
                //if (twitchChatMessage.Message.StartsWith("!discord"))
                //{
                //    await twitchBot.SendMessage(twitchChatMessage.Channel, "https://discord.gg/33ADgxyBPu");
                //}
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
            };
            await Task.Delay(-1);
        }
    }
}
