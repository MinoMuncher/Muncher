using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using System;
using TetrLoader.JsonClass;
using TetrLoader.JsonClass.API;
using TetrLoader.JsonClass.Raw;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Minomuncher;

/// <summary>
/// スレッドセーフ
/// </summary>
public class TetrioAPI
{
	public static string TOKEN { get; private set; }

	public static async Task Initialize()
	{
		var authBody = new
		{
			username = DotNetEnv.Env.GetString("TETRIO_USERNAME"),
			password = DotNetEnv.Env.GetString("TETRIO_PASSWORD")
		};
		var content = new StringContent(JsonConvert.SerializeObject(authBody), Encoding.UTF8, "application/json");

		var request = new HttpRequestMessage(HttpMethod.Post, "https://tetr.io/api/users/authenticate")
		{
			Content = content,
		};
		SetRequestMessage(request, false);

		using (var httpClient = new HttpClient())
		{
			var response = await httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				using var jsonDocument = JsonDocument.Parse(responseContent);
				var tokenValue = jsonDocument.RootElement.GetProperty("token").GetString();
				TOKEN = tokenValue;
			}
			else
			{
				throw new System.Exception();
			}
		}
	}

	public static async Task<Record[]> DownloadListOfGameIds(string userId)
	{
		//https://ch.tetr.io/api/streams/league_userrecent_${userID}
		var request =
			new HttpRequestMessage(HttpMethod.Get, $"https://ch.tetr.io/api/streams/league_userrecent_{userId}");
		SetRequestMessage(request, true);
		HttpResponseMessage response;
		using (var httpClient = new HttpClient())
			response = await httpClient.SendAsync(request);

		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var records = JsonSerializer.Deserialize<RawRecords>(responseContent);
			return records.data.records.ToArray();
		}
		else
			throw new System.Exception();

		return null;
	}

	/// <summary>
	/// idからゲームをダウンロード、一度読み込んだものはキャッシュされる。
	/// </summary>
	/// <param name="id"></param>
	/// <param name="useCache"></param>
	/// <returns></returns>
	public static async Task<string> GetReplayFromIdAsync(string id, bool useCache = true)
	{
		//cache使う
		//https://tetr.io/api/games/{id}

		string? data = null;
		if (useCache)
		{
			if (!Directory.Exists(".cache"))
				Directory.CreateDirectory(".cache");

			if (File.Exists(".cache/" + id))
				data = File.ReadAllText(".cache/" + id);
		}

		if (data == null)
		{
			data = await DownloadReplayFromIdAsync(id);
			if (useCache)
				File.WriteAllText(".cache/" + id, data);
		}

		return data;
	}

	private static async Task<string> DownloadReplayFromIdAsync(string id)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, $"https://tetr.io/api/games/{id}");
		SetRequestMessage(request, true);
		using (var httpClient = new HttpClient())
		{
			var response = await httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var records = JsonSerializer.Deserialize<RawGame>(responseContent);
				return records.game.ToString();
			}
			else
				throw new System.Exception(id);
		}
	}

	public static async Task<User> DownloadUserAsync(string username)
	{
		//https://ch.tetr.io/api/users/${username}
		var request = new HttpRequestMessage(HttpMethod.Get, $"https://ch.tetr.io/api/users/{username}");
		SetRequestMessage(request, true);
		using (var httpClient = new HttpClient())
		{
			var response = await httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var user = JsonSerializer.Deserialize<RawUser>(responseContent);
				return user.data.user;
			}
			else
				throw new System.Exception();
		}

		return null;
	}

	public static async Task<User?> SearchUserbyDsicordId(ulong discordId)
	{
		//https://ch.tetr.io/api/users/search/${discordId}
		var request = new HttpRequestMessage(HttpMethod.Get, $"https://ch.tetr.io/api/users/search/{discordId}");
		SetRequestMessage(request, true);
		using (var httpClient = new HttpClient())
		{
			var response = await httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var user = JsonSerializer.Deserialize<UserSearch>(responseContent);
				if ((bool)user.success)
					return user.data.user;
				else return null;
			}
			else
				throw new System.Exception();
		}

		return null;
	}

	public static async Task<List<User>?> DownloadLeagueUserList()
	{
		//https://ch.tetr.io/api/users/lists/league/all
		var request = new HttpRequestMessage(HttpMethod.Get, $"https://ch.tetr.io/api/users/lists/league/all");
		SetRequestMessage(request, true);
		using (var httpClient = new HttpClient())
		{
			var response = await httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var leaderBoard = JsonSerializer.Deserialize<LeagueLeaderBoard>(responseContent);
				if ((bool)leaderBoard.success)
					return leaderBoard.data.users;
				else return null;
			}
			else
				throw new System.Exception();
		}

		return null;
	}

	private static void SetRequestMessage(HttpRequestMessage request, bool setToken)
	{
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		if (request.Content != null)
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		if (setToken)
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN);
	}
}