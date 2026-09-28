using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityUI : MonoBehaviour
{
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private int abilityIndex;
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay;

    private void Start()
    {
        CreateKeyBadge();
        if (abilityController == null) return;

        var ability = abilityController.Abilities[abilityIndex];
        if (ability != null && icon != null && ability.icon != null)
            icon.sprite = ability.icon;

        abilityController.OnCooldownUpdated += HandleCooldownUpdated;
    }

    private void CreateKeyBadge()
    {
        string[] keys = { "Q", "W", "E" };
        if (abilityIndex < 0 || abilityIndex >= keys.Length) return;

        GameObject badgeObject = new GameObject("Ability Key Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObject.transform.SetParent(transform, false);

        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(3f, -3f);
        badgeRect.sizeDelta = new Vector2(22f, 20f);

        Image badgeImage = badgeObject.GetComponent<Image>();
        badgeImage.color = new Color(0.04f, 0.06f, 0.07f, 0.9f);
        badgeImage.raycastTarget = false;

        GameObject labelObject = new GameObject("Key", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(badgeObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = keys[abilityIndex];
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 12f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (abilityController != null)
            abilityController.OnCooldownUpdated -= HandleCooldownUpdated;
    }

    private void HandleCooldownUpdated(AbilityData ability, float normalizedRemaining)
    {
        if (abilityController.Abilities[abilityIndex] != ability) return;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = normalizedRemaining;
    }
}

