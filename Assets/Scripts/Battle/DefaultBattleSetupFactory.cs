namespace GameKari.Battle
{
    public static class DefaultBattleSetupFactory
    {
        public static BattleSetupData CreateDefaultSetup()
        {
            BattleSetupData setup = new BattleSetupData();
            QuestData quest = DefaultQuestFactory.CreateDefaultQuest();
            QuestProgressState questProgress = new QuestProgressState(quest);

            ApplyQuestSettingsToSetup(setup, quest);
            CreateAllies(setup);
            CreateEnemies(setup, questProgress);
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

        private static void CreateEnemies(BattleSetupData setup, QuestProgressState questProgress)
        {
            WaveData wave = GetCurrentWave(questProgress);
            ApplyWaveDataToSetup(setup, wave);
        }

        private static WaveData GetCurrentWave(QuestProgressState questProgress)
        {
            if (questProgress == null || questProgress.CurrentWave == null)
            {
                return DefaultWaveFactory.CreateDefaultWave();
            }

            return questProgress.CurrentWave;
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



