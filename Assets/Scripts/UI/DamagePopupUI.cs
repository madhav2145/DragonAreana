using UnityEngine;
using TMPro;

public class DamagePopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float lifetime = 0.8f;

    private float _timer;
    private Color _startColor;

    public void Setup(float damage)
    {
        label.text = Mathf.RoundToInt(damage).ToString();
        _startColor = label.color;
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        if (Camera.main != null) transform.forward = Camera.main.transform.forward;

        _timer += Time.deltaTime;
        float t = _timer / lifetime;
        label.color = new Color(_startColor.r, _startColor.g, _startColor.b, 1f - t);

        if (_timer >= lifetime) Destroy(gameObject);
    }
}
