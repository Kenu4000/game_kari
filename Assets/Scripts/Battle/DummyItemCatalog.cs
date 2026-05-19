using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    public static class DummyItemCatalog
    {
        public static List<InventoryItem> CreateDefaultItems()
        {
            return new List<InventoryItem>
            {
                new InventoryItem(
                    CreateHealItem(
                        "potion",
                        "Potion",
                        "Heal the ally in front of the active unit.",
                        20
                    ),
                    3
                ),

                new InventoryItem(
                    CreatePassItem(
                        "pass",
                        "Pass",
                        "End the current action. No MP is spent and no extra MP is recovered."
                    ),
                    99
                )
            };
        }

        private static ItemData CreateHealItem(
            string itemId,
            string itemName,
            string description,
            int healAmount)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();

            item.ItemId = itemId;
            item.ItemName = itemName;
            item.Description = description;
            item.Kind = ItemKind.Heal;
            item.HealAmount = healAmount;

            return item;
        }

        private static ItemData CreatePassItem(
            string itemId,
            string itemName,
            string description)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();

            item.ItemId = itemId;
            item.ItemName = itemName;
            item.Description = description;
            item.Kind = ItemKind.Pass;
            item.HealAmount = 0;

            return item;
        }
    }
}
