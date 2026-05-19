namespace GameKari.Battle
{
    public enum ItemKind
    {
        Heal,
        Pass
    }

    [System.Serializable]
    public class ItemData
    {
        public string ItemId;
        public string ItemName;
        public string Description;
        public ItemKind Kind;
        public int HealAmount;
    }
}
