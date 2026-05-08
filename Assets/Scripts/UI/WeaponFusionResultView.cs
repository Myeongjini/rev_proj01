using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WizardGrower.Weapons;

namespace WizardGrower.UI
{
    public class WeaponFusionResultView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float visibleSeconds = 1.8f;

        private float hideAt;

        private void Awake()
        {
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
            if (group == null)
                group = GetComponent<CanvasGroup>();
            Hide();
        }

        private void Update()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt)
                Hide();
        }

        public void Show(IReadOnlyList<WeaponFusionResult> results, WeaponDatabase database)
        {
            if (label == null)
                return;

            if (results == null || results.Count == 0)
            {
                label.text = "합성 가능한 무기가 없습니다.";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("무기 합성 완료");
                for (int i = 0; i < results.Count; i++)
                {
                    WeaponFusionResult result = results[i];
                    WeaponDefinition from = database != null ? database.GetById(result.fromWeaponId) : null;
                    WeaponDefinition to = database != null ? database.GetById(result.toWeaponId) : null;
                    sb.AppendLine($"{NameOf(from, result.fromWeaponId)} x{result.times * 3} -> {NameOf(to, result.toWeaponId)} x{result.times}");
                }
                label.text = sb.ToString();
            }

            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            gameObject.SetActive(true);
            hideAt = Time.unscaledTime + visibleSeconds;
        }

        private void Hide()
        {
            hideAt = 0f;
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            if (label != null)
                label.text = string.Empty;
        }

        private static string NameOf(WeaponDefinition weapon, string fallback)
        {
            return weapon != null ? weapon.displayName : fallback;
        }
    }
}
