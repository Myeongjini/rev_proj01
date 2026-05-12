using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WizardGrower.Ads;
using WizardGrower.Economy;
using WizardGrower.Missions;
using WizardGrower.Player;
using WizardGrower.Save;
using WizardGrower.Stages;

namespace WizardGrower.Dungeons
{
    [Serializable]
    public class GoldDungeonDifficulty
    {
        public int level = 1;
        public float enemyHpMultiplier = 1f;
        public float goldRewardMultiplier = 1f;
        public int unlockPlayerLevel;
    }

    public class GoldDungeonService : MonoBehaviour
    {
        [SerializeField] private int dailyEntryLimit = 3;
        [SerializeField] private GoldDungeonDifficulty[] difficulties;

        private SaveService save;
        private MissionResetService reset;
        private CurrencyWallet wallet;
        private StageManager stageManager;
        private IRewardedAdProvider adProvider;
        private PlayerLevelService playerLevel;

        public event Action<int> EntryCountChanged;
        public event Action<long> BestScoreChanged;
        public event Action StateChanged;

        public int DailyEntryLimit => Mathf.Max(1, dailyEntryLimit);
        public IReadOnlyList<GoldDungeonDifficulty> Difficulties => difficulties;
        public IRewardedAdProvider AdProvider => adProvider;

        public void Initialize(SaveService save, MissionResetService reset, CurrencyWallet wallet, StageManager stageMgr, IRewardedAdProvider adProvider)
        {
            this.save = save;
            this.reset = reset;
            this.wallet = wallet;
            stageManager = stageMgr;
            this.adProvider = adProvider;
            EnsureDefaults();
            ResetIfNewDay();
        }

        public Task<int> GetTodayEntryCountAsync()
        {
            ResetIfNewDay();
            return Task.FromResult(State.todayEntryCount);
        }

        public Task<bool> CanEnterTodayAsync()
        {
            ResetIfNewDay();
            return Task.FromResult(State.todayEntryCount < DailyEntryLimit);
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

        public async Task<long> CompleteEntryAsync(GoldDungeonResult result, bool watchedAd)
        {
            EnsureDefaults();
            long baseGold = result.earnedGold > 0
                ? result.earnedGold
                : CalculateReward(result.killCount, result.difficulty - 1);
            long total = watchedAd ? SafeMultiply(baseGold, 2) : baseGold;
            if (wallet != null)
            {
                bool granted = await wallet.AddGoldAsync(ToWalletAmount(total), "gold_dungeon", "dungeon");
                if (!granted)
                    return 0;
                if (save != null)
                    save.CurrentData.gold = wallet.Gold;
            }

            if (baseGold > State.bestScore)
            {
                State.bestScore = baseGold;
                BestScoreChanged?.Invoke(State.bestScore);
            }

            save?.Save();
            StateChanged?.Invoke();
            return total;
        }

        public void AttachPlayerLevel(PlayerLevelService playerLevel)
        {
            this.playerLevel = playerLevel;
            StateChanged?.Invoke();
        }

        public long GetBestScore()
        {
            EnsureDefaults();
            return State.bestScore;
        }

        public int GetRemainingEntries()
        {
            ResetIfNewDay();
            return Mathf.Max(0, DailyEntryLimit - State.todayEntryCount);
        }

        public long CalculateReward(int killCount, int difficultyIndex)
        {
            EnsureDefaults();
            int kills = Mathf.Max(0, killCount);
            GoldDungeonDifficulty difficulty = GetDifficulty(difficultyIndex);
            double rewardPerKill = GetBaseRewardPerKill() * 1.5d * Math.Max(0d, difficulty.goldRewardMultiplier);
            double total = rewardPerKill * kills;
            if (total <= 0d)
                return 0;
            return total >= long.MaxValue ? long.MaxValue : (long)Math.Floor(total);
        }

        private GoldDungeonState State
        {
            get
            {
                EnsureDefaults();
                return save.CurrentData.goldDungeon;
            }
        }

        private void EnsureDefaults()
        {
            if (difficulties == null || difficulties.Length != 5)
                difficulties = CreateDefaultDifficulties();

            if (save != null && save.CurrentData.goldDungeon == null)
                save.CurrentData.goldDungeon = new GoldDungeonState();
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

        public bool IsDifficultyUnlocked(int difficultyIndex)
        {
            GoldDungeonDifficulty difficulty = GetDifficulty(difficultyIndex);
            return difficulty != null && (difficulty.unlockPlayerLevel <= 0 || playerLevel == null || playerLevel.CurrentLevel >= difficulty.unlockPlayerLevel);
        }

        private GoldDungeonDifficulty GetDifficulty(int index)
        {
            EnsureDefaults();
            int safeIndex = Mathf.Clamp(index, 0, difficulties.Length - 1);
            return difficulties[safeIndex];
        }

        private double GetBaseRewardPerKill()
        {
            ChapterDefinition chapter = stageManager != null ? stageManager.CurrentChapter : null;
            if (chapter == null || chapter.stages == null || chapter.stages.Length == 0)
                return 10d;

            double total = 0d;
            int count = 0;
            foreach (StageDefinition stage in chapter.stages)
            {
                if (stage == null)
                    continue;
                total += Mathf.Max(0, stage.fieldMonsterReward);
                count++;
            }

            return count > 0 ? total / count : 10d;
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
