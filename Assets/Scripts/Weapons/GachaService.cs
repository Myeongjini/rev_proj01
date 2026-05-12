using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Functions;
using UnityEngine;
using WizardGrower.Cloud;
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
        private CloudFunctionsClient cloudFunctions;
        private IRandomSource random = new UnityRandomSource();
        private int currentPity;
        private readonly SummonLevelState summonState = new SummonLevelState();
        [SerializeField] private bool useSimulationFallback;

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

        public void Initialize(CurrencyWallet wallet, WeaponInventory inv, GachaDefinition definition, SaveService save, CloudFunctionsClient cloudFunctions)
        {
            this.cloudFunctions = cloudFunctions;
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

        public Task<GachaPullResult> TrySinglePullAsync(CancellationToken ct = default)
        {
            return TryPullAsync(1, definition != null ? definition.costSingle : 0, ct);
        }

        public Task<GachaPullResult> TryTenPullAsync(CancellationToken ct = default)
        {
            return TryPullAsync(10, definition != null ? definition.costTen : 0, ct);
        }

        public Task<GachaPullResult> TryThirtyPullAsync(CancellationToken ct = default)
        {
            return TryPullAsync(30, definition != null ? definition.costThirty : 0, ct);
        }

        [Obsolete("Use TrySinglePullAsync — sync API is no-op in server-authority mode", false)]
        public bool TrySinglePull(out WeaponDefinition pulled)
        {
            pulled = null;
            List<WeaponDefinition> results;
            if (!TryPull(1, definition != null ? definition.costSingle : 0, out results))
                return false;

            pulled = results.Count > 0 ? results[0] : null;
            return pulled != null;
        }

        [Obsolete("Use TryTenPullAsync — sync API is no-op in server-authority mode", false)]
        public bool TryTenPull(out List<WeaponDefinition> pulled)
        {
            return TryPull(10, definition != null ? definition.costTen : 0, out pulled);
        }

        [Obsolete("Use TryThirtyPullAsync — sync API is no-op in server-authority mode", false)]
        public IReadOnlyList<WeaponDefinition> PullThirty()
        {
            List<WeaponDefinition> pulled;
            return TryThirtyPull(out pulled) ? pulled : Array.Empty<WeaponDefinition>();
        }

        [Obsolete("Use TryThirtyPullAsync — sync API is no-op in server-authority mode", false)]
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

        private async Task<GachaPullResult> TryPullAsync(int count, int cost, CancellationToken ct)
        {
            if (definition == null || wallet == null || inventory == null || definition.pool == null)
                return FailResult("가챠 준비가 필요합니다.");

            if (cloudFunctions != null && cloudFunctions.IsReady)
            {
                try
                {
                    List<WeaponDefinition> serverPulled = await PullFromServerAsync(count, ct);
                    if (serverPulled.Count <= 0)
                        return FailResult("서버 뽑기 결과가 비어 있습니다.");

                    StateChanged?.Invoke();
                    PullCompleted?.Invoke(serverPulled.Count);
                    return GachaPullResult.Ok(serverPulled);
                }
                catch (TimeoutException ex)
                {
                    Debug.LogWarning($"Server gacha timed out: {ex.GetBaseException().Message}");
                    return FailResult("서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요.");
                }
                catch (FunctionsException ex)
                {
                    Debug.LogWarning($"Server gacha failed: {ex.GetBaseException().Message}");
                    return FailResult(ToGachaFailureMessage(ex));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Server gacha failed: {ex.GetBaseException().Message}");
                    return FailResult("서버 뽑기에 실패했습니다.");
                }
            }

#if UNITY_EDITOR
            if (useSimulationFallback)
                return await TryPullLocalAsync(count, cost, ct);
#endif

            return FailResult("서버 연결이 필요합니다.");
        }

        private bool TryPull(int count, int cost, out List<WeaponDefinition> pulled)
        {
            pulled = new List<WeaponDefinition>();
            if (definition == null || wallet == null || inventory == null || definition.pool == null)
                return Fail("가챠 준비가 필요합니다.");

            return Fail("Use async gacha API.");
        }

        private async Task<GachaPullResult> TryPullLocalAsync(int count, int cost, CancellationToken ct)
        {
            List<WeaponDefinition> pulled = new List<WeaponDefinition>();
            int gemsBeforePull = wallet.Gems;
            if (!await wallet.TrySpendGemsAsync(cost, $"gacha_local_{count}", ct))
                return FailResult("젬이 부족합니다.");

            for (int i = 0; i < count; i++)
            {
                WeaponDefinition weapon = PullOne();
                if (weapon == null)
                {
                    wallet.SetGems(gemsBeforePull);
                    return FailResult("뽑기 풀에 무기가 없습니다.");
                }

                inventory.Add(weapon.weaponId);
                pulled.Add(weapon);
                AdvanceSummonProgress(1);
            }

            StateChanged?.Invoke();
            PullCompleted?.Invoke(pulled.Count);
            return GachaPullResult.Ok(pulled);
        }

        private async Task<List<WeaponDefinition>> PullFromServerAsync(int count, CancellationToken ct)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "count", count },
                { "gachaId", definition != null && !string.IsNullOrEmpty(definition.gachaId) ? definition.gachaId : "standard" }
            };

            IDictionary<string, object> response = await cloudFunctions.CallAsync("rollGacha", payload, ct);
            if (response.TryGetValue("newGemBalance", out object gemBalance))
                wallet.SetGems(ConvertToInt(gemBalance, wallet.Gems));
            if (response.TryGetValue("newSummonLevel", out object summonLevel) || response.TryGetValue("newSummonPullsInLevel", out object pullsInLevel))
            {
                int nextLevel = response.TryGetValue("newSummonLevel", out summonLevel) ? ConvertToInt(summonLevel, CurrentSummonLevel) : CurrentSummonLevel;
                int nextPulls = response.TryGetValue("newSummonPullsInLevel", out pullsInLevel) ? ConvertToInt(pullsInLevel, SummonPullsInLevel) : SummonPullsInLevel;
                LoadState(nextLevel, nextPulls, currentPity);
            }

            List<WeaponDefinition> results = new List<WeaponDefinition>();
            if (!response.TryGetValue("pulls", out object pullsObject))
                return results;

            if (pullsObject is System.Collections.IEnumerable enumerable)
            {
                foreach (object entry in enumerable)
                {
                    string weaponId = ExtractWeaponId(entry);
                    WeaponDefinition weapon = definition.pool.GetById(weaponId);
                    if (weapon == null)
                        continue;
                    inventory.Add(weapon.weaponId);
                    results.Add(weapon);
                }
            }
            return results;
        }

        private static string ToGachaFailureMessage(FunctionsException ex)
        {
            if (ex.ErrorCode == FunctionsErrorCode.FailedPrecondition)
                return "젬이 부족합니다.";
            if (ex.ErrorCode == FunctionsErrorCode.DeadlineExceeded
                || ex.ErrorCode == FunctionsErrorCode.Unavailable
                || ex.ErrorCode == FunctionsErrorCode.Cancelled)
                return "서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요.";
            if (ex.ErrorCode == FunctionsErrorCode.Unauthenticated)
                return "로그인 세션이 필요합니다. LoginScene부터 다시 시작해주세요.";
            return "서버 뽑기에 실패했습니다.";
        }

        private static string ExtractWeaponId(object entry)
        {
            if (entry is IDictionary<string, object> dictionary && dictionary.TryGetValue("weaponId", out object weaponId))
                return weaponId?.ToString();
            if (entry is System.Collections.IDictionary raw && raw.Contains("weaponId"))
                return raw["weaponId"]?.ToString();
            return entry?.GetType().GetProperty("weaponId")?.GetValue(entry)?.ToString();
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

        private GachaPullResult FailResult(string message)
        {
            PullFailed?.Invoke(message);
            StateChanged?.Invoke();
            return GachaPullResult.Fail(message);
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
