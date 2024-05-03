using System.Text;
using System.Text.Json;

namespace Minomuncher.Charts;

public abstract class ChartBase
{
	public ChartBase(string type, string optionType)
	{
		this.type = type;
		this.data = new ChartData();
		this.data.datasets = new List<Dataset>();

		this.optionType = optionType;
	}

	public abstract string SerializeChart();

	public async Task<string> DownloadChart(int width, int height)
	{
		var postJson = SerializeChart();

		using HttpClient client = new HttpClient();
		HttpContent content = new StringContent(postJson, Encoding.UTF8, "application/json");
		var graphBase64 = await client.PostAsync($"http://localhost:8084?width={width}&height={height}", content);
		return await graphBase64.Content.ReadAsStringAsync();
	}

	public static async Task<string> DownloadChart(string json, int width, int height)
	{
		using HttpClient client = new HttpClient();
		HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
		var graphBase64 = await client.PostAsync($"http://localhost:8084?width={width}&height={height}", content);
		return await graphBase64.Content.ReadAsStringAsync();
	}

	public string type { get; private set; }
	public ChartData data { get; set; }
	public string optionType { get; set; }
}

public class ChartData
{
	public List<string> labels { get; set; }
	public List<Dataset> datasets { get; set; }
}

public class Dataset
{
	public string? label { get; set; }
	public int? index { get; set; }
	public double[]? data { get; set; }
	public string? backgroundColor { get; set; }
	public double? borderWidth { get; set; }
	public double? rawData { get; set; }
}