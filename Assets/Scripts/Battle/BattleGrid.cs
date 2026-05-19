using System.Collections.Generic;

namespace GameKari.Battle
{
    public enum GridPos
    {
        FrontTop,
        BackTop,
        FrontBottom,
        BackBottom
    }

    public class BattleGrid
    {
        private readonly Dictionary<GridPos, BattleUnit> _allyGrid = new();
        private readonly Dictionary<GridPos, BattleUnit> _enemyGrid = new();

        public BattleGrid()
        {
            foreach (GridPos pos in System.Enum.GetValues(typeof(GridPos)))
            {
                _allyGrid[pos] = null;
                _enemyGrid[pos] = null;
            }
        }

        public void SetUnit(bool ally, GridPos pos, BattleUnit unit)
        {
            if (ally)
            {
                _allyGrid[pos] = unit;
            }
            else
            {
                _enemyGrid[pos] = unit;
            }

            if (unit != null)
            {
                unit.IsAlly = ally;
                unit.GridPos = pos;
            }
        }

        public BattleUnit GetUnit(bool ally, GridPos pos) => ally ? _allyGrid[pos] : _enemyGrid[pos];

        public IReadOnlyDictionary<GridPos, BattleUnit> AllyGrid => _allyGrid;
        public IReadOnlyDictionary<GridPos, BattleUnit> EnemyGrid => _enemyGrid;
    }
}




