namespace GameKari.Battle
{
    public static class DummyBattleFactory
    {
        public static BattleUnit CreateAllyUnit(string name, int hp, int speed)
        {
            // Legacy compatibility for temporary callers.
            BattleUnit unit = CreateBaseUnit(name, hp, speed);
            DummySkillFactory.AddDefaultSkills(unit);
            return unit;
        }

        public static BattleUnit CreateAllyUnitById(string characterId)
        {
            CharacterData data = DummyCharacterFactory.CreateCharacterDataById(characterId);
            BattleUnit unit = new BattleUnit(data);
            DummySkillFactory.AddDefaultSkills(unit);
            return unit;
        }

        public static BattleUnit CreateEnemyUnit(string name, int hp, int speed)
        {
            return CreateBaseUnit(name, hp, speed);
        }

        private static BattleUnit CreateBaseUnit(string name, int hp, int speed)
        {
            CharacterData data = DummyCharacterFactory.CreateCharacterData(name, hp, speed);
            return new BattleUnit(data);
        }
    }
}
