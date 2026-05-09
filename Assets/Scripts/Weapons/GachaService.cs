using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Economy;
using WizardGrower.Save;

namespace WizardGrower.Weapons
{
    public interface IRandomSource
    {
        float Value();
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class UnityRandomSource : IRandomSource
    {
        public float Value()
        {
            return UnityEngine.Random.value;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxExclusive);
        }
    }

    public class GachaService : MonoBehaviour
    {
        private CurrencyWallet wallet;
        private WeaponInventory inventory;
        private GachaDefinition definition;
        private SaveService saveService;
        private IRandomSource random = new UnityRandomSource();
        private int currentPity;
        private readonly SummonLevelState summonState = new SummonLevelState();

        public int CurrentPity => currentPity;
        public int CurrentSummonLevel => Mathf.Max(1, summonState.summonLevel);
        public int SummonPullsInLevel => Mathf.Max(0, summonState.summonPullsInLevel);
        public GachaDefinition Definition => definition;
        public SummonLevelDefinition CurrentLevelDefinition => GetCurrentLevelDefinition();

        public event Action<int> PityChanged;
        public event Action<int> SummonLevelChanged;
        public event Action StateChanged;
        public event Action<string> PullFailed;
        public event Action<int> PullCompleted;

        public void Initialize(CurrencyWallet wallet, WeaponInventory inv, GachaDefinition definition, SaveService save)
        {
            Initialize(wallet, inv, definition, save, new UnityRandomSource());
        }

        public void Initialize(CurrencyWallet wallet, WeaponInventory inv, GachaDefinition definition, SaveService save, IRandomSource randomSource)
        {
            if (this.wallet != null)
                this.wallet.GemsChanged -= OnGemsChanged;

            this.wallet = wallet;
            inventory = inv;
            this.definition = definition;
            saveService = save;
            random = randomSource ?? new UnityRandomSource();

            SaveData data = saveService != null ? saveService.CurrentData : null;
            LoadState(
                data != null ? data.summonLevel : 1,
                data != null ? data.summonPullsInLevel : 0,
                data != null ? data.pityCounter : 0);

            if (this.wallet != null)
                this.wallet.GemsChanged += OnGemsChanged;
        }

        public void LoadPity(int pityCounter)
        {
            LoadState(CurrentSummonLevel, SummonPullsInLevel, pityCounter);
        }

        public void LoadState(int summonLevel, int pullsInLevel, int pityCounter)
        {
            int previousLevel = CurrentSummonLevel;
            summonState.summonLevel = Mathf.Max(1, summonLevel);
            summonState.summonPullsInLevel = Mathf.Max(0, pullsInLevel);
            currentPity = Mathf.Max(0, pityCounter);
            NormalizeSummonProgress();
            if (previousLevel != CurrentSummonLevel)
                SummonLevelChanged?.Invoke(CurrentSummonLevel);
            PityChanged?.Invoke(currentPity);
            StateChanged?.Invoke();
        }

        public bool CanSinglePull()
        {
            return CanSpend(definition != null ? definition.costSingle : 0);
        }

        public bool CanTenPull()
        {
            return CanSpend(definition != null ? definition.costTen : 0);
        }

        public bool CanThirtyPull()
        {
            return CanSpend(definition != null ? definition.costThirty : 0);
        }

        public bool TrySinglePull(out WeaponDefinition pulled)
        {
            pulled = null;
            List<WeaponDefinition> results;
            if (!TryPull(1, definition != null ? definition.costSingle : 0, out results))
                return false;

            pulled = results.Count > 0 ? results[0] : null;
            return pulled != null;
        }

        public bool TryTenPull(out List<WeaponDefinition> pulled)
        {
            return TryPull(10, definition != null ? definition.costTen : 0, out pulled);
        }

        public IReadOnlyList<WeaponDefinition> PullThirty()
        {
            List<WeaponDefinition> pulled;
            return TryThirtyPull(out pulled) ? pulled : Array.Empty<WeaponDefinition>();
        }

        public bool TryThirtyPull(out List<WeaponDefinition> pulled)
        {
            return TryPull(30, definition != null ? definition.costThirty : 0, out pulled);
        }

        public IReadOnlyList<WeaponGradeWeight> GetCurrentUpperGradeWeightsNormalized()
        {
            List<WeaponGradeWeight> normalized = new List<WeaponGradeWeight>();
            SummonLevelDefinition level = GetCurrentLevelDefinition();
            if (level == null || level.upperGradeWeights == null)
                return normalized;

            float total = 0f;
            for (int i = 0; i < level.upperGradeWeights.Length; i++)
            {
                WeaponGradeWeight entry = level.upperGradeWeights[i];
                if (entry.weight > 0f && entry.upperGrade <= level.maxUpperGrade)
                    total += entry.weight;
            }

            if (total <= 0f)
                return normalized;

            for (int i = 0; i < level.upperGradeWeights.Length; i++)
            {
                WeaponGradeWeight entry = level.upperGradeWeights[i];
                if (entry.weight <= 0f || entry.upperGrade > level.maxUpperGrade)
                    continue;

                normalized.Add(new WeaponGradeWeight
                {
                    upperGrade = entry.upperGrade,
                    weight = entry.weight / total
                });
            }

            return normalized;
        }

        public WeaponUpperGrade WeightedRandomUpperGrade(IRandomSource source, WeaponUpperGrade? minimumGrade = null)
        {
            SummonLevelDefinition level = GetCurrentLevelDefinition();
            if (level == null || level.upperGradeWeights == null || level.upperGradeWeights.Length == 0)
                return minimumGrade.HasValue ? ClampToCurrentMax(minimumGrade.Value) : WeaponUpperGrade.Common;

            WeaponUpperGrade floor = minimumGrade.HasValue ? ClampToCurrentMax(minimumGrade.Value) : WeaponUpperGrade.Common;
            float total = 0f;
            for (int i = 0; i < level.upperGradeWeights.Length; i++)
            {
                WeaponGradeWeight entry = level.upperGradeWeights[i];
                if (entry.weight <= 0f || entry.upperGrade < floor || entry.upperGrade > level.maxUpperGrade)
                    continue;
                total += entry.weight;
            }

            if (total <= 0f)
                return floor;

            float roll = Mathf.Clamp01(source != null ? source.Value() : UnityEngine.Random.value) * total;
            float cursor = 0f;
            for (int i = 0; i < level.upperGradeWeights.Length; i++)
            {
                WeaponGradeWeight entry = level.upperGradeWeights[i];
                if (entry.weight <= 0f || entry.upperGrade < floor || entry.upperGrade > level.maxUpperGrade)
                    continue;
                cursor += entry.weight;
                if (roll <= cursor)
                    return entry.upperGrade;
            }

            return floor;
        }

        private bool TryPull(int count, int cost, out List<WeaponDefinition> pulled)
        {
            pulled = new List<WeaponDefinition>();
            if (definition == null || wallet == null || inventory == null || definition.pool == null)
                return Fail("가챠 준비가 필요합니다.");

            if (!wallet.TrySpendGems(cost))
                return Fail("젬이 부족합니다.");

            for (int i = 0; i < count; i++)
            {
                WeaponDefinition weapon = PullOne();
                if (weapon == null)
                {
                    wallet.AddGems(cost);
                    pulled.Clear();
                    return Fail("뽑기 풀에 무기가 없습니다.");
                }

                inventory.Add(weapon.weaponId);
                pulled.Add(weapon);
                AdvanceSummonProgress(1);
            }

            StateChanged?.Invoke();
            PullCompleted?.Invoke(pulled.Count);
            return true;
        }

        private WeaponDefinition PullOne()
        {
            WeaponUpperGrade upper = WeightedRandomUpperGrade(random);
            WeaponLowerGrade lower = (WeaponLowerGrade)random.Range(0, 4);
            WeaponDefinition weapon = definition.pool.GetByGrade(upper, lower);

            if (weapon == null)
                weapon = PickAnyWeaponAtOrBelowCurrentMax();

            return weapon;
        }

        private void AdvanceSummonProgress(int amount)
        {
            int previousLevel = CurrentSummonLevel;
            summonState.summonPullsInLevel += Mathf.Max(0, amount);
            NormalizeSummonProgress();
            if (previousLevel != CurrentSummonLevel)
                SummonLevelChanged?.Invoke(CurrentSummonLevel);
        }

        private void NormalizeSummonProgress()
        {
            SummonLevelDefinition level = GetCurrentLevelDefinition();
            while (level != null && level.pullsToNextLevel > 0 && summonState.summonPullsInLevel >= level.pullsToNextLevel)
            {
                summonState.summonPullsInLevel -= level.pullsToNextLevel;
                SummonLevelDefinition next = definition != null ? definition.GetNextLevelDefinition(level.level) : null;
                if (next == null)
                {
                    summonState.summonPullsInLevel = 0;
                    return;
                }

                summonState.summonLevel = next.level;
                level = next;
            }

            if (level != null && level.pullsToNextLevel <= 0)
                summonState.summonPullsInLevel = 0;
        }

        private SummonLevelDefinition GetCurrentLevelDefinition()
        {
            if (definition == null)
                return null;

            SummonLevelDefinition level = definition.GetLevelDefinition(CurrentSummonLevel);
            if (level != null)
                summonState.summonLevel = Mathf.Max(1, level.level);
            return level;
        }

        private WeaponUpperGrade ClampToCurrentMax(WeaponUpperGrade grade)
        {
            SummonLevelDefinition level = GetCurrentLevelDefinition();
            if (level == null)
                return grade;
            return grade > level.maxUpperGrade ? level.maxUpperGrade : grade;
        }

        private WeaponDefinition PickAnyWeaponAtOrBelowCurrentMax()
        {
            WeaponDatabase pool = definition != null ? definition.pool : null;
            SummonLevelDefinition level = GetCurrentLevelDefinition();
            if (pool == null || pool.OrderedWeapons == null || pool.OrderedWeapons.Count == 0)
                return null;

            List<WeaponDefinition> matches = new List<WeaponDefinition>();
            for (int i = 0; i < pool.OrderedWeapons.Count; i++)
            {
                WeaponDefinition weapon = pool.OrderedWeapons[i];
                if (weapon != null && (level == null || weapon.upperGrade <= level.maxUpperGrade))
                    matches.Add(weapon);
            }

            if (matches.Count == 0)
                return null;
            return matches[random.Range(0, matches.Count)];
        }

        private bool CanSpend(int amount)
        {
            return wallet != null && definition != null && wallet.Gems >= amount;
        }

        private bool Fail(string message)
        {
            PullFailed?.Invoke(message);
            StateChanged?.Invoke();
            return false;
        }

        private void OnGemsChanged(int _)
        {
            StateChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (wallet != null)
                wallet.GemsChanged -= OnGemsChanged;
        }
    }
}
