using Discord;
using Discord.Commands;

namespace Minomuncher.Commands;

public class PingModule : ModuleBase<SocketCommandContext>
{
	[Command("ping")]
	public async Task EchoAsync([Remainder] string text = "")
	{
		DateTime dt = DateTime.ParseExact(Context.Message.CreatedAt.ToString(), "yyyy/MM/dd HH:mm:ss zzz", null);
		TimeSpan diff = DateTime.Now - dt;

		await Context.Message.ReplyAsync($"pong! ({diff.TotalMilliseconds}ms) {Context.Message.CreatedAt.ToString()}");
	}
}