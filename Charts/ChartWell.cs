
using System.Text.Json;

namespace Minomuncher.Charts;

public class ChartWell : ChartBase
{
	public double scaleYMax { get; private set; }

	public ChartWell(string username, PlayerStats playerStats, bool scale) : base("bar", "well")
	{
		var total = playerStats.wellColumns.Sum();

		scaleYMax = scale ? playerStats.wellColumns.Max() / total + 0.04 : 0.4;

		data.datasets.Add(new Dataset()
		{
			label = $"{username}\'s Well Column Distribution",
			data = playerStats.wellColumns.Select(x => x / (double)total).ToArray(),
			borderWidth = 1
		});
		data.labels = new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", };
	}

	public override string SerializeChart()
	=> JsonSerializer.Serialize(this);
}