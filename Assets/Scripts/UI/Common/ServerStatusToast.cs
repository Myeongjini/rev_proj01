using System;
using UnityEngine;
using WizardGrower.Economy;

namespace WizardGrower.UI.Common
{
    public static class ServerStatusToast
    {
        public const string ServerRequired = "서버 연결이 필요합니다.";
        public const string ServerDelayed = "서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요.";
        public const string RewardFailed = "서버 보상 수령에 실패했습니다. 다시 시도해주세요.";
        public const string GachaFailed = "서버 뽑기에 실패했습니다.";

        public static event Action<string> MessageRequested;

        public static string ResolveRewardFailureMessage()
        {
            string currencyFailure = CurrencyWallet.RecentFailureMessage;
            return string.IsNullOrEmpty(currencyFailure) ? RewardFailed : currencyFailure;
        }

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (message == RewardFailed)
                message = ResolveRewardFailureMessage();

            MessageRequested?.Invoke(message);
            Debug.LogWarning(message);
        }
    }
}
