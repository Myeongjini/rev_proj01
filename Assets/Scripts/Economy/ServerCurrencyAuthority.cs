using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Cloud;

namespace WizardGrower.Economy
{
    public sealed class ServerCurrencyAuthority : ICurrencyAuthority
    {
        private readonly CloudFunctionsClient client;

        public ServerCurrencyAuthority(CloudFunctionsClient client)
        {
            this.client = client;
        }

        public bool IsServerAuthoritative => client != null && client.IsReady;
        public bool IsBusy => false;

        public async Task<CurrencyAuthorityResult> GrantAsync(string kind, int amount, string reason, string source)
        {
            if (!IsServerAuthoritative)
                return new CurrencyAuthorityResult(false, -1, "Cloud Functions unavailable.");

            string functionName = ResolveGrantFunction(source);
            Dictionary<string, object> payload = BuildGrantPayload(kind, amount, reason, source);
            IDictionary<string, object> response = await client.CallAsync(functionName, payload);
            return new CurrencyAuthorityResult(true, ExtractBalance(response), string.Empty);
        }

        public async Task<CurrencyAuthorityResult> SpendAsync(string kind, int amount, string reason)
        {
            if (!IsServerAuthoritative)
                return new CurrencyAuthorityResult(false, -1, "Cloud Functions unavailable.");

            IDictionary<string, object> response = await client.CallAsync("spendCurrency", new Dictionary<string, object>
            {
                { "kind", kind },
                { "amount", Mathf.Max(0, amount) },
                { "reason", string.IsNullOrEmpty(reason) ? "spend" : reason }
            });
            return new CurrencyAuthorityResult(true, ExtractBalance(response), string.Empty);
        }

        private static Dictionary<string, object> BuildGrantPayload(string kind, int amount, string reason, string source)
        {
            string normalizedReason = string.IsNullOrEmpty(reason) ? "reward" : reason;
            string normalizedSource = string.IsNullOrEmpty(source) ? "gameplay" : source;
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "kind", kind },
                { "amount", Mathf.Max(0, amount) },
                { "reason", normalizedReason },
                { "source", normalizedSource }
            };

            if (normalizedSource == "mission")
                payload["missionId"] = normalizedReason.Replace("mission_", string.Empty);
            else if (normalizedSource == "attendance")
                payload["dayIndex"] = ExtractLastNumber(normalizedReason);
            else if (normalizedSource == "dungeon")
                payload["dungeonType"] = ResolveDungeonType(normalizedReason);
            return payload;
        }

        private static string ResolveDungeonType(string reason)
        {
            string normalized = string.IsNullOrEmpty(reason) ? string.Empty : reason.ToLowerInvariant();
            if (normalized.Contains("enhancement"))
                return "enhancement_stone";
            if (normalized.Contains("exp"))
                return "exp";
            return "gold";
        }

        private static string ResolveGrantFunction(string source)
        {
            return source switch
            {
                "developer" => "grantDeveloperCurrency",
                "mission" => "claimMissionReward",
                "attendance" => "claimAttendanceReward",
                "dungeon" => "claimDungeonReward",
                "offline" => "claimOfflineReward",
                "migration" => "migrateWallet",
                _ => "claimEnemyReward"
            };
        }

        private static int ExtractBalance(IDictionary<string, object> response)
        {
            if (response == null)
                return -1;
            if (response.TryGetValue("balanceAfter", out object balance))
                return ConvertToInt(balance, -1);
            if (response.TryGetValue("gold", out object gold))
                return ConvertToInt(gold, -1);
            if (response.TryGetValue("gem", out object gem))
                return ConvertToInt(gem, -1);
            if (response.TryGetValue("enhancement_stone", out object enhancementStone))
                return ConvertToInt(enhancementStone, -1);
            if (response.TryGetValue("enhancementStone", out object enhancementStoneCamel))
                return ConvertToInt(enhancementStoneCamel, -1);
            return -1;
        }

        private static int ExtractLastNumber(string text)
        {
            int value = 0;
            bool found = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    continue;
                found = true;
                value = value * 10 + (text[i] - '0');
            }
            return found ? value : 0;
        }

        private static int ConvertToInt(object value, int fallback)
        {
            if (value == null)
                return fallback;
            if (value is int typed)
                return typed;
            if (value is long longValue)
                return (int)longValue;
            if (value is double doubleValue)
                return Mathf.RoundToInt((float)doubleValue);
            return int.TryParse(value.ToString(), out int parsed) ? parsed : fallback;
        }
    }
}
