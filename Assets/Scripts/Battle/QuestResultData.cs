namespace GameKari.Battle
{
    /// <summary>
    /// Quest Clear / Quest Failed時に表示・集計するためのランタイム結果データ。
    /// Distance制は廃止し、固定Route上のBattle数・Kakera・EXPを中心に扱う。
    /// </summary>
    public sealed class QuestResultData
    {
        public int ClearedBattleCount;
        public int TotalBattleCount;
        public int TotalKakeraEarned;
        public int TotalExpEarned;
        public bool ReturnsToBase;
    }
}
