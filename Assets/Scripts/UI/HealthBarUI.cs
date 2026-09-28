using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider slider;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    private Canvas _canvas;
    private Camera _mainCamera;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (slider == null) slider = GetComponentInChildren<Slider>();
        if (followTarget == null && health != null) followTarget = health.transform;

        _canvas = GetComponent<Canvas>();
        _mainCamera = Camera.main;
        if (_canvas != null) _canvas.renderMode = RenderMode.WorldSpace;
    }

    private void Start()
    {
        if (health == null || slider == null) return;
        slider.maxValue = health.MaxHealth;
        slider.value = health.CurrentHealth;
        health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;
        if (_mainCamera == null) _mainCamera = Camera.main;

        transform.position = followTarget.position + worldOffset;
        if (_mainCamera != null)
            transform.rotation = Quaternion.LookRotation(
                _mainCamera.transform.forward,
                _mainCamera.transform.up);
    }
}
