﻿using System.Text.Json;
using Minomuncher.Commands;

namespace Minomuncher.Charts;

public class ChartRadar3 : ChartBase
{
	private const double EDGE_MULT = 1.2;
	public double[] min { get; private set; }
	public double[] max { get; private set; }
	public string[] formatTypes { get; private set; }

	public ChartRadar3(string username,
		PlayerStats playerStats,
		bool scale,
		MunchModule.Normalization norm) : base(
		"radar", "radar")
	{
		double[] radarData = new double[]
		{
			playerStats.stackHeight, playerStats.averageSpikePotential, playerStats.garbageHeight,
			playerStats.averageDefencePotential, playerStats.blockfishScore
		};

		double scaleMax = 0;
		double scaleMin = double.MaxValue;
		double[] scales = new double[radarData.Length];

		foreach (var scaleValue in scales)
		{
			scaleMax = Math.Max(scaleMax, scaleValue);
			scaleMin = Math.Min(scaleMin, scaleValue);
		}


		List<RadarConfig> template = new();
		template.Add(new RadarConfig()
		{
			label = "Stack Height",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 10 * EDGE_MULT
					: Max(max[0], max[2]) * EDGE_MULT
				: 10,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Board Spike Potential",
			min = 0,
			max = scale
				? scaleMax * 0.12 * EDGE_MULT
				: 0.12,
			formatType = "percentageToFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Garbage Height",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 10 * EDGE_MULT
					: Max(max[0], max[2]) * EDGE_MULT
				: 10,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Board Defence Potential",
			min = scale ? (Math.Min(0, scaleMin) * 6 + 17) : 17,
			max = scale ? (scaleMax * 6 * EDGE_MULT + 17) : 23,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Pieces To Garbage",
			min = scale ? (Math.Min(0, scaleMin) * 2 + 1) : 1,
			max = scale ? (scaleMax * 2 * EDGE_MULT + 1) : 3,
			formatType = "toFixed2"
		});

		this.min = new double[radarData.Length];
		this.max = new double[radarData.Length];
		for (int i = 0; i < radarData.Length; i++)
		{
			scales[i] = NormalizeStat(radarData[i], template[i].min, template[i].max);
			this.min[i] = template[i].min;
			this.max[i] = template[i].max;
		}

		data.labels = template.Select(x => x.label).ToList();
		formatTypes = template.Select(x => x.formatType).ToArray();

		data.datasets.Add(new Dataset()
		{
			label = username,
			data = scales
		});
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