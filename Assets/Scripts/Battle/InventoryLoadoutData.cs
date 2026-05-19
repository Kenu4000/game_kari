using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    [Serializable]
    public class InventoryLoadoutEntry
    {
        public ItemData Item;
        public int Count;
    }

    [CreateAssetMenu(
        fileName = "InventoryLoadoutData",
        menuName = "GameKari/Battle/Inventory Loadout Data")]
    public class InventoryLoadoutData : ScriptableObject
    {
        public List<InventoryLoadoutEntry> Items = new();
    }
}





