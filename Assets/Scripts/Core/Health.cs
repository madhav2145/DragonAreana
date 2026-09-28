using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool isDead;

    private Animator _animator;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, Vector3> OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        _animator = GetComponent<Animator>();
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (isDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnDamaged?.Invoke(amount, hitPoint);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f && !isDead)
        {
            isDead = true;
            if (_animator != null)
            {
                _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _animator.SetTrigger("Died");
            }
            OnDeath?.Invoke();
        }
    }

    public void ResetHealth()
    {
        isDead = false;
        CurrentHealth = maxHealth;
        if (_animator != null)
        {
            _animator.ResetTrigger("Died");
            _animator.updateMode = AnimatorUpdateMode.Normal;
        }
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
