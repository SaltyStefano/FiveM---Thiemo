namespace MaaslandDiscordBot.Commands.General
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.WebSocket;

    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Models.BOT;

    public class HelpCommands : BaseCommands
    {
        public override string Function => "help";

        public HelpCommands()
        {
            RegisterCommand("-help", Help);
        }

        public async Task Help(string[] arguments, SocketMessage message)
        {
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                    Name = $"MaaslandBOT Commands"
                },
                Color = Color.Blue,
                Footer = new EmbedFooterBuilder
                {
                    Text = "MAASLANDBOT COMMANDS"
                },
                Fields = new List<EmbedFieldBuilder>(),
                Timestamp = DateTimeOffset.Now,
            };

            embed.Fields.AddRange(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-help",
                    Value = "Het weergeven van alle commands"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-player [player]",
                    Value = "Het opzoeken van een speler op de FiveM server"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-history [player]",
                    Value = "Het opzoeken van login activiteit van een speler"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-ids [player]",
                    Value = "Identifiers van een speler opzoeken"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-weapons [player]",
                    Value = "Alle wapens van een speler opzoeken"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-inv [player]",
                    Value = "Alle items die een persoon op zak heeft"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-job [society]",
                    Value = "Organisatie opzoeken op basis van naam"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-log",
                    Value = "Het loggen van data uit de database, beschikbare opties zijn: weapons"
                },
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "-discord [player]",
                    Value = "Discord van een speler opzoeken"
                },
            });

            await message.Channel.SendMessageAsync(message.Author.Mention, false, embed.Build());
        }

        public override Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            return Task.CompletedTask;
        }
    }
}
