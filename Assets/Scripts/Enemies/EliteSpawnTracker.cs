using System;
using UnityEngine;
using WizardGrower.Armor;
using WizardGrower.Drops;
using WizardGrower.UI;

namespace WizardGrower.Enemies
{
    public class EliteSpawnTracker : MonoBehaviour
    {
        [SerializeField] private int killsPerElite = 100;

        private EnemySpawner spawner;
        private ArmorInventory armorInventory;
        private ArmorDatabase armorDatabase;
        private ArmorDropTable dropTable;
        private ArmorAcquiredPopupView popup;
        private int counter;

        public int Counter => counter;
        public event Action<int> CounterChanged;

        public void Initialize(EnemySpawner spawner, ArmorInventory armorInventory, ArmorDatabase armorDatabase, ArmorDropTable dropTable, ArmorAcquiredPopupView popup)
        {
            if (this.spawner != null)
                this.spawner.EnemyKilled -= OnEnemyKilled;

            this.spawner = spawner;
            this.armorInventory = armorInventory;
            this.armorDatabase = armorDatabase;
            this.dropTable = dropTable;
            this.popup = popup;

            if (this.spawner != null)
                this.spawner.EnemyKilled += OnEnemyKilled;
        }

        public void LoadCounter(int value)
        {
            counter = Mathf.Max(0, value);
            CounterChanged?.Invoke(counter);
        }

        private void OnDestroy()
        {
            if (spawner != null)
                spawner.EnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled(EnemyBase enemy)
        {
            if (enemy == null)
                return;

            if (enemy.IsElite)
            {
                TryDropArmor();
                return;
            }

            if (enemy is BossEnemy)
                return;

            counter++;
            if (counter >= Mathf.Max(1, killsPerElite))
            {
                counter = 0;
                spawner?.SpawnEliteNear(enemy.transform.position, enemy.MaxHealth, enemy.RewardGold, enemy.Armor);
            }
            CounterChanged?.Invoke(counter);
        }

        private void TryDropArmor()
        {
            ArmorDefinition armor = dropTable != null ? dropTable.Roll(armorDatabase) : null;
            if (armor == null || armorInventory == null)
                return;

            armorInventory.Add(armor.armorId, 1);
            if (popup != null)
                popup.Show(armor);
        }
    }
}
