namespace GameKari.Battle
{
    public static class DefaultBattleSetupFactory
    {
        public static BattleSetupData CreateDefaultSetup()
        {
            BattleSetupData setup = new BattleSetupData();
            QuestData quest = DefaultQuestFactory.CreateDefaultQuest();

            ApplyQuestSettingsToSetup(setup, quest);
            CreateAllies(setup);
            CreateEnemies(setup, quest);
            CreateInventory(setup);

            return setup;
        }

        private static void ApplyQuestSettingsToSetup(BattleSetupData setup, QuestData quest)
        {
            if (setup == null || quest == null)
            {
                return;
            }

            setup.TargetDistance = quest.TargetDistance;
            setup.OneTurnClearPartyHeal = quest.OneTurnClearPartyHeal;
        }
        private static void CreateAllies(BattleSetupData setup)
        {
            BattleUnit heroA = DefaultBattleUnitFactory.CreateAllyUnitById("knight");
            BattleUnit heroB = DefaultBattleUnitFactory.CreateAllyUnitById("mage");
            BattleUnit heroC = DefaultBattleUnitFactory.CreateAllyUnitById("cleric");
            BattleUnit heroD = DefaultBattleUnitFactory.CreateAllyUnitById("rogue");
            BattleUnit reserve = DefaultBattleUnitFactory.CreateAllyUnitById("reserve");

            setup.AllyPlacements.Add(new BattleUnitPlacement(GridPos.FrontTop, heroA));
            setup.AllyPlacements.Add(new BattleUnitPlacement(GridPos.BackTop, heroB));
            setup.AllyPlacements.Add(new BattleUnitPlacement(GridPos.FrontBottom, heroC));
            setup.AllyPlacements.Add(new BattleUnitPlacement(GridPos.BackBottom, heroD));

            setup.AllyReserves.Add(reserve);
            setup.FallbackActive = heroA;
        }

        private static void CreateInventory(BattleSetupData setup)
        {
            if (setup == null)
            {
                return;
            }

            setup.InventoryItems.AddRange(DefaultInventoryProvider.CreateDefaultItems());
        }

        private static void CreateEnemies(BattleSetupData setup, QuestData quest)
        {
            WaveData wave = GetFirstWave(quest);
            ApplyWaveDataToSetup(setup, wave);
        }

        private static WaveData GetFirstWave(QuestData quest)
        {
            if (quest == null || quest.Waves.Count == 0)
            {
                return DefaultWaveFactory.CreateDefaultWave();
            }

            return quest.Waves[0];
        }


        private static void ApplyWaveDataToSetup(BattleSetupData setup, WaveData wave)
        {
            if (setup == null || wave == null)
            {
                return;
            }

            setup.BaseWaveDistance = wave.BaseDistance;
            setup.EnemyPlacements.AddRange(wave.EnemyPlacements);
            setup.EnemyReserves.AddRange(wave.EnemyReserves);
        }
    }
}




