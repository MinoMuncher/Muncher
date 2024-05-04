using System.Collections.Concurrent;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Minomuncher;

internal class Program
{
	private DiscordSocketClient _client;
	private CommandService _commands;
	private IServiceProvider _services;
	public static ulong DISCORD_BOT_ADMIN { get; private set; }
	ConcurrentDictionary<ulong, DateTime> _rateLimitHistory = new();


	private static void Main(string[] args)
		=> new Program().MainAsync().Wait();

	private async Task MainAsync()
	{
		DotNetEnv.Env.Load("./.env");
		await TetrioAPI.Initialize();
		DISCORD_BOT_ADMIN = ulong.Parse(DotNetEnv.Env.GetString("DISCORD_BOT_ADMIN"));

		var config = new DiscordSocketConfig
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
		};

		_client = new DiscordSocketClient(config);
		_commands = new CommandService();

		_services = new ServiceCollection()
			.AddSingleton(_client)
			.AddSingleton(_commands)
			.BuildServiceProvider();

		var token = DotNetEnv.Env.GetString("DISCORD_API");
		_client.Log += Client_Log;

		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();
		await RegisterCommandsAsync();
		_client.Ready += Client_Ready;

		await Task.Delay(-1);
	}

	public async Task RegisterCommandsAsync()
	{
		_client.MessageReceived += HandleCommandAsync;

		await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
			services: null);
	}

	private async Task HandleCommandAsync(SocketMessage messageParam)
	{
		Task.Run(async () =>
		{
			var message = messageParam as SocketUserMessage;
			if (message == null) return;

			int argPos = 0;

			if (!(message.HasCharPrefix('!', ref argPos) &&
			      !message.Author.IsBot))
				return;

			var context = new SocketCommandContext(_client, message);

			if (_rateLimitHistory.TryGetValue(context.User.Id, out var value))
			{
				var diff = DateTime.Now - value;
				if (Math.Abs(diff.TotalSeconds) < 2)
				{
					await context.Message.ReplyAsync("You are being rate limited, try again later");
					return;
				}
				else
				{
					_rateLimitHistory.AddOrUpdate(context.User.Id, DateTime.Now, (Key, Value) => Value);
				}
			}
			else
			{
				_rateLimitHistory.AddOrUpdate(context.User.Id, DateTime.Now, (Key, Value) => Value);
			}

			await _commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: _services);
		});
	}

	private Task Client_Ready()
	{
		Console.WriteLine("Bot is connected!");
		return Task.CompletedTask;
	}

	private Task Client_Log(LogMessage arg)
	{
		Console.WriteLine(arg.ToString());

		return Task.CompletedTask;
	}
}