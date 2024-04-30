using Discord;
using Discord.Commands;

namespace Minomuncher.Commands;

public class PingModule : ModuleBase<SocketCommandContext>
{
	[Command("ping")]
	public async Task EchoAsync()
	{
		DateTime dt = DateTime.ParseExact("2024/04/30 13:52:06 +00:00", "yyyy/MM/dd HH:mm:ss zzz", null);
		TimeSpan diff = DateTime.Now - dt;

		await Context.Message.ReplyAsync($"pong! ({diff.Milliseconds}ms)");
	}
}