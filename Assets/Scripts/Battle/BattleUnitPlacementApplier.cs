using System.Collections.Generic;

namespace GameKari.Battle
{
    internal static class BattleUnitPlacementApplier
    {
        public static void Apply(
            BattleGrid grid,
            bool isAlly,
            List<BattleUnitPlacement> placements,
            List<BattleUnit> units)
        {
            if (grid == null || placements == null || units == null)
            {
                return;
            }

            for (int i = 0; i < placements.Count; i++)
            {
                BattleUnitPlacement placement = placements[i];

                if (placement == null || placement.Unit == null)
                {
                    continue;
                }

                grid.SetUnit(isAlly, placement.Position, placement.Unit);
                units.Add(placement.Unit);
            }
        }

        public static void ClearEnemySide(BattleGrid grid, List<BattleUnit> enemies, List<BattleUnit> enemyReserves)
        {
            if (grid != null)
            {
                grid.SetUnit(false, GridPos.FrontTop, null);
                grid.SetUnit(false, GridPos.BackTop, null);
                grid.SetUnit(false, GridPos.FrontBottom, null);
                grid.SetUnit(false, GridPos.BackBottom, null);
            }

            enemies?.Clear();
            enemyReserves?.Clear();
        }

        public static void ReplaceEnemyWave(BattleGrid grid, WaveData wave, List<BattleUnit> enemies, List<BattleUnit> enemyReserves)
        {
            ClearEnemySide(grid, enemies, enemyReserves);

            if (wave == null)
            {
                wave = DefaultWaveFactory.CreateDefaultWave();
            }

            Apply(grid, false, wave.EnemyPlacements, enemies);

            if (wave.EnemyReserves != null && enemyReserves != null)
            {
                enemyReserves.AddRange(wave.EnemyReserves);
            }
        }
    }
}
