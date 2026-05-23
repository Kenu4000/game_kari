namespace GameKari.Battle
{
    /// <summary>
    /// Quest固定ルート上の1地点を表すランタイム用データ。
    /// 初期実装ではScriptableObject化せず、DefaultQuestFactoryで仮生成する。
    /// </summary>
    public sealed class RoutePointData
    {
        public RoutePointType PointType;
        public string DisplayName;
        public string EventText;

        // Battle / Boss地点の場合、QuestData.Waves上のどの戦闘データを使うかを指す。
        // Start / Normal / Eventでは -1。
        public int WaveIndex = -1;

        public bool HasBattleData
        {
            get
            {
                return PointType == RoutePointType.Battle
                    || PointType == RoutePointType.Boss;
            }
        }
    }
}
