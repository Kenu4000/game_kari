namespace GameKari.Battle
{
    public static class DummyBattleFactory
    {
        public static BattleUnit CreateUnit(string name, int hp, int speed)
        {
            CharacterData data = DummyCharacterFactory.CreateCharacterData(name, hp, speed);

            BattleUnit unit = new BattleUnit(data);
            DummySkillFactory.AddDefaultSkills(unit);

            return unit;
        }
    }
}
