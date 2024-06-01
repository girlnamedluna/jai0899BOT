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
//Thank you @thisduckisnotentitled_0 for helping debug this code <3

namespace JaiBot
{
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

    public class FollowerResponse
    {
        public List<FollowerData> data { get; set; }
    }

    public class FollowerData
    {
        public string followed_at { get; set; }
    }

    class Program
    {
        // FOLLOWER AGE CODE
        private static async Task<int?> GetFollowAge(string viewerName, string channelName, string oauthToken)
        {
            try
            {
                Console.WriteLine($"Viewer: {viewerName}, Channel: {channelName}, OAuth Token: {oauthToken}");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Client-ID", "gp762nuuoqcoxypju8c569th9wz7q5");
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer oauth:twbuijxho1801zkhbv3byz3qhefi6v");

                    string apiUrl = $"https://api.twitch.tv/helix/users/follows?from_name={viewerName.ToLower()}&to_name={channelName}";
                    Console.WriteLine($"API URL: {apiUrl}");

                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                    Console.WriteLine($"Response Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadFromJsonAsync<FollowerResponse>();
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response Content: {responseContent}");

                        if (responseData?.data != null && responseData.data.Count > 0)
                        {
                            DateTime followedAt = DateTime.Parse(responseData.data[0].followed_at);
                            Console.WriteLine($"Followed At: {followedAt}");
                            TimeSpan followAge = DateTime.UtcNow - followedAt;
                            Console.WriteLine($"Follow Age: {followAge.Days}");
                            return followAge.Days;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"User or channel not found.");
                    }
                    else
                    {
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex}");
                Console.WriteLine($"Inner Exception: {ex.InnerException}");
            }
            return null;
        }

        private static int survivedCounter = 0;
        private static int deathsCounter = 0;
        private static int winsCounter = 0;
        private static int FourCounter = 0;

        // AUTHORIZED COMMANDS LIST
        private static bool IsCounterCommand(string command)
        {
            return command.StartsWith("!setdeaths") || command.StartsWith("!setsurvived")    ||
                   command.EndsWith("!s")           || command.StartsWith("!d")              ||
                   command.StartsWith("!resetS")    || command.StartsWith("!resetD")         ||
                   command.StartsWith("!setwins")   || command.EndsWith("!w")                ||
                   command.StartsWith("!set4k")     || command.EndsWith("!4k")               ||
                   command.StartsWith("!plus4k")    || command.EndsWith("!wins");
        }

        static async Task Main(string[] args)
        {
            // BOT INITALIZATION
            string password = File.ReadAllText("C:\\OauthTEXT\\oauth.txt");
            //string oauthToken = File.ReadAllText("C:\\OauthTEXT\\oauth.txt");
            string botUsername = "jais_pocket_dimension";

            var twitchBot = new TwitchBot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();
            await twitchBot.JoinChannel("jai0899");
           
            //VIEWER COMMANDS
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");

                if (twitchChatMessage.Message.StartsWith("!deaths"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has died {deathsCounter} trial(s) in a row! :(");
                }
                if (twitchChatMessage.Message.StartsWith("!survived"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has survived {survivedCounter} trial(s) in a row!");
                }
                if (twitchChatMessage.Message.StartsWith("!4k"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has had {FourCounter} 4k's in a row!");
                }
                if (twitchChatMessage.Message.EndsWith("!wins"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai has had {winsCounter} win(s) in a row!");
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
                if (twitchChatMessage.Message.Contains("Luna"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "Luna? @girlnamedluna ! That's the person who made me!");
                }

                // HELP COMMANDS
                if (twitchChatMessage.Message.StartsWith("!help"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "!welcome, !deaths, !wins, !4k, !hibaby, !dancedance, !gg, !ez, !shoot [@user], !hug [@user]");
                }
                if (twitchChatMessage.Message.StartsWith("!modhelp"))
                {
                    await twitchBot.SendMessage(twitchChatMessage.Channel, "!setdeaths [value], !set4k [value], !setwins [value], !setsurvived [value], !plus4k, !w, !s, !d");
                }

                // FOLLOW AGE COMMAND
                if (twitchChatMessage.Message.StartsWith("!followage"))
                {
                    string viewerName = twitchChatMessage.Sender;
                    string channelName = "jai0899"; // Change this to your channel name
                    string oauthToken = File.ReadAllText("C:\\OauthTEXT\\oauth.txt");

                    int? followAgeDays = await GetFollowAge(viewerName, channelName, oauthToken);

                    if (followAgeDays.HasValue)
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{viewerName}, you have been following for {followAgeDays} days.");
                    }
                    else
                    {
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"@{viewerName}, you are not following the channel.");
                    }
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
                {

                    // SET DEATH COUNT
                    if (twitchChatMessage.Message.StartsWith("!setdeaths"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            string[] parts = twitchChatMessage.Message.Split(' ');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int newCounterValue))
                            {
                                deathsCounter = newCounterValue;
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"Counter set to: {deathsCounter}");
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, "Invalid command format, please use !setdeaths [value]");
                            }
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // INCREASE KILLER 4K COUNT
                    else if (twitchChatMessage.Message.StartsWith("!plus4k"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            FourCounter++;
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai got another 4k! He is now at {FourCounter} 4k's in a row!");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // SET KILLER WINS COUNT
                    else if (twitchChatMessage.Message.StartsWith("!set4k"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            string[] parts = twitchChatMessage.Message.Split(' ');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int newCounterValue))
                            {
                                FourCounter = newCounterValue;
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"4k counter updated :)");
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, "Invalid command format, please use !set4k [value]");
                            }
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // INCREASE KILLER WINS COUNT
                    else if (twitchChatMessage.Message.EndsWith("!w"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            winsCounter++;
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Jai got another win! He is now at {winsCounter} killer wins in a row!");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // SET KILLER WINS COUNT
                    else if (twitchChatMessage.Message.StartsWith("!setwins"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            string[] parts = twitchChatMessage.Message.Split(' ');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int newCounterValue))
                            {
                                winsCounter = newCounterValue;
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"Killer wins counter updated :)");
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, "Invalid command format, please use !setwins [value]");
                            }
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // SET SURVIVED COUNT
                    else if (twitchChatMessage.Message.StartsWith("!setsurvived"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            string[] parts = twitchChatMessage.Message.Split(' ');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int newCounterValue))
                            {
                                survivedCounter = newCounterValue;
                                await twitchBot.SendMessage(twitchChatMessage.Channel, $"Survived counter updated :)");
                            }
                            else
                            {
                                await twitchBot.SendMessage(twitchChatMessage.Channel, "Invalid command format, please use !setsurvived [value]");
                            }
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // INCREASE SURVIVED COUNT
                    else if (twitchChatMessage.Message.EndsWith("!s"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            survivedCounter++;
                            deathsCounter = 0;
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Trials survived in a row: {survivedCounter} :)");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }

                    // INCREASE DEATH COUNT
                    else if (twitchChatMessage.Message.EndsWith("!d"))
                    {
                        if (IsAuthorized(twitchChatMessage.Sender))
                        {
                            deathsCounter++;
                            survivedCounter = 0;
                            await twitchBot.SendMessage(twitchChatMessage.Channel, $"Trials died in a row: {deathsCounter} :(");
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "You are not authorized to use this command.");
                        }
                    }
                }
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
