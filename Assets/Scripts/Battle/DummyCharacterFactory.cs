using UnityEngine;

namespace GameKari.Battle
{
    public static class DummyCharacterFactory
    {
        private const int DefaultMaxMP = 4;

        public static CharacterData CreateCharacterData(string name, int hp, int speed)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();

            data.Id = BuildCharacterId(name);
            data.DisplayName = name;
            data.MaxHP = hp;
            data.MaxMP = DefaultMaxMP;
            data.Speed = speed;

            AssignDefaultSkills(data);

            return data;
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
