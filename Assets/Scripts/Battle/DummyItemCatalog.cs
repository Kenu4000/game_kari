using System.Collections.Generic;

namespace GameKari.Battle
{
    public static class DummyItemCatalog
    {
        public static List<ItemData> CreateDefaultItems()
        {
            return new List<ItemData>
            {
                CreateHealItem(
                    "potion",
                    "Potion",
                    "Heal the ally in front of the active unit.",
                    20,
                    3
                ),

                CreatePassItem(
                    "pass",
                    "Pass",
                    "End the current action. No MP is spent and no extra MP is recovered.",
                    99
                )
            };
        }

        private static ItemData CreateHealItem(
            string itemId,
            string itemName,
            string description,
            int healAmount,
            int count)
        {
            return new ItemData
            {
                ItemId = itemId,
                ItemName = itemName,
                Description = description,
                Kind = ItemKind.Heal,
                HealAmount = healAmount,
                Count = count
            };
        }

        private static ItemData CreatePassItem(
            string itemId,
            string itemName,
            string description,
            int count)
        {
            return new ItemData
            {
                ItemId = itemId,
                ItemName = itemName,
                Description = description,
                Kind = ItemKind.Pass,
                HealAmount = 0,
                Count = count
            };
        }
    }
}
