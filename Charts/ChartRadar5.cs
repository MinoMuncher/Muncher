﻿using System.Collections.Concurrent;
using System.Text.Json;
using Minomuncher.Commands;

namespace Minomuncher.Charts;

public class ChartRadar5 : ChartBase
{
	private const double EDGE_MULT = 1.2;
	public double[] min { get; private set; }
	public double[] max { get; private set; }
	public string[] formatTypes { get; private set; }

	public ChartRadar5(KeyValuePair<string, PlayerStats>[] playerStats,
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
				stats.app, stats.comboChainApp, stats.btbChainApp
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
			label = "APP",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 1.4 * EDGE_MULT
					: Max(max[0], max[1], max[2]) * EDGE_MULT
				: norm == MunchModule.Normalization.Average
					? 1.4
					: 3,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Combo Chain App",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 3 * EDGE_MULT
					: Max(max[0], max[1], max[2]) * EDGE_MULT
				: 3,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "B2B Chain APP",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 1.6 * EDGE_MULT
					: Max(max[0], max[1], max[2]) * EDGE_MULT
				: norm == MunchModule.Normalization.Average
					? 1.6
					: 3,
			formatType = "toFixed2"
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