using System.Collections.Generic;

namespace GameKari.Battle
{
    public sealed class DummyBattleSetupData
    {
        public readonly List<DummyBattleUnitPlacement> AllyPlacements = new();
        public readonly List<DummyBattleUnitPlacement> EnemyPlacements = new();
        public readonly List<BattleUnit> AllyReserves = new();
        public readonly List<BattleUnit> EnemyReserves = new();

        public BattleUnit FallbackActive;

        public BattleUnit EnemyA;
        public BattleUnit EnemyB;
        public BattleUnit EnemyC;
        public BattleUnit EnemyD;
        public BattleUnit EnemyReserve;
    }

    public sealed class DummyBattleUnitPlacement
    {
        public GridPos Position;
        public BattleUnit Unit;

        public DummyBattleUnitPlacement(GridPos position, BattleUnit unit)
        {
            Position = position;
            Unit = unit;
        }
    }

    public static class DummyBattleSetupFactory
    {
        public static DummyBattleSetupData CreateDefaultSetup()
        {
            DummyBattleSetupData setup = new DummyBattleSetupData();

            CreateAllies(setup);
            CreateEnemies(setup);

            return setup;
        }

        private static void CreateAllies(DummyBattleSetupData setup)
        {
            BattleUnit heroA = DummyBattleFactory.CreateUnit("Knight", 130, 12);
            BattleUnit heroB = DummyBattleFactory.CreateUnit("Mage", 80, 15);
            BattleUnit heroC = DummyBattleFactory.CreateUnit("Cleric", 90, 9);
            BattleUnit heroD = DummyBattleFactory.CreateUnit("Rogue", 95, 18);
            BattleUnit reserve = DummyBattleFactory.CreateUnit("Reserve", 100, 11);

            setup.AllyPlacements.Add(new DummyBattleUnitPlacement(GridPos.FrontTop, heroA));
            setup.AllyPlacements.Add(new DummyBattleUnitPlacement(GridPos.BackTop, heroB));
            setup.AllyPlacements.Add(new DummyBattleUnitPlacement(GridPos.FrontBottom, heroC));
            setup.AllyPlacements.Add(new DummyBattleUnitPlacement(GridPos.BackBottom, heroD));

            setup.AllyReserves.Add(reserve);
            setup.FallbackActive = heroA;
        }

        private static void CreateEnemies(DummyBattleSetupData setup)
        {
            BattleUnit enemyA = DummyBattleFactory.CreateUnit("Goblin A", 70, 10);
            BattleUnit enemyB = DummyBattleFactory.CreateUnit("Archer", 30, 13);
            BattleUnit enemyC = DummyBattleFactory.CreateUnit("Goblin B", 50, 8);
            BattleUnit enemyD = DummyBattleFactory.CreateUnit("Shaman", 25, 7);
            BattleUnit enemyReserve = DummyBattleFactory.CreateUnit("Enemy Reserve", 65, 11);

            setup.EnemyPlacements.Add(new DummyBattleUnitPlacement(GridPos.FrontTop, enemyA));
            setup.EnemyPlacements.Add(new DummyBattleUnitPlacement(GridPos.BackTop, enemyB));
            setup.EnemyPlacements.Add(new DummyBattleUnitPlacement(GridPos.FrontBottom, enemyC));
            setup.EnemyPlacements.Add(new DummyBattleUnitPlacement(GridPos.BackBottom, enemyD));

            setup.EnemyReserves.Add(enemyReserve);

            setup.EnemyA = enemyA;
            setup.EnemyB = enemyB;
            setup.EnemyC = enemyC;
            setup.EnemyD = enemyD;
            setup.EnemyReserve = enemyReserve;
        }
    }
}
