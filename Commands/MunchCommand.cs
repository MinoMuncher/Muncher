using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.Commands;
using Minomuncher.Charts;
using ProcessMunch.Exception;
using TetrLoader;
using TetrLoader.Enum;
using TetrLoader.JsonClass.Raw;

namespace Minomuncher.Commands;

public class MunchModule : ModuleBase<SocketCommandContext>
{
	private readonly Dictionary<string, string> MODIFIER_ALIAS = new Dictionary<string, string>
	{
		{ "--scale", "-s" },
		{ "--normalize", "-n" },
		{ "--order", "-o" },
		{ "--games", "-g" },
		{ "--league", "-l" }
	};

	public enum Normalization
	{
		Average,
		Stat
	}

	[Command("munch")]
	public async Task MunchAsync(params string[]? args)
	{
		List<string> players = new List<string>();
//		ParseArgs(args, out players);
		players = args.ToList();

		try
		{
			int munchRemaining = 0;
			Dictionary<string, List<string>> playerGames = new();
			List<string> errors = new List<string>();

			//playerの指定がなかったらDiscordIdから検索
			if (players == null || players.Count == 0)
			{
				var username = (await TetrioAPI.SearchUserbyDsicordId(Context.User.Id))?.username;
				if (username == null)
				{
					await Context.Message.ReplyAsync(
						$"Couldn't find the user, link your account to TETR.IO or use use !munch <username> instead.");

					return;
				}

				players = new List<string> { username };
			}

			var message = await Context.Message.ReplyAsync("initializing munching process");

			if (players.Count > 10)
			{
				await message.ModifyAsync(properties =>
					properties.Content = $"up to 10 players");
				return;
			}


			if (Context.Message.Attachments.Count > 0)
			{
				//ユーザーのゲームリストに追加、ゲーム自体は同じだから同じゲームを追加
				using HttpClient client = new HttpClient();

				foreach (var attachment in Context.Message.Attachments)
				{
					var game = await client.GetStringAsync(attachment.Url);
					IReplayData replayData =
						ReplayLoader.ParseReplay(ref game,
							TetrLoader.Util.IsMulti(ref game) ? ReplayKind.TTRM : ReplayKind.TTR);

					foreach (var user in replayData.GetUsernames())
					{
						if (!playerGames.ContainsKey(user))
							playerGames.Add(user, new List<string>());

						playerGames[user].Add(game);
					}
				}
			}
			else
			{
				Dictionary<string, List<Record>> playerGameIds = new();

				foreach (var player in players)
				{
					var gamesList = await FetchGameListAsync(player);
					munchRemaining += gamesList.Count;
					playerGameIds.Add(player, gamesList);
				}

				await message.ModifyAsync(properties =>
					properties.Content = $"fetched {munchRemaining} TL games from {string.Join(",", players)}\n" +
					                     $"downloading...");


				foreach (var playerGameList in playerGameIds)
				{
					//		Console.WriteLine($"downloading TL games:{playerGameList.Key}");
					var games = new List<string>();
					//ダウンロードと処理
					foreach (var record in playerGameList.Value)
					{
						try
						{
							games.Add(await TetrioAPI.GetReplayFromIdAsync(record.replayid));
						}
						catch (System.Exception e)
						{
							errors.Add($"{record.replayid} is not exists");
							Console.WriteLine(e.ToString() + "\nfailed to download:" + record.replayid);
						}
					}

					playerGames.Add(playerGameList.Key, games);
				}
			}

			await message.ModifyAsync(properties =>
				properties.Content = $"analyzing");

			ConcurrentDictionary<string, PlayerStats> playerStats = new();

			ParallelOptions parallelOptions = new ParallelOptions();
			parallelOptions.MaxDegreeOfParallelism = 20;
			Parallel.ForEach(playerGames, parallelOptions, player =>
			{
				ManualResetEvent taskCompleted = new ManualResetEvent(false);
				ConcurrentBag<string> customStats = new ConcurrentBag<string>();

				int munchLeft = player.Value.Count;

				foreach (var game in player.Value)
				{
					ThreadPool.QueueUserWorkItem(state =>
					{
						var states = ((string name, string game))state;
						PlayerStats stats = null;

						try
						{
							foreach (var rawStats in Munch.Munch.ProcessReplay(states.name, states.game, errors))
								customStats.Add(JsonSerializer.Serialize(rawStats));
						}
						catch (MunchException e)
						{
							message.ModifyAsync(properties =>
								properties.Content = e.Message);

							return;
						}

						Interlocked.Decrement(ref munchLeft);
						if (munchLeft == 0)
							taskCompleted.Set();
					}, (player.Key, game));
				}

				taskCompleted.WaitOne();
				var rawStatsJson = customStats.ToArray();

				IntPtr[] ptrs = new IntPtr[rawStatsJson.Length];
				for (var i = 0; i < ptrs.Length; i++)
					ptrs[i] = (Marshal.StringToHGlobalAnsi(rawStatsJson[i]));
				var statsJson = NativeMethod.NativeMethod.Analyze(ptrs, ptrs.Length);

				var stats = JsonSerializer.Deserialize<PlayerStats>(statsJson);
				playerStats.AddOrUpdate(player.Key, stats, (Key, Value) => Value);

				foreach (var p in ptrs)
					Marshal.FreeHGlobal(p);
			});


			if (playerStats.Count == 0)
			{
				await message.ModifyAsync(properties =>
					properties.Content = $"more than one player has no valid replay");

				return;
			}

			await message.ModifyAsync(properties =>
				properties.Content = $"generating graphs");

			KeyValuePair<string, PlayerStats>[] playerStatsArray = playerStats.ToArray();

			//order
			for (var i = 0; i < players.Count; i++)
			{
				if (playerStatsArray[i].Key != players[i])
				{
					var index = players.FindIndex(x => x == playerStatsArray[i].Key);
					if (index < 0)
						break;
					(playerStatsArray[i], playerStatsArray[index])
						= (playerStatsArray[index], playerStatsArray[i]);
				}
			}


			List<string> sendImages = new List<string>();
			List<FileAttachment> attachments = new();
			try
			{
				{
					List<ChartBar> chartBars = new List<ChartBar>();

					foreach (var player in playerStatsArray)
					{
						var chart = new ChartBar(player.Key, player.Value);
						chartBars.Add(chart);
					}

					var json = JsonSerializer.Serialize(chartBars);
					var base64img = await ChartBase.DownloadChart(json, 800, 800);
					sendImages.Add(base64img);
				}

				{
					List<ChartWell> chartWells = new List<ChartWell>();

					foreach (var player in playerStatsArray)
					{
						var chart = new ChartWell(player.Key, player.Value, false);
						chartWells.Add(chart);
					}

					var json = JsonSerializer.Serialize(chartWells);
					var base64img =
						await ChartBase.DownloadChart(json, 800, 800);
					sendImages.Add(base64img);
				}

				{
					var chart = new ChartRadar1(playerStatsArray, false, Normalization.Average);
					var base64img = await chart.DownloadChart(800, 800);
					sendImages.Add(base64img);
				}

				{
					var chart = new ChartRadar2(playerStatsArray, false, Normalization.Average);
					var base64img = await chart.DownloadChart(800, 800);
					sendImages.Add(base64img);
				}

				{
					var chart = new ChartRadar3(playerStatsArray, false, Normalization.Average);
					var base64img = await chart.DownloadChart(200, 200);
					sendImages.Add(base64img);
				}

				{
					var chart = new ChartRadar4(playerStatsArray, false, Normalization.Average);
					var base64img = await chart.DownloadChart(200, 200);
					sendImages.Add(base64img);
				}

				{
					var chart = new ChartRadar5(playerStatsArray, false, Normalization.Average);
					var base64img = await chart.DownloadChart(200, 200);
					sendImages.Add(base64img);
				}

				{
					var chart = new ChartRadar6(playerStatsArray, false, Normalization.Average);
					var base64img = await chart.DownloadChart(200, 200);
					sendImages.Add(base64img);
				}


				foreach (var imageBase64 in sendImages)
				{
					var imageBytes = Convert.FromBase64String(imageBase64.Split(",")[1]);
					MemoryStream stream = new MemoryStream(imageBytes);
					attachments.Add(new FileAttachment(stream, "output.png"));
				}
			}
			catch
			{
				await message.ModifyAsync(properties =>
					properties.Content = $"failed to generate graphs");

				return;
			}

			await message.DeleteAsync();
			var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(playerStatsArray));
			MemoryStream jsonStream = new MemoryStream(jsonBytes);
			attachments.Add(new FileAttachment(jsonStream, "rawStats.json"));

			MessageReference refMessage = new MessageReference(Context.Message.Id);
			var resultMessage = "parsed\n";

			foreach (var error in errors)
			{
				resultMessage += error + "\n";
			}

			await Context.Channel.SendFilesAsync(attachments, resultMessage, messageReference: refMessage);
			//		GC.Collect();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}

	/// <summary>
	/// 引数のユーザーのそれぞれ過去10試合のゲームをダウンロード
	/// </summary>
	/// <param name="players"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	private async Task<List<Record>> FetchGameListAsync(string player)
	{
		var user = await TetrioAPI.DownloadUserAsync(player);
		return (await TetrioAPI.DownloadListOfGameIds(user._id)).ToList();
	}

	private void ParseArgs(string[] args, out List<string> players)
	{
		for (int i = 0; i < args.Length; i++)
		{
			if (MODIFIER_ALIAS.ContainsKey(args[i]))
				args[i] = MODIFIER_ALIAS[args[i]];
		}

		players = new List<string>();

		for (int i = 0; i < args.Length; i++)
		{
			if (MODIFIER_ALIAS.ContainsValue(args[i]))
			{
				switch (args[i])
				{
					case "-s":

						break;

					case "-o":
						//combined to default
						break;

					case "-n":

						break;
				}
			}
		}
	}
}