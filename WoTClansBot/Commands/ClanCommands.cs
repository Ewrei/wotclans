﻿using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using log4net;
using Negri.Wot.Sql;

namespace Negri.Wot.Bot
{
    public class ClanCommands : CommandsBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClanCommands));

        private readonly string _connectionString;


        public ClanCommands()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["Main"].ConnectionString;
        }

        [Command("clan")]
        [Description("A quick overview of a clan")]
        public async Task Clan(CommandContext ctx, [Description("The clan **tag**")] string clanTag)
        {
            if (!await CanExecute(ctx, Features.Clans))
            {
                return;
            }


            await ctx.TriggerTypingAsync();

            if (string.IsNullOrWhiteSpace(clanTag))
            {
                await ctx.RespondAsync($"You must send a clan tag as parameter, {ctx.User.Mention}.");
                return;
            }

            Log.Debug($"Requesting {nameof(Clan)}({clanTag})...");

            var cfg = GuildConfiguration.FromGuild(ctx.Guild);
            var platform = GetPlatform(clanTag, cfg.Plataform, out clanTag);

            clanTag = clanTag.Trim('[', ']');
            clanTag = clanTag.ToUpperInvariant();

            if (!ClanTagRegex.IsMatch(clanTag))
            {
                await ctx.RespondAsync($"You must send a **valid** clan **tag** as parameter, {ctx.User.Mention}.");
                return;
            }

            var provider = new DbProvider(_connectionString);

            var clan = provider.GetClan(platform, clanTag);
            if (clan == null)
            {
                platform = platform == Platform.PS ? Platform.XBOX : Platform.PS;

                clan = provider.GetClan(platform, clanTag);
                if (clan == null)
                {
                    await ctx.RespondAsync(
                        $"Can't find on a clan with tag `[{clanTag}]`, {ctx.User.Mention}. Maybe my site doesn't track it yet... or you have the wrong clan tag.");
                    return;
                }
            }

            var sb = new StringBuilder();

            sb.Append($"Information about the `{clan.ClanTag}`");
            if (!string.IsNullOrWhiteSpace(clan.Country))
            {
                sb.Append($" ({clan.Country.ToUpperInvariant()})");
            }

            sb.AppendLine($", on the {clan.Plataform}, {ctx.User.Mention}:");

            if (!clan.Enabled)
            {
                sb.AppendLine("Data collection for this clan is **disabled**!");
            }

            sb.AppendLine();

            sb.AppendLine($"Active Members: {clan.Active}; Total Members: {clan.Count};");
            sb.AppendLine($"Recent Win Rate: {clan.ActiveWinRate:P1}; Overall Win Rate: {clan.TotalWinRate:P1};");
            sb.AppendLine($"Recent WN8t15: {clan.Top15Wn8:N0}; Overall WN8: {clan.TotalWn8:N0};");
            if (clan.DeltaDayTop15Wn8.HasValue)
            {
                sb.AppendLine($"WN8t15 Variation from 1 day ago: {clan.DeltaDayTop15Wn8.Value:N0}");
            }

            if (clan.DeltaWeekTop15Wn8.HasValue)
            {
                sb.AppendLine($"WN8t15 Variation from 1 week ago: {clan.DeltaWeekTop15Wn8.Value:N0}");
            }

            if (clan.DeltaMonthTop15Wn8.HasValue)
            {
                sb.AppendLine($"WN8t15 Variation from 1 month ago: {clan.DeltaMonthTop15Wn8.Value:N0}");
            }

            sb.AppendLine($"Recent Actives Battles: {clan.ActiveBattles:N0}; Recent Avg Tier: {clan.ActiveAvgTier:N1}");

            sb.AppendLine();
            sb.AppendLine("**Top 15 Active Players**");
            foreach (var p in clan.Top15Players)
            {
                sb.AppendLine(
                    $"{Formatter.MaskedUrl(p.Name, new Uri(p.PlayerOverallUrl))}, {p.MonthBattles} battles, WN8: {p.MonthWn8:N0}");
            }

            var title = clan.ClanTag.EqualsCiAi(clan.Name) ? clan.ClanTag : $"{clan.ClanTag} - {clan.Name}";

            var color = clan.Top15Wn8.ToColor();

            var platformPrefix = clan.Plataform == Platform.PS ? "ps." : string.Empty;

            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Description = sb.ToString(),
                Color = new DiscordColor(color.R, color.G, color.B),
                Url = $"https://{platformPrefix}wotclans.com.br/Clan/{clan.ClanTag}",
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = "WoTClans",
                    Url = $"https://{platform}wotclans.com.br"
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Data calculated at {clan.Moment:yyyy-MM-dd HH:mm} UTC."
                }
            };

            Log.Debug($"Returned {nameof(Clan)}({clan.Plataform}.{clan.ClanTag})");

            await ctx.RespondAsync("", embed: embed);
        }
    }
}