using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Minomuncher.Commands;

public class StopModule : ModuleBase<SocketCommandContext>
{
	[Command("stop")]
	public async Task HelpAsync([Remainder] string text = "")
	{
		if (text == string.Empty)
			text = "bot stopped";

		await Context.Message.ReplyAsync($"{text} {Context.User.Id}");
	}
}