using UnityEngine;

namespace WizardGrower.UI
{
    public class MainUI01Coordinator : MonoBehaviour
    {
        [SerializeField] private MainUI01Bar bar;
        [SerializeField] private UpgradeDrawerView upgrade;
        [SerializeField] private WeaponInventoryPanel weapon;
        [SerializeField] private GachaPanel summon;
        [SerializeField] private SkillTabPanel skill;
        [SerializeField] private GoldDungeonEntryPanel goldDungeon;
        private MainUI01Bar.NavTab? activeTab;
        private bool suppressCallbacks;

        private void Awake()
        {
            AutoResolveReferences();
            if (bar != null)
                Initialize(bar, upgrade, weapon, summon, skill, goldDungeon);
        }

        public void Initialize(MainUI01Bar bar, UpgradeDrawerView upgrade, WeaponInventoryPanel weapon, GachaPanel summon)
        {
            Initialize(bar, upgrade, weapon, summon, null);
        }

        public void Initialize(MainUI01Bar bar, UpgradeDrawerView upgrade, WeaponInventoryPanel weapon, GachaPanel summon, SkillTabPanel skill)
        {
            Initialize(bar, upgrade, weapon, summon, skill, null);
        }

        public void Initialize(MainUI01Bar bar, UpgradeDrawerView upgrade, WeaponInventoryPanel weapon, GachaPanel summon, SkillTabPanel skill, GoldDungeonEntryPanel goldDungeon)
        {
            if (this.bar != null)
                this.bar.TabRequested -= OnTabRequested;
            if (this.upgrade != null)
                this.upgrade.OpenStateChanged -= OnUpgradeChanged;
            if (this.weapon != null)
                this.weapon.OpenStateChanged -= OnWeaponChanged;
            if (this.summon != null)
                this.summon.OpenStateChanged -= OnSummonChanged;
            if (this.skill != null)
                this.skill.OpenStateChanged -= OnSkillChanged;
            if (this.goldDungeon != null)
                this.goldDungeon.OpenStateChanged -= OnGoldDungeonChanged;

            this.bar = bar;
            this.upgrade = upgrade;
            this.weapon = weapon;
            this.summon = summon;
            this.skill = skill;
            this.goldDungeon = goldDungeon;

            if (this.bar != null)
            {
                this.bar.Bind();
                this.bar.TabRequested += OnTabRequested;
            }
            if (this.upgrade != null)
                this.upgrade.OpenStateChanged += OnUpgradeChanged;
            if (this.weapon != null)
                this.weapon.OpenStateChanged += OnWeaponChanged;
            if (this.summon != null)
                this.summon.OpenStateChanged += OnSummonChanged;
            if (this.skill != null)
                this.skill.OpenStateChanged += OnSkillChanged;
            if (this.goldDungeon != null)
                this.goldDungeon.OpenStateChanged += OnGoldDungeonChanged;

            CloseAll();
        }

        private void AutoResolveReferences()
        {
            if (bar == null)
                bar = FindAnyObjectByType<MainUI01Bar>(FindObjectsInactive.Include);
            if (upgrade == null)
                upgrade = FindAnyObjectByType<UpgradeDrawerView>(FindObjectsInactive.Include);
            if (weapon == null)
                weapon = FindAnyObjectByType<WeaponInventoryPanel>(FindObjectsInactive.Include);
            if (summon == null)
                summon = FindAnyObjectByType<GachaPanel>(FindObjectsInactive.Include);
            if (skill == null)
                skill = FindAnyObjectByType<SkillTabPanel>(FindObjectsInactive.Include);
            if (goldDungeon == null)
                goldDungeon = FindAnyObjectByType<GoldDungeonEntryPanel>(FindObjectsInactive.Include);
        }

        public void Open(MainUI01Bar.NavTab tab)
        {
            if (activeTab.HasValue && activeTab.Value == tab)
            {
                CloseAll();
                return;
            }

            suppressCallbacks = true;
            if (upgrade != null)
                upgrade.Close();
            if (weapon != null)
                weapon.Close();
            if (summon != null)
                summon.Close();
            if (skill != null)
                skill.Close();
            if (goldDungeon != null)
                goldDungeon.Close();

            if (tab == MainUI01Bar.NavTab.Upgrade && upgrade != null)
                upgrade.Open();
            else if (tab == MainUI01Bar.NavTab.Weapon && weapon != null)
                weapon.Open();
            else if (tab == MainUI01Bar.NavTab.Summon && summon != null)
                summon.Open();
            else if (tab == MainUI01Bar.NavTab.Skill && skill != null)
                skill.Open();
            else if (tab == MainUI01Bar.NavTab.GoldDungeon && goldDungeon != null)
                goldDungeon.Open();

            suppressCallbacks = false;
            activeTab = tab;
            if (bar != null)
                bar.SetActiveTab(activeTab);
        }

        public void CloseAll()
        {
            suppressCallbacks = true;
            if (upgrade != null)
                upgrade.Close();
            if (weapon != null)
                weapon.Close();
            if (summon != null)
                summon.Close();
            if (skill != null)
                skill.Close();
            if (goldDungeon != null)
                goldDungeon.Close();
            suppressCallbacks = false;

            activeTab = null;
            if (bar != null)
                bar.SetActiveTab(null);
        }

        private void OnTabRequested(MainUI01Bar.NavTab tab)
        {
            Open(tab);
        }

        private void OnUpgradeChanged(bool open)
        {
            if (!suppressCallbacks)
                SyncExternalClose(open, MainUI01Bar.NavTab.Upgrade);
        }

        private void OnWeaponChanged(bool open)
        {
            if (!suppressCallbacks)
                SyncExternalClose(open, MainUI01Bar.NavTab.Weapon);
        }

        private void OnSummonChanged(bool open)
        {
            if (!suppressCallbacks)
                SyncExternalClose(open, MainUI01Bar.NavTab.Summon);
        }

        private void OnSkillChanged(bool open)
        {
            if (!suppressCallbacks)
                SyncExternalClose(open, MainUI01Bar.NavTab.Skill);
        }

        private void OnGoldDungeonChanged(bool open)
        {
            if (!suppressCallbacks)
                SyncExternalClose(open, MainUI01Bar.NavTab.GoldDungeon);
        }

        private void SyncExternalClose(bool open, MainUI01Bar.NavTab tab)
        {
            if (open)
            {
                activeTab = tab;
            }
            else if (activeTab.HasValue && activeTab.Value == tab)
            {
                activeTab = null;
            }

            if (bar != null)
                bar.SetActiveTab(activeTab);
        }
    }
}
