namespace GameKari.Battle
{
    public static class DefaultBattleUnitFactory
    {
        public static BattleUnit CreateAllyUnitById(string characterId)
        {
            CharacterData data = CharacterAssetProvider.CreateCharacterDataById(characterId);
            BattleUnit unit = new BattleUnit(data);
            UnitSkillInitializer.AddDefaultSkills(unit);
            return unit;
        }

        public static BattleUnit CreateEnemyUnitById(string characterId)
        {
            CharacterData data = CharacterAssetProvider.CreateCharacterDataById(characterId);
            return new BattleUnit(data);
        }
    }
}




