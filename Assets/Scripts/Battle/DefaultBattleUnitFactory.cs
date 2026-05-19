namespace GameKari.Battle
{
    public static class DefaultBattleUnitFactory
    {
        public static BattleUnit CreateAllyUnitById(string characterId)
        {
            CharacterData data = DummyCharacterFactory.CreateCharacterDataById(characterId);
            BattleUnit unit = new BattleUnit(data);
            DummySkillFactory.AddDefaultSkills(unit);
            return unit;
        }

        public static BattleUnit CreateEnemyUnitById(string characterId)
        {
            CharacterData data = DummyCharacterFactory.CreateCharacterDataById(characterId);
            return new BattleUnit(data);
        }
    }
}



