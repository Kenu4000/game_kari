namespace GameKari.Battle
{
    public static class DummyBattleFactory
    {
        public static BattleUnit CreateUnit(string name, int hp, int speed)
        {
            var data = new CharacterData
            {
                Id = name.ToLower().Replace(" ", "_"),
                DisplayName = name,
                MaxHP = hp,
                Speed = speed
            };

            var unit = new BattleUnit(data);
            DummySkillFactory.AddDefaultSkills(unit);

            return unit;
        }
    }
}
