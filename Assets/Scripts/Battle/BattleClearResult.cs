namespace GameKari.Battle
{
    internal enum BattleClearRank
    {
        OneTurn,
        TwoTurn,
        ThreeTurn,
        FourPlusTurn
    }

    internal sealed class BattleClearResult
    {
        public BattleClearRank Rank;
        public int PartyHealAmount;
        public int BattleNumber;
        public int TotalBattles;
        public bool HasNextWave;
    }
}
