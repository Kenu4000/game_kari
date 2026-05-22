using System.Collections.Generic;

namespace GameKari.Battle
{
    public sealed class BattleSetupData
    {
        public readonly List<BattleUnitPlacement> AllyPlacements = new();
        public readonly List<BattleUnitPlacement> EnemyPlacements = new();
        public readonly List<BattleUnit> AllyReserves = new();
        public readonly List<BattleUnit> EnemyReserves = new();
        public readonly List<InventoryItem> InventoryItems = new();

        // 現在は単発Battleを1Waveとして扱うための仮設定。
        // 将来はQuestData / WaveData側へ移す。
        public int TargetDistance = 100;
        public int BaseWaveDistance = 20;
        public int OneTurnClearPartyHeal = 5;

        public BattleUnit FallbackActive;

        public BattleUnit EnemyA;
        public BattleUnit EnemyB;
        public BattleUnit EnemyC;
        public BattleUnit EnemyD;
        public BattleUnit EnemyReserve;
    }

    public sealed class BattleUnitPlacement
    {
        public GridPos Position;
        public BattleUnit Unit;

        public BattleUnitPlacement(GridPos position, BattleUnit unit)
        {
            Position = position;
            Unit = unit;
        }
    }

    public static class DefaultBattleSetupFactory
    {
        public static BattleSetupData CreateDefaultSetup()
        {
            BattleSetupData setup = new BattleSetupData();

            CreateAllies(setup);
            CreateEnemies(setup);
            CreateInventory(setup);

            return setup;
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

        private static void CreateEnemies(BattleSetupData setup)
        {
            WaveData wave = CreateDefaultWave();
            ApplyWaveDataToSetup(setup, wave);
        }

        private static WaveData CreateDefaultWave()
        {
            WaveData wave = new WaveData();

            BattleUnit enemyA = DefaultBattleUnitFactory.CreateEnemyUnitById("goblin_a");
            BattleUnit enemyB = DefaultBattleUnitFactory.CreateEnemyUnitById("archer");
            BattleUnit enemyC = DefaultBattleUnitFactory.CreateEnemyUnitById("goblin_b");
            BattleUnit enemyD = DefaultBattleUnitFactory.CreateEnemyUnitById("shaman");
            BattleUnit enemyReserve = DefaultBattleUnitFactory.CreateEnemyUnitById("enemy_reserve");

            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontTop, enemyA));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackTop, enemyB));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontBottom, enemyC));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackBottom, enemyD));

            wave.EnemyReserves.Add(enemyReserve);

            return wave;
        }

        private static void ApplyWaveDataToSetup(BattleSetupData setup, WaveData wave)
        {
            if (setup == null || wave == null)
            {
                return;
            }

            setup.EnemyPlacements.AddRange(wave.EnemyPlacements);
            setup.EnemyReserves.AddRange(wave.EnemyReserves);

            SetLegacyEnemyReferences(setup, wave);
        }

        private static void SetLegacyEnemyReferences(BattleSetupData setup, WaveData wave)
        {
            if (setup == null || wave == null)
            {
                return;
            }

            setup.EnemyA = GetPlacedEnemy(wave, GridPos.FrontTop);
            setup.EnemyB = GetPlacedEnemy(wave, GridPos.BackTop);
            setup.EnemyC = GetPlacedEnemy(wave, GridPos.FrontBottom);
            setup.EnemyD = GetPlacedEnemy(wave, GridPos.BackBottom);
            setup.EnemyReserve = wave.EnemyReserves.Count > 0
                ? wave.EnemyReserves[0]
                : null;
        }

        private static BattleUnit GetPlacedEnemy(WaveData wave, GridPos position)
        {
            if (wave == null)
            {
                return null;
            }

            for (int i = 0; i < wave.EnemyPlacements.Count; i++)
            {
                BattleUnitPlacement placement = wave.EnemyPlacements[i];
                if (placement != null && placement.Position == position)
                {
                    return placement.Unit;
                }
            }

            return null;
        }
    }
}











