using UnityEngine;

namespace WizardGrower.UI
{
    public class MainUI01Coordinator : MonoBehaviour
    {
        private MainUI01Bar bar;
        private UpgradeDrawerView upgrade;
        private WeaponInventoryPanel weapon;
        private GachaPanel summon;
        private MainUI01Bar.NavTab? activeTab;
        private bool suppressCallbacks;

        public void Initialize(MainUI01Bar bar, UpgradeDrawerView upgrade, WeaponInventoryPanel weapon, GachaPanel summon)
        {
            if (this.bar != null)
                this.bar.TabRequested -= OnTabRequested;
            if (this.upgrade != null)
                this.upgrade.OpenStateChanged -= OnUpgradeChanged;
            if (this.weapon != null)
                this.weapon.OpenStateChanged -= OnWeaponChanged;
            if (this.summon != null)
                this.summon.OpenStateChanged -= OnSummonChanged;

            this.bar = bar;
            this.upgrade = upgrade;
            this.weapon = weapon;
            this.summon = summon;

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

            CloseAll();
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

            if (tab == MainUI01Bar.NavTab.Upgrade && upgrade != null)
                upgrade.Open();
            else if (tab == MainUI01Bar.NavTab.Weapon && weapon != null)
                weapon.Open();
            else if (tab == MainUI01Bar.NavTab.Summon && summon != null)
                summon.Open();

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
            suppressCallbacks = false;

            activeTab = null;
            if (bar != null)
                bar.SetActiveTab(null);
        }

        private void OnTabRequested(MainUI01Bar.NavTab tab)
        {
            if (tab == MainUI01Bar.NavTab.Reserved4 || tab == MainUI01Bar.NavTab.Reserved5)
                return;

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
