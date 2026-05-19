using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    public static class DummyItemCatalog
    {
        private const string PotionAssetPath = "Battle/Items/potion";
        private const string PassAssetPath = "Battle/Items/pass";

        public static List<InventoryItem> CreateDefaultItems()
        {
            return new List<InventoryItem>
            {
                new InventoryItem(
                    GetPotion(),
                    3
                ),

                new InventoryItem(
                    GetPass(),
                    99
                )
            };
        }

        private static ItemData GetPotion()
        {
            return LoadItemAsset(PotionAssetPath) ?? CreateHealItem(
                "potion",
                "Potion",
                "Heal the ally in front of the active unit.",
                20
            );
        }

        private static ItemData GetPass()
        {
            return LoadItemAsset(PassAssetPath) ?? CreatePassItem(
                "pass",
                "Pass",
                "End the current action. No MP is spent and no extra MP is recovered."
            );
        }

        private static ItemData LoadItemAsset(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                return null;
            }

            return Resources.Load<ItemData>(resourcesPath);
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
