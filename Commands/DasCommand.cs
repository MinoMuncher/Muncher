using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Minomuncher.Commands;

public class DasModule : ModuleBase<SocketCommandContext>
{
	[Command("das")]
	public async Task DasAsync(params string[]? players)
	{
		if (Context.Message.Attachments.Count > 0)
		{
			/*using HttpClient client = new HttpClient();

			foreach (var attachment in Context.Message.Attachments)
			{
				var game = await client.GetStringAsync(attachment.Url);
			
				foreach (var user in replayData.GetUsernames())
				{
					if (!playerGames.ContainsKey(user))
						playerGames.Add(user, new List<string>());

					playerGames[user].Add(game);
				}
			}*/
		}
	}
}