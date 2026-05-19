using UnityEngine;

namespace GameKari.Battle
{
    public enum ItemKind
    {
        Heal,
        Pass
    }

    [CreateAssetMenu(
        fileName = "ItemData",
        menuName = "GameKari/Battle/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string ItemId;
        public string ItemName;
        [TextArea] public string Description;
        public ItemKind Kind;
        public int HealAmount;
    }
}
