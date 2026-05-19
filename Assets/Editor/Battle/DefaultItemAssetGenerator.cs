using GameKari.Battle;
using UnityEditor;
using UnityEngine;

namespace GameKari.Battle.Editor
{
    public static class DefaultItemAssetGenerator
    {
        private const string ItemAssetDirectory = "Assets/Resources/Battle/Items";

        [MenuItem("Tools/GameKari/Battle/Generate Default Item Assets")]
        public static void GenerateDefaultItemAssets()
        {
            EnsureDirectory(ItemAssetDirectory);

            CreateOrUpdateItem(
                "potion",
                "potion",
                "Potion",
                "Heal the ally in front of the active unit.",
                ItemKind.Heal,
                20
            );

            CreateOrUpdateItem(
                "pass",
                "pass",
                "Pass",
                "End the current action. No MP is spent and no extra MP is recovered.",
                ItemKind.Pass,
                0
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameKari] Default ItemData assets generated.");
        }

        private static void CreateOrUpdateItem(
            string assetName,
            string itemId,
            string itemName,
            string description,
            ItemKind kind,
            int healAmount)
        {
            string path = $"{ItemAssetDirectory}/{assetName}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.ItemId = itemId;
            item.ItemName = itemName;
            item.Description = description;
            item.Kind = kind;
            item.HealAmount = healAmount;

            EditorUtility.SetDirty(item);
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
