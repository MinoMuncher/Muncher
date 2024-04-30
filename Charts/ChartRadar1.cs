using System.Text.Json;
using Minomuncher.Commands;

namespace Minomuncher.Charts;

public class ChartRadar1 : ChartBase
{
	private const double EDGE_MULT = 1.2;
	public double[] min { get; private set; }
	public double[] max { get; private set; }
	public string[] formatTypes { get; private set; }

	public ChartRadar1(string username,
		PlayerStats playerStats,
		bool scale,
		MunchModule.Normalization norm) : base(
		"radar", "radar")
	{
		double[] radarData = new double[]
		{
			playerStats.pps, playerStats.openerApm, playerStats.openerPps, playerStats.apm, playerStats.midgamePps,
			playerStats.midgameApm
		};

		double scaleMax = 0;
		double[] scales = new double[radarData.Length];

		foreach (var scaleValue in scales)
			scaleMax = Math.Max(scaleMax, scaleValue);

		List<RadarConfig> template = new();
		template.Add(new RadarConfig()
		{
			label = "PPS",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 5 * EDGE_MULT
					: Max(max[0], max[2], max[4]) * EDGE_MULT
				: 5,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Opener APM",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 250 * EDGE_MULT
					: Max(max[1], max[3], max[5]) * EDGE_MULT
				: 250,
			formatType = "round"
		});
		template.Add(new RadarConfig()
		{
			label = "Opener PPS",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 5 * EDGE_MULT
					: Max(max[0], max[2], max[4]) * EDGE_MULT
				: 5,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "APM",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 250 * EDGE_MULT
					: Max(max[1], max[3], max[5]) * EDGE_MULT
				: 250,
			formatType = "round"
		});
		template.Add(new RadarConfig()
		{
			label = "Midgame PPS",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 5 * EDGE_MULT
					: Max(max[0], max[2], max[4]) * EDGE_MULT
				: 5,
			formatType = "toFixed2"
		});
		template.Add(new RadarConfig()
		{
			label = "Midgame APM",
			min = 0,
			max = scale
				? norm == MunchModule.Normalization.Average
					? scaleMax * 250 * EDGE_MULT
					: Max(max[1], max[3], max[5]) * EDGE_MULT
				: 250,
			formatType = "round"
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