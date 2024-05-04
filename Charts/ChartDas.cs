using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Text.Json;
using TetrLoader;
using TetrLoader.Enum;
using TetrLoader.JsonClass.Event;

namespace Minomuncher.Charts;

public class ChartDas : ChartBase
{
	public ChartDas(string username, string json) : base("bar", "bar")
	{
		IReplayData replayData = ReplayLoader.ParseReplay(ref json,
			TetrLoader.Util.IsMulti(ref json) ? ReplayKind.TTRM : ReplayKind.TTR);
		var gameCount = replayData.GetGamesCount();

		Dictionary<KeyType, List<double>> keys = new();
		Dictionary<KeyType, double?> keyDownFrames = new();

		for (int gameIndex = 0; gameIndex < gameCount; gameIndex++)
		{
			var events = replayData.GetReplayEvents(username, gameIndex);
			foreach (var @event in events)
			{
				//それぞれのキーのkeydownからkeyupのフレームを計測、
				switch (@event.type)
				{
					case EventType.Keydown:
						var keyEvent = @event as EventKeyInput;
						if (!(keyEvent.data.key is KeyType.MoveRight or KeyType.MoveLeft))
						{
							continue;
						}

						keyDownFrames[@keyEvent.data.key] = (double)(keyEvent.frame + keyEvent.data.subframe);
						break;

					case EventType.Keyup:
						keyEvent = @event as EventKeyInput;
						if (!(keyEvent.data.key is KeyType.MoveRight or KeyType.MoveLeft))
						{
							continue;
						}

						if (keyDownFrames[@keyEvent.data.key] != null)
						{
							var frame = (double)(keyEvent.frame + keyEvent.data.subframe);
							if (!keys.ContainsKey(keyEvent.data.key))
								keys.Add(keyEvent.data.key, new List<double>());
							keys[keyEvent.data.key].Add(frame - (double)keyDownFrames[@keyEvent.data.key]);
						}

						keyDownFrames[@keyEvent.data.key] = null;
						break;
				}
			}
		}


		//とりま出力
		StreamWriter writer = new StreamWriter("test.csv");
		foreach (var key in keys)
		{
			foreach (var value in key.Value)
			{
				writer.WriteLine($"{key.Key},{value}");
			}
		}

		writer.Flush();
		writer.Close();
	}

	public override string SerializeChart()
		=> JsonSerializer.Serialize(this);
}