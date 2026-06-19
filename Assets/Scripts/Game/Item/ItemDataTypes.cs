using System;
using UnityEngine;

[Serializable]
public struct ItemDefinition
{
    public int itemId;
    public int itemKind;
    public string itemKey;
    public string displayName;
    public string iconSprite;
    public Sprite icon;
    public int itemTier;
    public int sellGold;
    public int stackMax;
    public int atk;
    public int def;
    public int equip;
}

[Serializable]
public struct ItemEffectDefinition
{
    public int itemId;
    public int effectId;
    public float value;
}
