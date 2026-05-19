using System;
using UnityEngine;

namespace GameKari.Battle
{
    public static class DummyCharacterFactory
    {
        private const int DefaultMaxMP = 4;
        private const string CharacterAssetBasePath = "Battle/Characters/";

        public static CharacterData CreateCharacterDataById(string characterId)
        {
            CharacterData asset = LoadCharacterAsset(characterId);
            if (asset != null)
            {
                return asset;
            }

            throw new InvalidOperationException($"CharacterData asset not found for id: {characterId}");
        }

        public static CharacterData CreateCharacterData(string name, int hp, int speed)
        {
            string characterId = BuildCharacterId(name);
            CharacterData asset = LoadCharacterAsset(characterId);
            if (asset != null)
            {
                return asset;
            }

            return CreateRuntimeCharacterData(characterId, name, hp, speed);
        }

        private static CharacterData CreateRuntimeCharacterData(string characterId, string displayName, int hp, int speed)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();

            data.Id = characterId;
            data.DisplayName = displayName;
            data.MaxHP = hp;
            data.MaxMP = DefaultMaxMP;
            data.Speed = speed;

            AssignDefaultSkills(data);

            return data;
        }

        private static CharacterData LoadCharacterAsset(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            return Resources.Load<CharacterData>(CharacterAssetBasePath + characterId);
        }

        private static void AssignDefaultSkills(CharacterData data)
        {
            if (data == null)
            {
                return;
            }

            data.DefaultSkills.Clear();

            switch (data.Id)
            {
                case "knight":
                    data.DefaultSkills.Add(DummySkillCatalog.GetSlash());
                    data.DefaultSkills.Add(DummySkillCatalog.GetPierce());
                    data.DefaultSkills.Add(DummySkillCatalog.GetTwinHit());
                    data.DefaultSkills.Add(DummySkillCatalog.GetFocus());
                    break;

                case "mage":
                    data.DefaultSkills.Add(DummySkillCatalog.GetSlash());
                    data.DefaultSkills.Add(DummySkillCatalog.GetPierce());
                    data.DefaultSkills.Add(DummySkillCatalog.GetFocus());
                    break;

                case "cleric":
                    data.DefaultSkills.Add(DummySkillCatalog.GetSlash());
                    data.DefaultSkills.Add(DummySkillCatalog.GetFocus());
                    break;

                case "rogue":
                    data.DefaultSkills.Add(DummySkillCatalog.GetSlash());
                    data.DefaultSkills.Add(DummySkillCatalog.GetPierce());
                    data.DefaultSkills.Add(DummySkillCatalog.GetFocus());
                    break;

                default:
                    data.DefaultSkills.Add(DummySkillCatalog.GetSlash());
                    break;
            }
        }

        private static string BuildCharacterId(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            return name.ToLower().Replace(" ", "_");
        }
    }
}
