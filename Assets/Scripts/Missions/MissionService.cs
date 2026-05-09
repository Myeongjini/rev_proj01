using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Stages;
using WizardGrower.Weapons;

namespace WizardGrower.Missions
{
    public class MissionService : MonoBehaviour
    {
        [SerializeField] private MissionDatabase database;

        private CurrencyWallet wallet;
        private MissionResetService resetService;
        private readonly List<DailyMissionState> dailyStates = new List<DailyMissionState>();
        private readonly List<RepeatMissionState> repeatStates = new List<RepeatMissionState>();

        public event Action StateChanged;

        public MissionDatabase Database => database;
        public IReadOnlyList<DailyMissionState> DailyStates => dailyStates;
        public IReadOnlyList<RepeatMissionState> RepeatStates => repeatStates;

        public void Initialize(
            MissionDatabase database,
            CurrencyWallet wallet,
            EnemySpawner spawner,
            StageManager stageManager,
            GachaService gachaService,
            WeaponFusionService fusionService,
            MissionResetService resetService)
        {
            this.database = database != null ? database : CreateDefaultDatabase();
            this.wallet = wallet;
            this.resetService = resetService;

            if (spawner != null)
                spawner.EnemyKilled += _ => Increment(MissionTracker.KillMonsters, 1);
            if (stageManager != null)
                stageManager.BossCleared += () => Increment(MissionTracker.ClearBoss, 1);
            if (wallet != null)
                wallet.GoldGained += amount => Increment(MissionTracker.EarnGold, amount);
            if (gachaService != null)
                gachaService.PullCompleted += count => Increment(MissionTracker.GachaPull, count);
            if (fusionService != null)
                fusionService.FusionCompleted += results => Increment(MissionTracker.SynthesizeWeapon, CountFusion(results));
            if (this.resetService != null)
                this.resetService.DailyResetTriggered += ResetDaily;
        }

        public void Load(List<DailyMissionState> daily, List<RepeatMissionState> repeat, long nowUtcMs)
        {
            dailyStates.Clear();
            repeatStates.Clear();

            if (daily != null)
                dailyStates.AddRange(daily);
            if (repeat != null)
                repeatStates.AddRange(repeat);

            SeedMissing(nowUtcMs);
            long lastReset = dailyStates.Count > 0 ? dailyStates[0].lastResetUtcMs : 0;
            if (resetService != null && resetService.IsLaterKstDay(lastReset, nowUtcMs))
                ResetDaily(nowUtcMs);
            else
                StateChanged?.Invoke();
        }

        public List<DailyMissionState> CaptureDaily()
        {
            return CloneDaily(dailyStates);
        }

        public List<RepeatMissionState> CaptureRepeat()
        {
            return CloneRepeat(repeatStates);
        }

        public MissionDefinition GetDefinition(string missionId)
        {
            return database != null ? database.GetById(missionId) : null;
        }

        public bool IsComplete(DailyMissionState state)
        {
            MissionDefinition definition = state != null ? GetDefinition(state.missionId) : null;
            return definition != null && state.progress >= definition.initialTargetCount;
        }

        public bool IsComplete(RepeatMissionState state)
        {
            return state != null && state.runningCounter >= state.currentTargetN;
        }

        public bool ClaimDaily(string missionId)
        {
            DailyMissionState state = dailyStates.Find(s => s != null && s.missionId == missionId);
            MissionDefinition definition = GetDefinition(missionId);
            if (state == null || definition == null || state.claimed || !IsComplete(state))
                return false;

            state.claimed = true;
            Grant(definition);
            StateChanged?.Invoke();
            return true;
        }

        public bool ClaimRepeat(string missionId)
        {
            RepeatMissionState state = repeatStates.Find(s => s != null && s.missionId == missionId);
            MissionDefinition definition = GetDefinition(missionId);
            if (state == null || definition == null || !IsComplete(state))
                return false;

            Grant(definition);
            state.currentTargetN += Mathf.Max(1, definition.repeatDelta);
            StateChanged?.Invoke();
            return true;
        }

        public void Increment(MissionTracker tracker, int amount)
        {
            if (amount <= 0 || database == null)
                return;

            bool changed = false;
            foreach (MissionDefinition definition in database.OrderedMissions)
            {
                if (definition == null || definition.tracker != tracker)
                    continue;

                if (definition.type == MissionType.Daily)
                {
                    DailyMissionState state = dailyStates.Find(s => s != null && s.missionId == definition.missionId);
                    if (state != null && !state.claimed)
                    {
                        state.progress += amount;
                        changed = true;
                    }
                }
                else
                {
                    RepeatMissionState state = repeatStates.Find(s => s != null && s.missionId == definition.missionId);
                    if (state != null)
                    {
                        state.runningCounter += amount;
                        changed = true;
                    }
                }
            }

            if (changed)
                StateChanged?.Invoke();
        }

        private void SeedMissing(long nowUtcMs)
        {
            if (database == null)
                return;

            foreach (MissionDefinition definition in database.OrderedMissions)
            {
                if (definition == null)
                    continue;

                if (definition.type == MissionType.Daily)
                {
                    if (!dailyStates.Exists(s => s != null && s.missionId == definition.missionId))
                        dailyStates.Add(new DailyMissionState(definition.missionId, nowUtcMs));
                }
                else if (!repeatStates.Exists(s => s != null && s.missionId == definition.missionId))
                {
                    repeatStates.Add(new RepeatMissionState(definition.missionId, Mathf.Max(1, definition.initialTargetCount)));
                }
            }
        }

        private void ResetDaily(long nowUtcMs)
        {
            dailyStates.Clear();
            if (database != null)
            {
                foreach (MissionDefinition definition in database.OrderedMissions)
                {
                    if (definition != null && definition.type == MissionType.Daily)
                        dailyStates.Add(new DailyMissionState(definition.missionId, nowUtcMs));
                }
            }
            StateChanged?.Invoke();
        }

        private void Grant(MissionDefinition definition)
        {
            if (definition.rewardKind == RewardKind.Gem && wallet != null)
                wallet.AddGems(definition.rewardAmount);
        }

        private static int CountFusion(IReadOnlyList<WeaponFusionResult> results)
        {
            int total = 0;
            if (results == null)
                return total;
            for (int i = 0; i < results.Count; i++)
                total += Mathf.Max(0, results[i].times);
            return total;
        }

        public static List<DailyMissionState> CloneDaily(IReadOnlyList<DailyMissionState> source)
        {
            List<DailyMissionState> clone = new List<DailyMissionState>();
            if (source == null)
                return clone;
            for (int i = 0; i < source.Count; i++)
            {
                DailyMissionState s = source[i];
                if (s != null)
                    clone.Add(new DailyMissionState { missionId = s.missionId, progress = s.progress, claimed = s.claimed, lastResetUtcMs = s.lastResetUtcMs });
            }
            return clone;
        }

        public static List<RepeatMissionState> CloneRepeat(IReadOnlyList<RepeatMissionState> source)
        {
            List<RepeatMissionState> clone = new List<RepeatMissionState>();
            if (source == null)
                return clone;
            for (int i = 0; i < source.Count; i++)
            {
                RepeatMissionState s = source[i];
                if (s != null)
                    clone.Add(new RepeatMissionState { missionId = s.missionId, currentTargetN = s.currentTargetN, runningCounter = s.runningCounter });
            }
            return clone;
        }

        public static MissionDatabase CreateDefaultDatabase()
        {
            MissionDatabase db = ScriptableObject.CreateInstance<MissionDatabase>();
            db.name = "RuntimeMissionDatabase";
            db.missions = new[]
            {
                CreateDefinition("kill_100_monsters_daily", "몬스터 {0}마리 처치", MissionType.Daily, MissionTracker.KillMonsters, 100, 0, 50),
                CreateDefinition("clear_boss_once_daily", "보스 {0}회 클리어", MissionType.Daily, MissionTracker.ClearBoss, 1, 0, 100),
                CreateDefinition("gacha_once_daily", "가챠 {0}회 사용", MissionType.Daily, MissionTracker.GachaPull, 1, 0, 30),
                CreateDefinition("kill_monsters_repeat", "몬스터 {0}마리 누적 처치", MissionType.Repeat, MissionTracker.KillMonsters, 100, 100, 30),
                CreateDefinition("earn_gold_repeat", "골드 {0} 누적 획득", MissionType.Repeat, MissionTracker.EarnGold, 10000, 10000, 30),
                CreateDefinition("synthesize_weapon_repeat", "무기 {0}회 합성", MissionType.Repeat, MissionTracker.SynthesizeWeapon, 1, 1, 50)
            };
            return db;
        }

        private static MissionDefinition CreateDefinition(
            string id,
            string description,
            MissionType type,
            MissionTracker tracker,
            int target,
            int delta,
            int gems)
        {
            MissionDefinition definition = ScriptableObject.CreateInstance<MissionDefinition>();
            definition.name = id;
            definition.missionId = id;
            definition.descriptionKo = description;
            definition.type = type;
            definition.tracker = tracker;
            definition.initialTargetCount = target;
            definition.repeatDelta = delta;
            definition.rewardKind = RewardKind.Gem;
            definition.rewardAmount = gems;
            return definition;
        }
    }
}
