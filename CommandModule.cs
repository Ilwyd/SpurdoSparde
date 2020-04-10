using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Net;
using Discord.Commands;
using Discord;
using Discord.Addons.Preconditions;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using HtmlAgilityPack;

namespace Spurdoo {
    [Ratelimit(1, 0.167, Measure.Minutes, RatelimitFlags.NoLimitForAdmins)]
    public class CommandModule : ModuleBase {

        static TimeSpan time = TimeSpan.FromSeconds(10);

        //Acquiresparde command
        [Command("acquiresparde"), Summary("Info on bot support")]
        public async Task AcquireSparde() {
            string getspurdobot = "Use this invite link to get the bot in your server: <https://discordapp.com/oauth2/authorize?client_id=441803321209126932&scope=bot&permissions=0>\n"
                                + "You can also join our support server to make suggestions and report bugs with this link: <https://discord.gg/G97GgWd>\n"
                                + "As well you can vote for the bot here: <https://discordbots.org/bot/441803321209126932>";

            await ReplyAsync(getspurdobot);
            CommandMethods.CommandCount("acquiresparde");
        }

        //Spurdo command with no given number
        [Command("spurdo", RunMode = RunMode.Async), Summary("Responds with a random Spurdo Sparde image")]
        public async Task Spurdo([Remainder, Summary("")] string msg = "") {
            //Get a random image
            string file = CommandMethods.GetRandomFile("./pics");
            
            //Make sure a file was actually selected
            if(file != null) {
                await Context.Channel.SendFileAsync(file);
            }
            else {
                string error = "fugg xDDD an error oggured, bls tell the bot mayger Ilwyd#1743 lol";
                await Context.Channel.SendMessageAsync(error);
            }

            CommandMethods.CommandCount("spurdo");
        }

        //Runescape merchant command
        [Command("merch", RunMode = RunMode.Async), Summary("Returns today's merchant stock")]
        public async Task Merch() {
            //Creating a webclient and getting the HTML text back from the page
            WebClient webClient = new WebClient();
            string page = webClient.DownloadString("https://runescape.wiki/w/Template:Travelling_Merchant");

            //Loading the text as HTML
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);

            //Getting the date from the page
            var date = doc.DocumentNode.SelectSingleNode("//div[@class='mw-parser-output']/p/i").InnerText;
            
            //Putting the table into a 2d List
            List<List<string>> table = doc.DocumentNode.SelectSingleNode("//table[@class='wikitable align-center-1 align-center-4']")
                .Descendants("tr")
                .Skip(1)
                .Where(tr=>tr.Elements("td").Count()>1)
                .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                .ToList();
            
            //Building the message to return
            EmbedBuilder embed = new EmbedBuilder();
            EmbedAuthorBuilder embedAuthor = new EmbedAuthorBuilder();

            //Building the author
            embedAuthor.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
            embedAuthor.Name = date;
            embedAuthor.Url = "https://runescape.wiki/w/Template:Travelling_Merchant";
            embedAuthor.Build();

            //Building the footer
            EmbedFooterBuilder embedFooter = new EmbedFooterBuilder();
            embedFooter.Text = "Incorrect date? Click the title and then click \"wrong?\" on the wiki page to force update it.";

            //Building the embed
            string name = "";
            string cost = "";
            //string quantity = "";
            string description = "";
            foreach(List<string> innerList in table) {
                name += innerList.ElementAt(1) + "\n";
                cost += innerList.ElementAt(2);
                cost +=  " (" + innerList.ElementAt(3) + ")\n";
                description += innerList.ElementAt(4) + "\n";
            }
            
            embed.AddField("Item", name, true);
            embed.AddField("Cost (Quantity)", cost, true);
            //embed.AddInlineField("Quantity", quantity);
            //embed.AddInlineField("Descriptions", description);
            embed.Author = embedAuthor;
            embed.Color = Color.Blue;
            embed.Footer = embedFooter;

            await ReplyAsync("", false, embed.Build());
        }
    }




    [RequireOwner]
    public class OwnerCommandModule : ModuleBase {
        //BELOW ARE OWNER ONLY COMMANDS
        //Info command
        [Command("halb"), Summary("Returns various info on the bot")]
        public async Task Halb() {
            var guilds = Context.Client.GetGuildsAsync().Result;
            var guildCount = guilds.Count;
            var userCount = 0;
            var start = DateTime.Now;
            EmbedBuilder embed = new EmbedBuilder();
            EmbedAuthorBuilder embedAuthor = new EmbedAuthorBuilder();
            EmbedFooterBuilder embedFooter = new EmbedFooterBuilder();
            
            //Getting info from discord bots list API
            IEnumerable<string> listtokens = System.IO.File.ReadLines("./botlisttokens");
            AuthDiscordBotListApi DblApi = new AuthDiscordBotListApi(ulong.Parse(listtokens.ElementAt(0)), listtokens.ElementAt(1));
            IDblSelfBot me = await DblApi.GetMeAsync();
            var votes = me.Points;
            var description = me.ShortDescription;
            var library = me.LibraryUsed;
            var prefix = me.PrefixUsed;
            var aqcuireCount = System.IO.File.ReadAllText("./acquiresparde.txt");
            var spurdoCount = System.IO.File.ReadAllText("./spurdo.txt");
            var commandCounts = "Spurdo: " + spurdoCount + "\nAcquire: " + aqcuireCount;

            //Getting the user count
            foreach(var item in guilds) {
                if(!(item.Id == 264445053596991498)) {
                    userCount += item.GetUsersAsync().Result.Count;
                }
            }

            //Building the author
            embedAuthor.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
            embedAuthor.Name = Context.Client.CurrentUser.Username;
            embedAuthor.Url = "https://discordbots.org/bot/441803321209126932";
            embedAuthor.Build();

            //Building the embed
            embed.Author = embedAuthor;
            embed.Title = "Info";
            embed.Description = description;
            embed.AddField("Total Guilds", guildCount, true);
            embed.AddField("Total Users", userCount, true);
            embed.AddField("DBL Points", votes, true);
            embed.AddField("Library", library, true);
            embed.AddField("Prefix", prefix, true);
            embed.AddField("Commands used since Dec-18-2018", commandCounts, true);

            var totalTime = (DateTime.Now - start).TotalSeconds.ToString();
            
            //Building the footer
            embedFooter.Text = "Built in " + totalTime + " seconds.";
            embedFooter.Build();

            //Adding the footer to the embed
            embed.Footer = embedFooter;

            //Sending the response
            await ReplyAsync("", false, embed.Build());
        }

        //Addimg command
        [Command("addimg"), Summary("Adds the given attachments to the pics folder")]
        public async Task Addimg() {
            //Array of all attachments
            var imageLinks = Context.Message.Attachments.ToArray();

            foreach(IAttachment imageLink in imageLinks)
            {
                //New guid for image name
                var directory = new DirectoryInfo("./pics");
                var guid = Guid.NewGuid();

                //Getting the url of each image
                var url = imageLink.Url.ToString();

                //Getting proper extension
                var extension = url.Substring(url.Length-3);

                //Creating the name and location for pic
                var filename = guid + "." + extension;

                Console.WriteLine("Adding img as: " + filename);
                        
                using(var webClient = new WebClient())
                {
                    //Download the file
                    webClient.DownloadFile(url, "./pics/" + filename);
                }
                await Context.Message.DeleteAsync();
            }
        }
    }




    public class CommandMethods {
        //BELOW ARE NON COMMAND METHODS
        //Get a random file from the given directory
        public static string GetRandomFile(string path) {
            string file = null;
            if(!string.IsNullOrEmpty(path)) {
                //Setting usable file extensions
                var extensions = new string[] {".png", ".jpg", ".gif"};
                
                try {
                    //Getting the directory as an object
                    var directory = new DirectoryInfo(path);

                    //Make sure the directory exists you tard lol eggs deee
                    if(directory.Exists) {
                        //Finding all files with appropriate extensions
                        var rgFiles = directory.GetFiles("*.*").Where(f => extensions.Contains(f.Extension.ToLower()));
                        
                        //Selecting a random appropriate file
                        Random r = new Random();
                        file = rgFiles.ElementAt(r.Next(0, rgFiles.Count())).FullName;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("\n\nError during GetRandomFile(" + path + "): \n\n" + e.StackTrace);
                }
            }
            return file;
        }

        //Get a specific file from the given directory
        public static string GetSpecificFile(string path, int num) {
            string file = null;

            if(!string.IsNullOrEmpty(path)) {
                try {
                    //Getting the directory as an object
                    var directory = new DirectoryInfo(path);

                    if(directory.Exists) {
                        file = directory.GetFiles(num + ".*").ElementAt(0).FullName;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("\n\nError during GetSpecificFile(" + path + ", " + num + "): \n\n" + e);
                }
            }
            return file;
        }

        //Count the use of a command
        public static void CommandCount(string command) {
            var file = "./" + command + ".txt";
            var fileText = System.IO.File.ReadAllText(file);
            int count = int.Parse(fileText);

            int newCount = count + 1;
            string text = newCount.ToString();
            System.IO.File.WriteAllText(file, text);
        }
    }
}