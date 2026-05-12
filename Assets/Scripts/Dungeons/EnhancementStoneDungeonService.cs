using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Ads;
using WizardGrower.Economy;
using WizardGrower.Missions;
using WizardGrower.Player;
using WizardGrower.Save;

namespace WizardGrower.Dungeons
{
    public class EnhancementStoneDungeonService : MonoBehaviour
    {
        [SerializeField] private int dailyEntryLimit = 3;
        [SerializeField] private int baseStonePerKill = 1;
        [SerializeField] private GoldDungeonDifficulty[] difficulties;

        private SaveService save;
        private MissionResetService reset;
        private CurrencyWallet wallet;
        private PlayerLevelService playerLevel;
        private IRewardedAdProvider adProvider;

        public event Action<int> EntryCountChanged;
        public event Action<long> BestScoreChanged;
        public event Action StateChanged;

        public int DailyEntryLimit => Mathf.Max(1, dailyEntryLimit);
        public IReadOnlyList<GoldDungeonDifficulty> Difficulties => difficulties;
        public IRewardedAdProvider AdProvider => adProvider;

        public void Initialize(SaveService save, MissionResetService reset, CurrencyWallet wallet, PlayerLevelService playerLevel, IRewardedAdProvider adProvider)
        {
            this.save = save;
            this.reset = reset;
            this.wallet = wallet;
            this.playerLevel = playerLevel;
            this.adProvider = adProvider;
            EnsureDefaults();
            ResetIfNewDay();
        }

        public Task<int> GetTodayEntryCountAsync()
        {
            ResetIfNewDay();
            return Task.FromResult(State.todayEntryCount);
        }

        public Task<bool> BeginEntryAsync(int difficultyIndex)
        {
            ResetIfNewDay();
            if (State.todayEntryCount >= DailyEntryLimit || !IsDifficultyUnlocked(difficultyIndex))
                return Task.FromResult(false);

            State.todayEntryCount++;
            State.lastEntryDateUtcMs = NowMs();
            save?.Save();
            EntryCountChanged?.Invoke(State.todayEntryCount);
            StateChanged?.Invoke();
            return Task.FromResult(true);
        }

        public async Task<long> CompleteEntryAsync(EnhancementStoneDungeonResult result, bool watchedAd)
        {
            EnsureDefaults();
            long baseStone = result.earnedStone > 0
                ? result.earnedStone
                : CalculateReward(result.killCount, result.difficulty - 1);
            long total = watchedAd ? SafeMultiply(baseStone, 2) : baseStone;
            if (wallet != null)
            {
                bool granted = await wallet.AddEnhancementStoneAsync(ToWalletAmount(total), "enhancement_stone_dungeon", "dungeon");
                if (!granted)
                    return 0;
                if (save != null)
                    save.CurrentData.enhancementStone = wallet.EnhancementStone;
            }

            if (baseStone > State.bestScore)
            {
                State.bestScore = baseStone;
                BestScoreChanged?.Invoke(State.bestScore);
            }

            save?.Save();
            StateChanged?.Invoke();
            return total;
        }

        public long GetBestScore()
        {
            EnsureDefaults();
            return State.bestScore;
        }

        public bool IsDifficultyUnlocked(int difficultyIndex)
        {
            GoldDungeonDifficulty difficulty = GetDifficulty(difficultyIndex);
            return difficulty != null && (difficulty.unlockPlayerLevel <= 0 || playerLevel == null || playerLevel.CurrentLevel >= difficulty.unlockPlayerLevel);
        }

        public long CalculateReward(int killCount, int difficultyIndex)
        {
            EnsureDefaults();
            int kills = Mathf.Max(0, killCount);
            GoldDungeonDifficulty difficulty = GetDifficulty(difficultyIndex);
            double total = Math.Max(0, baseStonePerKill) * Math.Max(0d, difficulty.goldRewardMultiplier) * kills;
            if (total <= 0d)
                return 0;
            return total >= long.MaxValue ? long.MaxValue : (long)Math.Floor(total);
        }

        private EnhancementStoneDungeonState State
        {
            get
            {
                EnsureDefaults();
                return save.CurrentData.enhancementStoneDungeon;
            }
        }

        private void EnsureDefaults()
        {
            if (difficulties == null || difficulties.Length != 5)
                difficulties = CreateDefaultDifficulties();
            if (save != null && save.CurrentData.enhancementStoneDungeon == null)
                save.CurrentData.enhancementStoneDungeon = new EnhancementStoneDungeonState();
        }

        private void ResetIfNewDay()
        {
            if (save == null)
                return;

            EnsureDefaults();
            long now = NowMs();
            if (reset != null && reset.IsLaterKstDay(State.lastEntryDateUtcMs, now))
            {
                State.todayEntryCount = 0;
                State.lastEntryDateUtcMs = now;
                save.Save();
                EntryCountChanged?.Invoke(State.todayEntryCount);
                StateChanged?.Invoke();
            }
        }

        private GoldDungeonDifficulty GetDifficulty(int index)
        {
            EnsureDefaults();
            return difficulties[Mathf.Clamp(index, 0, difficulties.Length - 1)];
        }

        private long NowMs()
        {
            return reset != null ? reset.CurrentServerUtcMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static GoldDungeonDifficulty[] CreateDefaultDifficulties()
        {
            return new[]
            {
                new GoldDungeonDifficulty { level = 1, enemyHpMultiplier = 1f, goldRewardMultiplier = 1f, unlockPlayerLevel = 0 },
                new GoldDungeonDifficulty { level = 2, enemyHpMultiplier = 1.5f, goldRewardMultiplier = 1.3f, unlockPlayerLevel = 5 },
                new GoldDungeonDifficulty { level = 3, enemyHpMultiplier = 2.25f, goldRewardMultiplier = 1.69f, unlockPlayerLevel = 10 },
                new GoldDungeonDifficulty { level = 4, enemyHpMultiplier = 3.375f, goldRewardMultiplier = 2.197f, unlockPlayerLevel = 15 },
                new GoldDungeonDifficulty { level = 5, enemyHpMultiplier = 5.0625f, goldRewardMultiplier = 2.856f, unlockPlayerLevel = 20 }
            };
        }

        private static int ToWalletAmount(long amount)
        {
            if (amount <= 0)
                return 0;
            return amount > int.MaxValue ? int.MaxValue : (int)amount;
        }

        private static long SafeMultiply(long value, long multiplier)
        {
            if (value <= 0 || multiplier <= 0)
                return 0;
            if (value > long.MaxValue / multiplier)
                return long.MaxValue;
            return value * multiplier;
        }
    }
}
