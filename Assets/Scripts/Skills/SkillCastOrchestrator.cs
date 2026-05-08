using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardGrower.Combat;
using WizardGrower.Enemies;
using WizardGrower.Player;

namespace WizardGrower.Skills
{
    public class SkillCastOrchestrator : MonoBehaviour
    {
        public const int SlotCount = 5;

        [SerializeField] private SkillDatabase database;
        [SerializeField] private PlayerWizard wizard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ProjectileFactory projectileFactory;
        [SerializeField] private PlayerMana mana;

        private readonly SkillRuntime[] slots = new SkillRuntime[SlotCount];
        private CombatCalculator calculator;
        private bool isDashing;

        public event Action<int, SkillDefinition> SlotChanged;

        public SkillDatabase Database => database;
        public IReadOnlyList<SkillDefinition> OwnedSkills { get; private set; } = Array.Empty<SkillDefinition>();

        public void Initialize(
            SkillDatabase database,
            PlayerWizard wizard,
            EnemySpawner enemySpawner,
            ProjectileFactory projectileFactory,
            PlayerMana mana,
            CombatCalculator calculator)
        {
            this.database = database;
            this.wizard = wizard;
            this.enemySpawner = enemySpawner;
            this.projectileFactory = projectileFactory;
            this.mana = mana;
            this.calculator = calculator;
            LoadOwnedSkills(null);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < slots.Length; i++)
                slots[i]?.Tick(deltaTime);
        }

        public void LoadOwnedSkills(IReadOnlyList<string> ownedSkillIds)
        {
            List<SkillDefinition> owned = new List<SkillDefinition>();
            if (database != null)
            {
                IReadOnlyList<string> ids = ownedSkillIds != null && ownedSkillIds.Count > 0 ? ownedSkillIds : SkillId.DefaultOwned;
                for (int i = 0; i < ids.Count; i++)
                {
                    SkillDefinition skill = database.GetById(ids[i]);
                    if (skill != null && !owned.Contains(skill))
                        owned.Add(skill);
                }
            }

            OwnedSkills = owned;
        }

        public void LoadEquippedSlots(IReadOnlyList<string> equippedSkillSlots)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                string id = equippedSkillSlots != null && i < equippedSkillSlots.Count ? equippedSkillSlots[i] : string.Empty;
                EquipSkill(i, id, false);
            }
        }

        public List<string> CaptureOwnedSkillIds()
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < OwnedSkills.Count; i++)
            {
                SkillDefinition skill = OwnedSkills[i];
                if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                    ids.Add(skill.skillId);
            }
            return ids;
        }

        public List<string> CaptureEquippedSkillSlots()
        {
            List<string> ids = new List<string>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
                ids.Add(GetEquippedSkillId(i));
            return ids;
        }

        public void EquipSkill(int slotIndex, string skillId)
        {
            EquipSkill(slotIndex, skillId, true);
        }

        public string GetEquippedSkillId(int slotIndex)
        {
            SkillRuntime runtime = GetSlot(slotIndex);
            return runtime != null && runtime.Definition != null ? runtime.Definition.skillId : string.Empty;
        }

        public SkillRuntime GetSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCount ? slots[slotIndex] : null;
        }

        public void TickAutoCast(PlayerMana manaOverride = null)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (TryCastSlot(i, manaOverride ?? mana))
                    return;
            }
        }

        public bool TryManualCast(int slotIndex)
        {
            return TryCastSlot(slotIndex, mana);
        }

        private void EquipSkill(int slotIndex, string skillId, bool notify)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return;

            if (!string.IsNullOrEmpty(skillId))
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (i != slotIndex && GetEquippedSkillId(i) == skillId)
                    {
                        slots[i] = null;
                        if (notify)
                            SlotChanged?.Invoke(i, null);
                    }
                }
            }

            SkillDefinition definition = database != null ? database.GetById(skillId) : null;
            slots[slotIndex] = definition != null ? new SkillRuntime(definition) : null;
            if (notify)
                SlotChanged?.Invoke(slotIndex, definition);
        }

        private bool TryCastSlot(int slotIndex, PlayerMana manaForCast)
        {
            SkillRuntime runtime = GetSlot(slotIndex);
            if (runtime == null || !runtime.IsReady(manaForCast) || wizard == null || enemySpawner == null || calculator == null)
                return false;

            EnemyBase target = enemySpawner.GetNearestEnemy(wizard.transform.position);
            if (target == null || !target.IsAlive)
                return false;

            if (!runtime.TrySpendAndStart(manaForCast))
                return false;

            Cast(runtime.Definition, target);
            return true;
        }

        private void Cast(SkillDefinition skill, EnemyBase target)
        {
            if (skill == null || target == null || !target.IsAlive)
                return;

            Vector3 castPosition = wizard != null ? wizard.CastPoint.position : transform.position;
            SpawnVfx(skill.castVfxPrefab, castPosition);

            if (skill.targeting == SkillTargetingMode.DashToNearestEnemy)
            {
                if (!isDashing)
                    StartCoroutine(DashAndImpact(skill, target));
                return;
            }

            if (projectileFactory != null)
                projectileFactory.FireSkill(castPosition, target, calculator.Skill(wizard.gameObject, skill.damageCoefficient));
            StartCoroutine(DelayedImpact(skill, target, 0.25f));
        }

        private IEnumerator DashAndImpact(SkillDefinition skill, EnemyBase target)
        {
            isDashing = true;
            Vector3 start = wizard.transform.position;
            Vector3 end = target != null ? target.HitTransform.position : start;
            float elapsed = 0f;
            const float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                wizard.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            ApplySkillDamage(skill, target);
            isDashing = false;
        }

        private IEnumerator DelayedImpact(SkillDefinition skill, EnemyBase target, float delay)
        {
            yield return new WaitForSeconds(delay);
            ApplySkillDamage(skill, target);
        }

        private void ApplySkillDamage(SkillDefinition skill, EnemyBase target)
        {
            if (skill == null || target == null || !target.IsAlive || calculator == null || wizard == null)
                return;

            Vector3 impactPosition = target.HitTransform.position;
            SpawnVfx(skill.impactVfxPrefab, impactPosition);

            DamageInfo damage = calculator.Skill(wizard.gameObject, skill.damageCoefficient);
            if (skill.aoeRadius > 0f)
            {
                IReadOnlyList<EnemyBase> enemies = enemySpawner.ActiveEnemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyBase enemy = enemies[i];
                    if (enemy != null && enemy.IsAlive && Vector3.Distance(enemy.HitTransform.position, impactPosition) <= skill.aoeRadius)
                        enemy.TakeDamage(damage);
                }
            }
            else
            {
                target.TakeDamage(damage);
            }
        }

        private void SpawnVfx(ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null)
                return;

            ParticleSystem instance = Instantiate(prefab, position, Quaternion.identity);
            instance.Play(true);
            ParticleSystem.MainModule main = instance.main;
            Destroy(instance.gameObject, main.duration + main.startLifetime.constantMax + 0.25f);
        }
    }
}
