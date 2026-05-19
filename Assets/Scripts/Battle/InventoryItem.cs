namespace GameKari.Battle
{
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData Item;
        public int Count;

        public InventoryItem(ItemData item, int count)
        {
            Item = item;
            Count = count;
        }
    }
}




