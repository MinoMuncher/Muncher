using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Minomuncher.Commands;

public class StopModule : ModuleBase<SocketCommandContext>
{
	[Command("stop")]
	public async Task HelpAsync([Remainder] string text = "")
	{
		if (Context.User.Id != Program.DISCORD_BOT_ADMIN)
			return;

		if (text == string.Empty)
			text = "bot stopped";

		await Context.Message.ReplyAsync($"{text} {Context.User.Id}");
	}
}