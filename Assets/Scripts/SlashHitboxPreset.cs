using UnityEngine;

[CreateAssetMenu(fileName = "SlashHitboxPreset", menuName = "Combat/Slash Hitbox Preset")]
public class SlashHitboxPreset : ScriptableObject
{
    public SlashHitboxGroup slashA;
    public SlashHitboxGroup slashB;
}
