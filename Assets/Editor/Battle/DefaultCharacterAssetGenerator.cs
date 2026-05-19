using GameKari.Battle;
using UnityEditor;
using UnityEngine;

namespace GameKari.Battle.Editor
{
    public static class DefaultCharacterAssetGenerator
    {
        private const string CharacterAssetDirectory = "Assets/Resources/Battle/Characters";

        [MenuItem("Tools/GameKari/Battle/Generate Default Character Assets")]
        public static void GenerateDefaultCharacterAssets()
        {
            EnsureDirectory(CharacterAssetDirectory);

            CreateOrUpdateCharacter(
                "knight",
                "knight",
                "Knight",
                130,
                4,
                12,
                new[]
                {
                    DefaultSkillAssetProvider.GetSlash(),
                    DefaultSkillAssetProvider.GetPierce(),
                    DefaultSkillAssetProvider.GetTwinHit(),
                    DefaultSkillAssetProvider.GetFocus()
                }
            );

            CreateOrUpdateCharacter(
                "mage",
                "mage",
                "Mage",
                80,
                4,
                15,
                new[]
                {
                    DefaultSkillAssetProvider.GetSlash(),
                    DefaultSkillAssetProvider.GetPierce(),
                    DefaultSkillAssetProvider.GetFocus()
                }
            );

            CreateOrUpdateCharacter(
                "cleric",
                "cleric",
                "Cleric",
                90,
                4,
                9,
                new[]
                {
                    DefaultSkillAssetProvider.GetSlash(),
                    DefaultSkillAssetProvider.GetFocus()
                }
            );

            CreateOrUpdateCharacter(
                "rogue",
                "rogue",
                "Rogue",
                95,
                4,
                18,
                new[]
                {
                    DefaultSkillAssetProvider.GetSlash(),
                    DefaultSkillAssetProvider.GetPierce(),
                    DefaultSkillAssetProvider.GetFocus()
                }
            );

            CreateOrUpdateCharacter(
                "reserve",
                "reserve",
                "Reserve",
                100,
                4,
                11,
                new[]
                {
                    DefaultSkillAssetProvider.GetSlash()
                }
            );

            CreateOrUpdateCharacter(
                "goblin_a",
                "goblin_a",
                "Goblin A",
                70,
                4,
                10,
                new[]
                {
                    DefaultSkillAssetProvider.GetEnemyClaw()
                },
                new[]
                {
                    CreateEnemyActionSlot(DefaultSkillAssetProvider.GetEnemyClaw(), 70),
                    CreateEnemyActionSlot(DefaultSkillAssetProvider.GetEnemyStrike(), 30)
                }
            );

            CreateOrUpdateCharacter(
                "archer",
                "archer",
                "Archer",
                30,
                4,
                13,
                new[]
                {
                    DefaultSkillAssetProvider.GetEnemyArrow()
                },
                new[]
                {
                    CreateEnemyActionSlot(DefaultSkillAssetProvider.GetEnemyArrow(), 100)
                }
            );

            CreateOrUpdateCharacter(
                "goblin_b",
                "goblin_b",
                "Goblin B",
                50,
                4,
                8,
                new[]
                {
                    DefaultSkillAssetProvider.GetEnemyBite()
                },
                new[]
                {
                    CreateEnemyActionSlot(DefaultSkillAssetProvider.GetEnemyBite(), 100)
                }
            );

            CreateOrUpdateCharacter(
                "shaman",
                "shaman",
                "Shaman",
                25,
                4,
                7,
                new[]
                {
                    DefaultSkillAssetProvider.GetEnemyHex()
                },
                new[]
                {
                    CreateEnemyActionSlot(DefaultSkillAssetProvider.GetEnemyHex(), 100)
                }
            );

            CreateOrUpdateCharacter(
                "enemy_reserve",
                "enemy_reserve",
                "Enemy Reserve",
                65,
                4,
                11,
                new[]
                {
                    DefaultSkillAssetProvider.GetEnemyStrike()
                },
                new[]
                {
                    CreateEnemyActionSlot(DefaultSkillAssetProvider.GetEnemyStrike(), 100)
                }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameKari] Default CharacterData assets generated.");
        }

        private static void CreateOrUpdateCharacter(
            string assetName,
            string characterId,
            string displayName,
            int maxHp,
            int maxMp,
            int speed,
            SkillData[] defaultSkills,
            EnemyActionSlot[] enemyActionSlots = null)
        {
            string path = $"{CharacterAssetDirectory}/{assetName}.asset";
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);

            if (character == null)
            {
                character = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(character, path);
            }

            character.Id = characterId;
            character.DisplayName = displayName;
            character.MaxHP = maxHp;
            character.MaxMP = maxMp;
            character.Speed = speed;

            character.DefaultSkills.Clear();
            if (defaultSkills != null)
            {
                for (int i = 0; i < defaultSkills.Length; i++)
                {
                    if (defaultSkills[i] != null)
                    {
                        character.DefaultSkills.Add(defaultSkills[i]);
                    }
                }
            }

            character.EnemyActionSlots.Clear();
            if (enemyActionSlots != null)
            {
                for (int i = 0; i < enemyActionSlots.Length; i++)
                {
                    if (enemyActionSlots[i] != null && enemyActionSlots[i].Skill != null && enemyActionSlots[i].Weight > 0)
                    {
                        character.EnemyActionSlots.Add(enemyActionSlots[i]);
                    }
                }
            }

            EditorUtility.SetDirty(character);
        }

        private static EnemyActionSlot CreateEnemyActionSlot(SkillData skill, int weight)
        {
            return new EnemyActionSlot
            {
                Skill = skill,
                Weight = weight
            };
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




