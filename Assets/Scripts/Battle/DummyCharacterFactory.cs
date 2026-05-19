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

            return data;
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
