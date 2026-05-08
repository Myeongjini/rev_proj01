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

        public int CurrentPity => currentPity;
        public GachaDefinition Definition => definition;

        public event Action<int> PityChanged;
        public event Action StateChanged;
        public event Action<string> PullFailed;

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
            currentPity = Mathf.Max(0, saveService != null && saveService.CurrentData != null ? saveService.CurrentData.pityCounter : 0);

            if (this.wallet != null)
                this.wallet.GemsChanged += OnGemsChanged;

            StateChanged?.Invoke();
            PityChanged?.Invoke(currentPity);
        }

        public void LoadPity(int pityCounter)
        {
            currentPity = Mathf.Max(0, pityCounter);
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

        public Rarity WeightedRandomRarity(IRandomSource source, Rarity? minimumRarity = null)
        {
            if (definition == null || definition.weights == null || definition.weights.Length == 0)
                return minimumRarity ?? Rarity.Common;

            float total = 0f;
            for (int i = 0; i < definition.weights.Length; i++)
            {
                RarityWeight entry = definition.weights[i];
                if (entry.weight <= 0f)
                    continue;
                if (minimumRarity.HasValue && entry.rarity < minimumRarity.Value)
                    continue;
                total += entry.weight;
            }

            if (total <= 0f)
                return minimumRarity ?? Rarity.Common;

            float roll = Mathf.Clamp01(source != null ? source.Value() : UnityEngine.Random.value) * total;
            float cursor = 0f;
            for (int i = 0; i < definition.weights.Length; i++)
            {
                RarityWeight entry = definition.weights[i];
                if (entry.weight <= 0f)
                    continue;
                if (minimumRarity.HasValue && entry.rarity < minimumRarity.Value)
                    continue;
                cursor += entry.weight;
                if (roll <= cursor)
                    return entry.rarity;
            }

            return definition.weights[definition.weights.Length - 1].rarity;
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
            }

            StateChanged?.Invoke();
            return true;
        }

        private WeaponDefinition PullOne()
        {
            currentPity++;
            bool guaranteed = definition.pityThreshold > 0 && currentPity >= definition.pityThreshold;
            Rarity rarity = WeightedRandomRarity(random, guaranteed ? definition.pityFloor : (Rarity?)null);
            WeaponDefinition weapon = PickWeapon(rarity);

            if (weapon == null && guaranteed)
                weapon = PickWeaponAtOrAbove(definition.pityFloor);

            if (weapon == null)
                weapon = PickAnyWeapon();

            if (weapon != null && weapon.rarity >= definition.pityFloor)
                currentPity = 0;

            PityChanged?.Invoke(currentPity);
            return weapon;
        }

        private WeaponDefinition PickWeapon(Rarity rarity)
        {
            List<WeaponDefinition> matches = CollectWeapons(rarity, false);
            if (matches.Count == 0)
                return null;

            List<WeaponDefinition> unowned = new List<WeaponDefinition>();
            for (int i = 0; i < matches.Count; i++)
            {
                if (inventory == null || !inventory.IsOwned(matches[i].weaponId))
                    unowned.Add(matches[i]);
            }

            List<WeaponDefinition> pool = unowned.Count > 0 ? unowned : CollectUnownedWeapons();
            if (pool.Count == 0)
                pool = matches;
            return pool[random.Range(0, pool.Count)];
        }

        private WeaponDefinition PickWeaponAtOrAbove(Rarity floor)
        {
            List<WeaponDefinition> matches = CollectWeapons(floor, true);
            if (matches.Count == 0)
                return null;

            return matches[random.Range(0, matches.Count)];
        }

        private WeaponDefinition PickAnyWeapon()
        {
            WeaponDatabase pool = definition != null ? definition.pool : null;
            if (pool == null || pool.weapons == null || pool.weapons.Length == 0)
                return null;

            return pool.weapons[random.Range(0, pool.weapons.Length)];
        }

        private List<WeaponDefinition> CollectUnownedWeapons()
        {
            List<WeaponDefinition> matches = new List<WeaponDefinition>();
            WeaponDatabase pool = definition != null ? definition.pool : null;
            if (pool == null || pool.weapons == null)
                return matches;

            for (int i = 0; i < pool.weapons.Length; i++)
            {
                WeaponDefinition weapon = pool.weapons[i];
                if (weapon != null && (inventory == null || !inventory.IsOwned(weapon.weaponId)))
                    matches.Add(weapon);
            }

            return matches;
        }

        private List<WeaponDefinition> CollectWeapons(Rarity rarity, bool atOrAbove)
        {
            List<WeaponDefinition> matches = new List<WeaponDefinition>();
            WeaponDatabase pool = definition != null ? definition.pool : null;
            if (pool == null || pool.weapons == null)
                return matches;

            for (int i = 0; i < pool.weapons.Length; i++)
            {
                WeaponDefinition weapon = pool.weapons[i];
                if (weapon == null)
                    continue;
                if (atOrAbove ? weapon.rarity >= rarity : weapon.rarity == rarity)
                    matches.Add(weapon);
            }

            return matches;
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
