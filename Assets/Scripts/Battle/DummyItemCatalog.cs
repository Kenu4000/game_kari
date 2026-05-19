using System;
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
            return LoadRequiredItemAsset(PotionAssetPath);
        }

        private static ItemData GetPass()
        {
            return LoadRequiredItemAsset(PassAssetPath);
        }

        private static ItemData LoadRequiredItemAsset(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                throw new InvalidOperationException("Item asset path is empty.");
            }

            ItemData item = Resources.Load<ItemData>(resourcesPath);
            if (item != null)
            {
                return item;
            }

            throw new InvalidOperationException($"ItemData asset not found at Resources path: {resourcesPath}");
        }
    }
}
