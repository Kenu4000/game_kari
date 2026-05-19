namespace GameKari.Battle
{
    public static class DummyCharacterFactory
    {
        public static CharacterData CreateCharacterData(string name, int hp, int speed)
        {
            return new CharacterData
            {
                Id = BuildCharacterId(name),
                DisplayName = name,
                MaxHP = hp,
                Speed = speed
            };
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
