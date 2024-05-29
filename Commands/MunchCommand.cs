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
	private bool _noImgMode = false;

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
		bool scale;
		bool normalize;
		int games;
		bool league;

		List<string> players = new List<string>();
		ParseArgs(args, out players, out scale, out normalize, out games, out league);
		players = args.ToList();

		try
		{
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
				try
				{
					await Util.GetPlayerGames(players.ToArray(), errors, message, playerGames);
				}
				catch (Exception e)
				{
					await message.ModifyAsync(properties =>
						properties.Content = e.Message);
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
							errors.Add(e.Message);
							//	message.ModifyAsync(properties =>
							//		properties.Content = e.Message);
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

			if (!_noImgMode)
			{
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
			}

			await message.DeleteAsync();
			var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(playerStatsArray));
			MemoryStream jsonStream = new MemoryStream(jsonBytes);
			attachments.Add(new FileAttachment(jsonStream, "rawStats.json"));

			MessageReference refMessage = new MessageReference(Context.Message.Id);
			var resultMessage = "parsed\n";
			if (_noImgMode)
				resultMessage += "skipped generating images\n";

			foreach (var error in errors)
			{
				resultMessage += error + "\n";
			}

			foreach (var player in playerStatsArray)
			{
				float totalClears = player.Value.clearTypes.GetTotalClearsExceptNone();

				var glickoPredData = new GlickoEstimator.ModelInput()
				{
					Single = player.Value.clearTypes.SINGLE / totalClears,
					Double = player.Value.clearTypes.DOUBLE / totalClears,
					Triple = player.Value.clearTypes.TRIPLE / totalClears,
					Quad = player.Value.clearTypes.QUAD / totalClears,
					Tspin = player.Value.clearTypes.TSPIN / totalClears,
					TspinSingle = player.Value.clearTypes.TSPIN_SINGLE / totalClears,
					TspinDouble = player.Value.clearTypes.TSPIN_DOUBLE / totalClears,
					TspinTriple = player.Value.clearTypes.TSPIN_TRIPLE / totalClears,
					TspinMini = player.Value.clearTypes.TSPIN_MINI / totalClears,
					TspinMiniSingle = player.Value.clearTypes.TSPIN_MINI_SINGLE / totalClears,
					//	TspinMiniDouble = player.Value.clearTypes.TSPIN_MINI_DOUBLE / totalClears,
					PerfectClear = player.Value.clearTypes.PERFECT_CLEAR / totalClears,
					TEfficiency = (float)Math.Min(Math.Max(0, player.Value.tEfficiency), 1),
					IEfficiency = (float)Math.Min(Math.Max(0, player.Value.iEfficiency), 1),
					CheeseApl = (float)Math.Min(Math.Max(0, player.Value.cheeseApl / 5f), 1),
					DownStackAPL = (float)Math.Min(Math.Max(0, player.Value.downstackApl / 5f), 1),
					UpStackAPL = (float)Math.Min(Math.Max(0, player.Value.upstackApl / 5f), 1),
					APL = (float)Math.Min(Math.Max(0, player.Value.apl / 5f), 1),
					APP = (float)Math.Min(Math.Max(0, player.Value.app / 5f), 1),
					KPP = (float)Math.Min(Math.Max(0, player.Value.kpp / 5f), 1),
					KPS = (float)Math.Min(Math.Max(0, player.Value.kps / 20f), 1),
					StackHeight = (float)Math.Min(Math.Max(0, player.Value.stackHeight / 20f), 1),
					GarbageHeight = (float)Math.Min(Math.Max(0, player.Value.garbageHeight / 20f), 1),
					SpikeEfficiency = (float)Math.Min(Math.Max(0, player.Value.spikeEfficiency / 1f), 1),
					APM = (float)Math.Min(Math.Max(0, player.Value.apm / 300f), 1),
					OpenerAPM = (float)Math.Min(Math.Max(0, player.Value.openerApm / 500f), 1),
					MidGameAPM = (float)Math.Min(Math.Max(0, player.Value.midgameApm / 300f), 1),
					PPS = (float)Math.Min(Math.Max(0, player.Value.pps / 5f), 1),
					OpenerPPS = (float)Math.Min(Math.Max(0, player.Value.openerPps / 5f), 1),
					MidGamePPS = (float)Math.Min(Math.Max(0, player.Value.midgamePps / 5f), 1),
					BTBChainEfficiency = (float)Math.Min(Math.Max(0, player.Value.btbChainEfficiency / 1f), 1),
					BTBChain = (float)Math.Min(Math.Max(0, player.Value.btbChain / 20f), 1),
					BTBChainAPM = (float)Math.Min(Math.Max(0, player.Value.btbChainApm / 500f), 1),
					BTBChainAttack = (float)Math.Min(Math.Max(0, player.Value.btbChainAttack / 100f), 1),
					BTBChainAPP = (float)Math.Min(Math.Max(0, player.Value.btbChainApp / 5f), 1),
					BTBChainEfficiency2 = (float)Math.Min(Math.Max(0, player.Value.comboChainEfficiency / 1f), 1),
					ComboChain = (float)Math.Min(Math.Max(0, player.Value.comboChain / 10f), 1),
					ComboChainAPM = (float)Math.Min(Math.Max(0, player.Value.comboChainEfficiency / 500f), 1),
					ComboChainAttack = (float)Math.Min(Math.Max(0, player.Value.comboChainAttack / 50f), 1),
					ComboChainAPP = (float)Math.Min(Math.Max(0, player.Value.comboChainApp / 5f), 1),
					AverageSpikePotential = (float)Math.Min(Math.Max(0, player.Value.averageSpikePotential / 1f), 1),
					AverageDefencePotential =
						(float)Math.Min(Math.Max(0, player.Value.averageDefencePotential / 30f), 1),
					BlockfishScore = (float)Math.Min(Math.Max(0, player.Value.blockfishScore / 20f), 1),
					BurstPPS = (float)Math.Min(Math.Max(0, player.Value.burstPps / 10f), 1),
					AttackDelayRate = (float)Math.Min(Math.Max(0, player.Value.attackDelayRate / 1f), 1),
					PreAttackDelayRate = (float)Math.Min(Math.Max(0, player.Value.preAttackDelayRate / 1f), 1),
				};
				var glickoResult = GlickoEstimator.Predict(glickoPredData);

				var predData = new TREstimator.ModelInput()
				{
					Glicko = glickoResult.Score,
					Single = player.Value.clearTypes.SINGLE / totalClears,
					Double = player.Value.clearTypes.DOUBLE / totalClears,
					Triple = player.Value.clearTypes.TRIPLE / totalClears,
					Quad = player.Value.clearTypes.QUAD / totalClears,
					Tspin = player.Value.clearTypes.TSPIN / totalClears,
					TspinSingle = player.Value.clearTypes.TSPIN_SINGLE / totalClears,
					TspinDouble = player.Value.clearTypes.TSPIN_DOUBLE / totalClears,
					TspinTriple = player.Value.clearTypes.TSPIN_TRIPLE / totalClears,
					TspinMini = player.Value.clearTypes.TSPIN_MINI / totalClears,
					TspinMiniSingle = player.Value.clearTypes.TSPIN_MINI_SINGLE / totalClears,
					//	TspinMiniDouble = player.Value.clearTypes.TSPIN_MINI_DOUBLE / totalClears,
					PerfectClear = player.Value.clearTypes.PERFECT_CLEAR / totalClears,
					TEfficiency = (float)Math.Min(Math.Max(0, player.Value.tEfficiency), 1),
					IEfficiency = (float)Math.Min(Math.Max(0, player.Value.iEfficiency), 1),
					CheeseApl = (float)Math.Min(Math.Max(0, player.Value.cheeseApl / 5f), 1),
					DownStackAPL = (float)Math.Min(Math.Max(0, player.Value.downstackApl / 5f), 1),
					UpStackAPL = (float)Math.Min(Math.Max(0, player.Value.upstackApl / 5f), 1),
					APL = (float)Math.Min(Math.Max(0, player.Value.apl / 5f), 1),
					APP = (float)Math.Min(Math.Max(0, player.Value.app / 5f), 1),
					KPP = (float)Math.Min(Math.Max(0, player.Value.kpp / 5f), 1),
					KPS = (float)Math.Min(Math.Max(0, player.Value.kps / 20f), 1),
					StackHeight = (float)Math.Min(Math.Max(0, player.Value.stackHeight / 20f), 1),
					GarbageHeight = (float)Math.Min(Math.Max(0, player.Value.garbageHeight / 20f), 1),
					SpikeEfficiency = (float)Math.Min(Math.Max(0, player.Value.spikeEfficiency / 1f), 1),
					APM = (float)Math.Min(Math.Max(0, player.Value.apm / 300f), 1),
					OpenerAPM = (float)Math.Min(Math.Max(0, player.Value.openerApm / 500f), 1),
					MidGameAPM = (float)Math.Min(Math.Max(0, player.Value.midgameApm / 300f), 1),
					PPS = (float)Math.Min(Math.Max(0, player.Value.pps / 5f), 1),
					OpenerPPS = (float)Math.Min(Math.Max(0, player.Value.openerPps / 5f), 1),
					MidGamePPS = (float)Math.Min(Math.Max(0, player.Value.midgamePps / 5f), 1),
					BTBChainEfficiency = (float)Math.Min(Math.Max(0, player.Value.btbChainEfficiency / 1f), 1),
					BTBChain = (float)Math.Min(Math.Max(0, player.Value.btbChain / 20f), 1),
					BTBChainAPM = (float)Math.Min(Math.Max(0, player.Value.btbChainApm / 500f), 1),
					BTBChainAttack = (float)Math.Min(Math.Max(0, player.Value.btbChainAttack / 100f), 1),
					BTBChainAPP = (float)Math.Min(Math.Max(0, player.Value.btbChainApp / 5f), 1),
					BTBChainEfficiency2 = (float)Math.Min(Math.Max(0, player.Value.comboChainEfficiency / 1f), 1),
					ComboChain = (float)Math.Min(Math.Max(0, player.Value.comboChain / 10f), 1),
					ComboChainAPM = (float)Math.Min(Math.Max(0, player.Value.comboChainEfficiency / 500f), 1),
					ComboChainAttack = (float)Math.Min(Math.Max(0, player.Value.comboChainAttack / 50f), 1),
					ComboChainAPP = (float)Math.Min(Math.Max(0, player.Value.comboChainApp / 5f), 1),
					AverageSpikePotential = (float)Math.Min(Math.Max(0, player.Value.averageSpikePotential / 1f), 1),
					AverageDefencePotential =
						(float)Math.Min(Math.Max(0, player.Value.averageDefencePotential / 30f), 1),
					BlockfishScore = (float)Math.Min(Math.Max(0, player.Value.blockfishScore / 20f), 1),
					BurstPPS = (float)Math.Min(Math.Max(0, player.Value.burstPps / 10f), 1),
					AttackDelayRate = (float)Math.Min(Math.Max(0, player.Value.attackDelayRate / 1f), 1),
					PreAttackDelayRate = (float)Math.Min(Math.Max(0, player.Value.preAttackDelayRate / 1f), 1),
				};


				var trResult = TREstimator.Predict(predData);

				resultMessage += $"{player.Key}'s estimated glicko:{glickoResult.Score}, tr:{trResult.Score}" + "\n";
			}


//Load model and predict output


			await Context.Channel.SendFilesAsync(attachments, resultMessage, messageReference: refMessage);
			//		GC.Collect();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}


	private void ParseArgs(string[] args,
		out List<string> players,
		out bool scale,
		out bool normalize,
		out int games,
		out bool league)
	{
		scale = false;
		normalize = false;
		games = int.MaxValue;
		league = false;


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
						scale = true;
						break;

					case "-o":
						//combined to default
						break;

					case "-n":
						normalize = true;
						break;

					case "-g":
						i++;
						games = int.Parse(args[i]);
						break;

					case "-l":
						league = true;
						break;
				}
			}
			else
			{
				players.Add(args[i]);
			}
		}
	}
}