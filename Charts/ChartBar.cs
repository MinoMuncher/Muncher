using System.Text.Json;

namespace Minomuncher.Charts;

public class ChartBar : ChartBase
{
	public ChartBar(string username, PlayerStats playerStats) : base("bar", "bar")
	{
		var total = playerStats.clearTypes.GetTotalClears();
		data.labels = new List<string>() { username };


		data.datasets.Add(new Dataset()
		{
			label =
				$"Singles {playerStats.clearTypes.SINGLE / (double)total * 100:0.00}% ({playerStats.clearTypes.SINGLE})",
			data = new[] { playerStats.clearTypes.SINGLE / (double)total * 100 },
			backgroundColor = "#dd8480",
			rawData = playerStats.clearTypes.SINGLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Doubles {playerStats.clearTypes.DOUBLE / (double)total * 100:0.00}% ({playerStats.clearTypes.DOUBLE})",
			data = new[] { playerStats.clearTypes.DOUBLE / (double)total * 100 },
			backgroundColor = "#e8b699",
			rawData = playerStats.clearTypes.DOUBLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Triples {playerStats.clearTypes.TRIPLE / (double)total * 100:0.00}% ({playerStats.clearTypes.TRIPLE})",
			data = new[] { playerStats.clearTypes.TRIPLE / (double)total * 100 },
			backgroundColor = "#f3e6b2",
			rawData = playerStats.clearTypes.TRIPLE
		});
		data.datasets.Add(new Dataset()
		{
			label = $"Quads {playerStats.clearTypes.QUAD / (double)total * 100:0.00}% ({playerStats.clearTypes.QUAD})",
			data = new[] { playerStats.clearTypes.QUAD / (double)total * 100 },
			backgroundColor = "#83b2d0",
			rawData = playerStats.clearTypes.QUAD
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Tspin Mini Singles {playerStats.clearTypes.TSPIN_MINI_SINGLE / (double)total * 100:0.00}% ({playerStats.clearTypes.TSPIN_MINI_SINGLE})",
			data = new[] { playerStats.clearTypes.TSPIN_MINI_SINGLE / (double)total * 100 },
			backgroundColor = "#8685cf",
			rawData = playerStats.clearTypes.TSPIN_MINI_SINGLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Tspin Mini Doubles {playerStats.clearTypes.TSPIN_MINI_DOUBLE / (double)total * 100:0.00}% ({playerStats.clearTypes.TSPIN_MINI_DOUBLE})",
			data = new[] { playerStats.clearTypes.TSPIN_MINI_DOUBLE / (double)total * 100 },
			backgroundColor = "#875acb",
			rawData = playerStats.clearTypes.TSPIN_MINI_DOUBLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Tspin Singles {playerStats.clearTypes.TSPIN_SINGLE / (double)total * 100:0.00}% ({playerStats.clearTypes.TSPIN_SINGLE})",
			data = new[] { playerStats.clearTypes.TSPIN_SINGLE / (double)total * 100 },
			backgroundColor = "#f7c9da",
			rawData = playerStats.clearTypes.TSPIN_SINGLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Tspin Doubles {playerStats.clearTypes.TSPIN_DOUBLE / (double)total * 100:0.00}% ({playerStats.clearTypes.TSPIN_DOUBLE})",
			data = new[] { playerStats.clearTypes.TSPIN_DOUBLE / (double)total * 100 },
			backgroundColor = "#96dab5",
			rawData = playerStats.clearTypes.TSPIN_DOUBLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"Tspin Triples {playerStats.clearTypes.TSPIN_TRIPLE / (double)total * 100:0.00}% ({playerStats.clearTypes.TSPIN_TRIPLE})",
			data = new[] { playerStats.clearTypes.TSPIN_TRIPLE / (double)total * 100 },
			backgroundColor = "#dd8480",
			rawData = playerStats.clearTypes.TSPIN_TRIPLE
		});
		data.datasets.Add(new Dataset()
		{
			label =
				$"All Clears {playerStats.clearTypes.PERFECT_CLEAR / (double)total * 100:0.00}% ({playerStats.clearTypes.PERFECT_CLEAR})",
			data = new[] { playerStats.clearTypes.PERFECT_CLEAR / (double)total * 100 },
			backgroundColor = "#e8b699",
			rawData = playerStats.clearTypes.PERFECT_CLEAR
		});
	}

	public override string SerializeChart()
		=> JsonSerializer.Serialize(this);
}