using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    public static class DefaultInventoryProvider
    {
        private const string DefaultInventoryLoadoutPath = "Battle/Inventory/default_inventory";

        public static List<InventoryItem> CreateDefaultItems()
        {
            InventoryLoadoutData loadout = LoadRequiredInventoryLoadout(DefaultInventoryLoadoutPath);
            List<InventoryItem> items = new List<InventoryItem>();

            for (int i = 0; i < loadout.Items.Count; i++)
            {
                InventoryLoadoutEntry entry = loadout.Items[i];
                if (entry == null || entry.Item == null)
                {
                    throw new InvalidOperationException($"Inventory loadout entry is invalid at index: {i}");
                }

                if (entry.Count < 0)
                {
                    throw new InvalidOperationException($"Inventory loadout count is negative at index: {i}");
                }

                items.Add(new InventoryItem(entry.Item, entry.Count));
            }

            return items;
        }

        private static InventoryLoadoutData LoadRequiredInventoryLoadout(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                throw new InvalidOperationException("Inventory loadout path is empty.");
            }

            InventoryLoadoutData loadout = Resources.Load<InventoryLoadoutData>(resourcesPath);
            if (loadout != null)
            {
                return loadout;
            }

            throw new InvalidOperationException($"InventoryLoadoutData asset not found at Resources path: {resourcesPath}");
        }
    }
}

