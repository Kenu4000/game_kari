using System.Collections.Generic;

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
            return new ItemData
            {
                ItemId = itemId,
                ItemName = itemName,
                Description = description,
                Kind = ItemKind.Heal,
                HealAmount = healAmount
            };
        }

        private static ItemData CreatePassItem(
            string itemId,
            string itemName,
            string description)
        {
            return new ItemData
            {
                ItemId = itemId,
                ItemName = itemName,
                Description = description,
                Kind = ItemKind.Pass,
                HealAmount = 0
            };
        }
    }
}
