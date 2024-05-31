using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Minomuncher.Charts;

namespace Minomuncher.Commands;

public class DasModule : ModuleBase<SocketCommandContext>
{
	[Command("das")]
	public async Task DasAsync(params string[]? players)
	{
		try
		{
			Dictionary<string, List<string>> playerGames = new();
			List<string> errors = new List<string>();

			var message = await Context.Message.ReplyAsync("initializing munching process");


			if (Context.Message.Attachments.Count > 0)
			{
				throw new NotImplementedException();
			}
			else
			{
				try
				{
					await Util.GetPlayerGames(players.ToArray(), errors, message, playerGames,0);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
					await message.ModifyAsync(properties =>
						properties.Content = e.Message);
					return;
				}
			}

			foreach (var player in playerGames)
			{
				ChartDas das = new ChartDas(player.Key, player.Value[0]);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}
}