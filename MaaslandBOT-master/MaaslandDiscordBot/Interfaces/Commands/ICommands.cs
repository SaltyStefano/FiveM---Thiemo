namespace MaaslandDiscordBot.Interfaces.Commands
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Discord;
    using Discord.WebSocket;
    using MaaslandDiscordBot.Models.BOT;

    public interface ICommands
    {
        string[] GetCommands { get; }

        bool IsFunction(string function);

        Task ExecuteCommand(string command, string[] args, SocketMessage message, DiscordSocketClient discordClient);

        Task ActionHandler(
            Dictionary<string, string> players,
            IUserMessage message,
            MessageStore messageStore);
    }
}
