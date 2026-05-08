using UnityEngine;
using WizardGrower.Combat;
using WizardGrower.Weapons;

namespace WizardGrower.Player
{
    public class WeaponVisualController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer wizardBody;
        [SerializeField] private Vector3 glyphLocalOffset = new Vector3(0.18f, 0.05f, -0.01f);

        private GameObject glyphChild;
        private WeaponInventory inventory;

        public void Bind(PlayerWizard wizard, WeaponInventory inventory, ProjectileFactory projectileFactory)
        {
            if (wizardBody == null && wizard != null)
                wizardBody = wizard.GetComponent<SpriteRenderer>();
            if (projectileFactory != null)
                projectileFactory.BindWeaponInventory(inventory);
            Bind(inventory);
        }

        public void Bind(WeaponInventory inventory)
        {
            if (this.inventory != null)
                this.inventory.EquippedChanged -= Apply;

            this.inventory = inventory;
            if (wizardBody == null)
                wizardBody = GetComponent<SpriteRenderer>();

            if (this.inventory != null)
            {
                this.inventory.EquippedChanged += Apply;
                Apply(this.inventory.Equipped);
            }
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.EquippedChanged -= Apply;
        }

        private void Apply(WeaponDefinition weapon)
        {
            if (wizardBody != null)
                wizardBody.color = weapon != null ? weapon.tintColor : Color.white;

            if (glyphChild == null)
            {
                glyphChild = new GameObject("WeaponGlyph");
                glyphChild.transform.SetParent(transform, false);
                glyphChild.transform.localPosition = glyphLocalOffset;
                glyphChild.transform.localScale = Vector3.one * 0.35f;
                SpriteRenderer renderer = glyphChild.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = wizardBody != null ? wizardBody.sortingOrder + 1 : 11;
            }

            SpriteRenderer glyphRenderer = glyphChild.GetComponent<SpriteRenderer>();
            if (glyphRenderer != null)
                glyphRenderer.sprite = weapon != null ? weapon.accessoryGlyph : null;
            glyphChild.SetActive(weapon != null && weapon.accessoryGlyph != null);
        }
    }
}
