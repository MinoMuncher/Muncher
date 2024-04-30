﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Minomuncher.Commands;

public class HelpModule : ModuleBase<SocketCommandContext>
{
	public const string HELP_MESSAGE =
		"**!ping**\n" +
		"Check if the bot is online.\n" +
		"**!munch**\n" +
		"If you attach files, MinoMuncher will output stats for the players you input. If you do not input any names, it will output stats for all players in the replays attached.\n\n" +
		"If you do not attach files, MinoMuncher will output stats from the Tetra League games of the player names you input, you must input names. \n\n" +
		"Attach names by following up the **!munch** command with usernames separated by spaces.\n\n" +
		"*Modifiers:*\n" +
		"**-s --scale**\n" +
		"Graph will scale to fit maximum stat values.\n" +
		"**-n --normalize**\n" +
		"Graph will no longer be scaled such that the average X rank player is in the middle, instead having similar stats (midgame APM vs opener APM, etc) be scaled the same.\n" +
		"**-o --order**\n" +
		"Followed by usernames separated by spaces, alters the order of player stats presented. Any absent names will be presented in a random order after the ordered names.\n" +
		"**-g --games**\n" +
		"If you choose the Tetra League option, this alters the number of games to pull from each player, most recent games first.\n" +
		"**-l --league**\n" +
		"If you choose the files option, allows you to query games from league too. Usernames separated by spaces\n" +
		"**!help**\n" +
		"Display this message.";

	[Command("help")]
	public async Task HelpAsync([Remainder] string text = "")
	{
		await Context.Message.ReplyAsync($"{HELP_MESSAGE}");
	}
}