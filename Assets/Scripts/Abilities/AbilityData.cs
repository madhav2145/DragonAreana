using UnityEngine;

public enum AbilityType { Fire, Fly, Tail, BasicAttack }

[CreateAssetMenu(fileName = "NewAbility", menuName = "Dragon/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName = "Ability";
    public AbilityType type;
    public Sprite icon;

    [Header("Stats")]
    public float damage = 10f;
    public float cooldown = 3f;
    public float range = 3f;
    public float castTime = 0.3f;
    [Header("Feel")]
    public string animationTrigger = "Attack";
    public AudioClip sfxClip;
}
