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
            BattleUnit heroA = DummyBattleFactory.CreateAllyUnitById("knight");
            BattleUnit heroB = DummyBattleFactory.CreateAllyUnitById("mage");
            BattleUnit heroC = DummyBattleFactory.CreateAllyUnitById("cleric");
            BattleUnit heroD = DummyBattleFactory.CreateAllyUnitById("rogue");
            BattleUnit reserve = DummyBattleFactory.CreateAllyUnitById("reserve");

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
            BattleUnit enemyA = DummyBattleFactory.CreateEnemyUnitById("goblin_a");
            BattleUnit enemyB = DummyBattleFactory.CreateEnemyUnitById("archer");
            BattleUnit enemyC = DummyBattleFactory.CreateEnemyUnitById("goblin_b");
            BattleUnit enemyD = DummyBattleFactory.CreateEnemyUnitById("shaman");
            BattleUnit enemyReserve = DummyBattleFactory.CreateEnemyUnitById("enemy_reserve");

            setup.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontTop, enemyA));
            setup.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackTop, enemyB));
            setup.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontBottom, enemyC));
            setup.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackBottom, enemyD));

            setup.EnemyReserves.Add(enemyReserve);

            setup.EnemyA = enemyA;
            setup.EnemyB = enemyB;
            setup.EnemyC = enemyC;
            setup.EnemyD = enemyD;
            setup.EnemyReserve = enemyReserve;
        }
    }
}






