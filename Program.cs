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

	private static void Main(string[] args)
		=> new Program().MainAsync().Wait();

	private async Task MainAsync()
	{
		DotNetEnv.Env.Load("./.env");
		await TetrioAPI.Initialize();

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
		// Don't process the command if it was a system message
		var message = messageParam as SocketUserMessage;
		if (message == null) return;

		// Create a number to track where the prefix ends and the command begins
		int argPos = 0;

		// Determine if the message is a command based on the prefix and make sure no bots trigger commands
		if (!(message.HasCharPrefix('!', ref argPos) &&
		      !message.Author.IsBot))
			return;

		var context = new SocketCommandContext(_client, message);
//TODO: rate limit

		Console.WriteLine("excute:" + context);
		await _commands.ExecuteAsync(
			context: context,
			argPos: argPos,
			services: _services);
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
