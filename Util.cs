using Discord;
using ProcessMunch.Exception;
using TetrLoader.JsonClass.Raw;

namespace Minomuncher;

public static class Util
{
	/// <summary>
	/// get player replays in args
	/// </summary>
	/// <param name="players"></param>
	public static async Task GetPlayerGames(string[] players, List<string> errors,
		IUserMessage message, Dictionary<string, List<string>> playerGames)
	{
		int munchGames = 0;
		Dictionary<string, List<Record>> playerGameIds = new();

		foreach (var player in players)
		{
			List<Record> gamesList;
			try
			{
				gamesList = await FetchGameListAsync(player);
			}
			catch
			{
				throw new MunchException($"failed to fetch user from {string.Join(",", players)}");
			}

			munchGames += gamesList.Count;
			playerGameIds.Add(player, gamesList);
		}

		await message.ModifyAsync(properties =>
			properties.Content = $"fetched {munchGames} TL games from {JoinWithQuotes(",", players)}\n" +
			                     $"downloading...");


		foreach (var playerGameList in playerGameIds)
		{
			var games = new List<string>();
			//ダウンロードと処理
			for (var i = 0; i < playerGameList.Value.Count; i++)
			{
				var record = playerGameList.Value[i];
				await message.ModifyAsync(properties =>
					properties.Content = $"fetched {munchGames} TL games from {JoinWithQuotes(",", players)}\n" +
					                     $"downloading...({i + 1}/{playerGameList.Value.Count}) {record.replayid}");

				try
				{
					games.Add(await TetrioAPI.GetReplayFromIdAsync(record.replayid));
				}
				catch (Exception e)
				{
					errors.Add($"{record.replayid} is not exists");
				}
			}

			playerGames.Add(playerGameList.Key, games);
		}
	}

	/// <summary>
	/// 引数のユーザーのそれぞれ過去10試合のゲームをダウンロード
	/// </summary>
	/// <param name="players"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<List<Record>> FetchGameListAsync(string player)
	{
		var user = await TetrioAPI.DownloadUserAsync(player);
		return (await TetrioAPI.DownloadListOfGameIds(user._id)).ToList();
	}

	public static string JoinWithQuotes(string separator, IEnumerable<string> values)
	{
		return string.Join(separator, values.Select(v => $"`{v}`"));
	}
}