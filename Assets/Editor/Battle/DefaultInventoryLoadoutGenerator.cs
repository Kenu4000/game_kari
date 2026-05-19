using GameKari.Battle;
using UnityEditor;
using UnityEngine;

namespace GameKari.Battle.Editor
{
    public static class DefaultInventoryLoadoutGenerator
    {
        private const string InventoryAssetDirectory = "Assets/Resources/Battle/Inventory";
        private const string InventoryAssetPath = "Assets/Resources/Battle/Inventory/default_inventory.asset";
        private const string PotionAssetPath = "Assets/Resources/Battle/Items/potion.asset";
        private const string PassAssetPath = "Assets/Resources/Battle/Items/pass.asset";

        [MenuItem("Tools/GameKari/Battle/Generate Default Inventory Loadout")]
        public static void GenerateDefaultInventoryLoadout()
        {
            EnsureDirectory(InventoryAssetDirectory);

            ItemData potion = AssetDatabase.LoadAssetAtPath<ItemData>(PotionAssetPath);
            ItemData pass = AssetDatabase.LoadAssetAtPath<ItemData>(PassAssetPath);

            if (potion == null)
            {
                Debug.LogError($"[GameKari] Potion asset not found: {PotionAssetPath}");
                return;
            }

            if (pass == null)
            {
                Debug.LogError($"[GameKari] Pass asset not found: {PassAssetPath}");
                return;
            }

            InventoryLoadoutData loadout = AssetDatabase.LoadAssetAtPath<InventoryLoadoutData>(InventoryAssetPath);
            if (loadout == null)
            {
                loadout = ScriptableObject.CreateInstance<InventoryLoadoutData>();
                AssetDatabase.CreateAsset(loadout, InventoryAssetPath);
            }

            loadout.Items.Clear();
            loadout.Items.Add(new InventoryLoadoutEntry
            {
                Item = potion,
                Count = 3
            });

            loadout.Items.Add(new InventoryLoadoutEntry
            {
                Item = pass,
                Count = 99
            });

            EditorUtility.SetDirty(loadout);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GameKari] Default inventory loadout generated.");
        }

        private static void EnsureDirectory(string directory)
        {
            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string[] parts = directory.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
