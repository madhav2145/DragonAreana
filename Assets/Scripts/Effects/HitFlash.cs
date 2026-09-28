using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Canvas worldSpaceCanvasForPopups;

    private Color[] _originalColors;

    private void Awake()
    {
        _originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            _originalColors[i] = renderers[i].material.color;
    }

    private void OnEnable()
    {
        if (health != null) health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(float amount, Vector3 hitPoint)
    {
        StartCoroutine(FlashRoutine());
        SpawnPopup(amount, hitPoint);
    }

    private IEnumerator FlashRoutine()
    {
        foreach (var r in renderers) r.material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = _originalColors[i];
    }

    private void SpawnPopup(float amount, Vector3 hitPoint)
    {
        if (damagePopupPrefab == null || worldSpaceCanvasForPopups == null) return;
        var popup = Instantiate(damagePopupPrefab, hitPoint + Vector3.up * 2f, Quaternion.identity,
            worldSpaceCanvasForPopups.transform);
        popup.GetComponent<DamagePopupUI>()?.Setup(amount);
    }
}
