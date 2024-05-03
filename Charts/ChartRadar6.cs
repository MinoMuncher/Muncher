using System.Collections.Concurrent;
using System.Text.Json;
using Minomuncher.Commands;

namespace Minomuncher.Charts;

public class ChartRadar6 : ChartBase
{
	private const double EDGE_MULT = 1.2;
	public double[] min { get; private set; }
	public double[] max { get; private set; }
	public string[] formatTypes { get; private set; }

	public ChartRadar6(KeyValuePair<string, PlayerStats>[] playerStats,
		bool scale,
		MunchModule.Normalization norm) : base(
		"radar", "radar")
	{
		double scaleMax = 0;
		Dictionary<string, double[]> scalePlayers = new();

		foreach (var player in playerStats)
		{
			var stats = player.Value;
			double[] radarData = new double[]
			{
				stats.pps, stats.kps, stats.attackDelayRate, stats.burstPps, stats.kpp,
				stats.preAttackDelayRate
			};
			scalePlayers.Add(player.Key, radarData);
		}

		foreach (var stats in scalePlayers.Values)
		{
			foreach (var scaleValue in stats)
			{
				scaleMax = Math.Max(scaleMax, scaleValue);
			}
		}


		List<RadarConfig> template = new();
		template.Add(new RadarConfig()
		{
			label = "PPS",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 5 * EDGE_MULT
					: Max(max[0], max[3]) * EDGE_MULT
				: norm == MunchModule.Normalization.Average
					? 5
					: 10,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Keys Per Second",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 22 * EDGE_MULT
					: Max(max[0], max[1], max[2]) * EDGE_MULT
				: 22,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Attack Delay Rate",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 0.2 * EDGE_MULT
					: Max(max[2], max[5]) * EDGE_MULT
				: 0.2,
			formatType = "percentageToFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Burst PPS",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 10 * EDGE_MULT
					: Max(max[0], max[3]) * EDGE_MULT
				: 10,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "keys Per Piece",
			min = 0,
			max = scale
				? scaleMax * 7 * EDGE_MULT
				: 7,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Tank Before Attack Rate",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 0.15 * EDGE_MULT
					: Max(max[2], max[5]) * EDGE_MULT
				: 0.15,
			formatType = "percentageToFixed2"
		});

		this.min = new double[template.Count];
		this.max = new double[template.Count];
		foreach (var player in scalePlayers)
		{
			for (int i = 0; i < template.Count; i++)
			{
				scalePlayers[player.Key][i] =
					NormalizeStat(scalePlayers[player.Key][i], template[i].min, template[i].max);
				this.min[i] = template[i].min;
				this.max[i] = template[i].max;
			}

			data.datasets.Add(new Dataset()
			{
				label = player.Key,
				data = scalePlayers[player.Key]
			});
		}

		data.labels = template.Select(x => x.label).ToList();
		formatTypes = template.Select(x => x.formatType).ToArray();
	}


	public static double Max(params double[] array)
	{
		double maxValue = double.MinValue;

		foreach (double value in array)
		{
			if (maxValue < value)
				maxValue = value;
		}

		return maxValue;
	}


	public double NormalizeStat(double value, double min, double max)
	{
		var range = max - min;
		var truncatedValue = value - min;
		return truncatedValue / range;
	}


	public override string SerializeChart()
		=> JsonSerializer.Serialize(this);
}