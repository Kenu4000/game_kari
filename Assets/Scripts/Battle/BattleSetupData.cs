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
