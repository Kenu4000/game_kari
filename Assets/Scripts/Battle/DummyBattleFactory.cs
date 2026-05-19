namespace GameKari.Battle
{
    public static class DummyBattleFactory
    {
        public static BattleUnit CreateUnit(string name, int hp, int speed)
        {
            // Legacy compatibility: default unit creation is ally-style.
            return CreateAllyUnit(name, hp, speed);
        }

        public static BattleUnit CreateAllyUnit(string name, int hp, int speed)
        {
            BattleUnit unit = CreateBaseUnit(name, hp, speed);
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
