using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Minomuncher.Commands;

public class ResumeModule : ModuleBase<SocketCommandContext>
{
	[Command("resume")]
	public async Task ResumeAsync([Remainder] string text = "")
	{

		await Context.Message.ReplyAsync($"bot resumed!");
	}
}