using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Minomuncher.Charts;
using TetrEnvironment.Constants;
using TetrEnvironment.Info;
using TetrLoader;
using TetrLoader.Enum;
using TetrLoader.JsonClass.Raw;

namespace Minomuncher.Commands;

public class MunchModule : ModuleBase<SocketCommandContext>
{
	public enum Normalization
	{
		Average,
		Stat
	}

	[DllImport("evaluator.dll")]
	private static extern string analyze(IntPtr[] arr, int size);

	[Command("munch")]
	public async Task MunchAsync(string? playersText = "")
	{
		int munchRemaining = 0;
		Dictionary<string, List<string>> playerGames = new();


		if (string.IsNullOrEmpty(playersText))
		{
			var username = (await TetrioAPI.SearchUserbyDsicordId(Context.User.Id))?.username;
			if (username == null)
			{
				await Context.Message.ReplyAsync(
					$"Couldn't find the user, link your account to TETR.IO or use use !munch <username> instead.");

				return;
			}

			playersText = username;
		}

		var players = playersText!.Split(" ");

		var message = await Context.Message.ReplyAsync("initializing munching process");

		if (Context.Message.Attachments.Count > 0)
		{
			//ユーザーのゲームリストに追加、ゲーム自体は同じだから同じ者を渡す
			using HttpClient client = new HttpClient();
			foreach (var attachment in Context.Message.Attachments)
			{
				var game = await client.GetStringAsync(attachment.Url);
				IReplayData replayData =
					ReplayLoader.ParseReplay(ref game, Util.IsMulti(ref game) ? ReplayKind.TTRM : ReplayKind.TTR);

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
			//ファイル添付の場合は別

			//名前を渡してゲーム一覧を取得
			Dictionary<string, List<Record>> playerGameIds = new();


			foreach (var player in players)
			{
				Console.WriteLine($"fetching TL games:{player}");
				var gamesList = await FetchGameListAsync(player);
				munchRemaining += gamesList.Count;
				playerGameIds.Add(player, gamesList);
			}

			await message.ModifyAsync(properties =>
				properties.Content = $"fetched {munchRemaining} TL games from {string.Join(",", players)}\n" +
				                     $"downloading...");


			foreach (var playerGameList in playerGameIds)
			{
				Console.WriteLine($"downloading TL games:{playerGameList.Key}");
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
						Console.WriteLine(e.ToString() + "\nfailed to download:" + record.replayid);
					}
				}

				playerGames.Add(playerGameList.Key, games);
				//例外をキャッチした場合はidを控えてスキップ
				//processedCountをfinallyで増やして、gameCountで終了判定
			}
		}

		message.ModifyAsync(properties =>
			properties.Content = $"analyzing");

		ConcurrentDictionary<string, PlayerStats> playerStats = new();

		Parallel.ForEach(playerGames, player =>
		{
			ManualResetEvent taskCompleted = new ManualResetEvent(false);
			ConcurrentBag<string> customStats = new ConcurrentBag<string>();

			int munchLeft = player.Value.Count;
			//TL games
			//TODO: ゲームなかったらreturn
			foreach (var game in player.Value)
			{
				ThreadPool.QueueUserWorkItem(state =>
				{
					var states = ((string name, string game))state;
					PlayerStats stats = null;

					foreach (var rawStats in Munch.Munch.ProcessReplay(states.name, states.game))
						customStats.Add(JsonSerializer.Serialize(rawStats));

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
			var statsJson = analyze(ptrs, ptrs.Length);

			var stats = JsonSerializer.Deserialize<PlayerStats>(statsJson);
			playerStats.AddOrUpdate(player.Key, stats, (Key, Value) => Value);

			foreach (var p in ptrs)
				Marshal.FreeHGlobal(p);

			message.ModifyAsync(properties =>
				properties.Content = $"generating graphs");
			//	playerStats[states.name].Add(stats);
			//playerStats.AddOrUpdate(states.name, stats, (Key, Value) => Value);
		});

		//createLineClearGraph
		//chartjs-webapiの方で結合する
		//新しいapiを追加
		List<string> sendImages = new List<string>();
		foreach (var player in playerStats)
		{
			var chart = new ChartBar(player.Key, player.Value);
			var base64img = await chart.DownloadChart(800, 800);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartWell(player.Key, player.Value, false);
			var base64img = await chart.DownloadChart(800, 800);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartRadar1(player.Key, player.Value, false, Normalization.Average);
			var base64img = await chart.DownloadChart(200, 200);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartRadar2(player.Key, player.Value, false, Normalization.Average);
			var base64img = await chart.DownloadChart(200, 200);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartRadar3(player.Key, player.Value, false, Normalization.Average);
			var base64img = await chart.DownloadChart(200, 200);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartRadar4(player.Key, player.Value, false, Normalization.Average);
			var base64img = await chart.DownloadChart(200, 200);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartRadar5(player.Key, player.Value, false, Normalization.Average);
			var base64img = await chart.DownloadChart(200, 200);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		foreach (var player in playerStats)
		{
			var chart = new ChartRadar6(player.Key, player.Value, false, Normalization.Average);
			var base64img = await chart.DownloadChart(200, 200);
			sendImages.Add(base64img);
			Console.WriteLine();
		}

		await message.DeleteAsync();
		List<FileAttachment> attachments = new();
		foreach (var imageBase64 in sendImages)
		{
			var imageBytes = Convert.FromBase64String(imageBase64.Split(",")[1]);
			MemoryStream stream = new MemoryStream(imageBytes);
			attachments.Add(new FileAttachment(stream, "output.png"));
		}

		var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(playerStats));
		MemoryStream jsonStream = new MemoryStream(jsonBytes);
		attachments.Add(new FileAttachment(jsonStream, "rawStats.json"));

		MessageReference refMessage = new MessageReference(Context.Message.Id);
		await Context.Channel.SendFilesAsync(attachments, "parsed", messageReference: refMessage);
		//		GC.Collect();
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
}