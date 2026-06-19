using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "The Guild/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public const string DefaultAssetPath = "Assets/Data/Generated/ItemDatabase.asset";

    public List<ItemDefinition> items = new();
    public List<ItemEffectDefinition> effects = new();

    public ItemDefinition? GetItem(int itemId)
    {
        foreach (var row in items)
        {
            if (row.itemId == itemId)
                return row;
        }

        return null;
    }

    public ItemDefinition? GetItemByKey(string itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
            return null;

        foreach (var row in items)
        {
            if (row.itemKey == itemKey)
                return row;
        }

        return null;
    }

    public IEnumerable<ItemEffectDefinition> GetEffects(int itemId) =>
        effects.Where(e => e.itemId == itemId);
}
