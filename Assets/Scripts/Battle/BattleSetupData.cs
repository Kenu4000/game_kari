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

        // 1Turn Kill時の生存者HP回復量。
        public int OneTurnClearPartyHeal = 5;

        public QuestProgressState QuestProgress;
        public BattleUnit FallbackActive;
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
}




