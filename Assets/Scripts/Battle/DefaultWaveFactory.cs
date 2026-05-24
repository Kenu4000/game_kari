namespace GameKari.Battle
{
    public static class DefaultWaveFactory
    {
        public static WaveData CreateDefaultWave()
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

        public static WaveData CreateSecondWave()
        {
            WaveData wave = new WaveData();

            BattleUnit enemyA = DefaultBattleUnitFactory.CreateEnemyUnitById("goblin_b");
            BattleUnit enemyB = DefaultBattleUnitFactory.CreateEnemyUnitById("shaman");
            BattleUnit enemyC = DefaultBattleUnitFactory.CreateEnemyUnitById("goblin_a");
            BattleUnit enemyD = DefaultBattleUnitFactory.CreateEnemyUnitById("archer");
            BattleUnit enemyReserve = DefaultBattleUnitFactory.CreateEnemyUnitById("enemy_reserve");

            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontTop, enemyA));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackTop, enemyB));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontBottom, enemyC));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackBottom, enemyD));

            wave.EnemyReserves.Add(enemyReserve);

            return wave;
        }

        public static WaveData CreateThirdWave()
        {
            WaveData wave = new WaveData();

            BattleUnit enemyA = DefaultBattleUnitFactory.CreateEnemyUnitById("shaman");
            BattleUnit enemyB = DefaultBattleUnitFactory.CreateEnemyUnitById("goblin_a");
            BattleUnit enemyC = DefaultBattleUnitFactory.CreateEnemyUnitById("archer");
            BattleUnit enemyD = DefaultBattleUnitFactory.CreateEnemyUnitById("goblin_b");
            BattleUnit enemyReserve = DefaultBattleUnitFactory.CreateEnemyUnitById("enemy_reserve");

            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontTop, enemyA));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackTop, enemyB));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.FrontBottom, enemyC));
            wave.EnemyPlacements.Add(new BattleUnitPlacement(GridPos.BackBottom, enemyD));

            wave.EnemyReserves.Add(enemyReserve);

            return wave;
        }
    }
}






