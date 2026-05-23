namespace GameKari.Battle
{
    /// <summary>
    /// 固定ルート上の地点種別。
    /// Goalは使わず、最終地点はBossとして扱う。
    /// </summary>
    public enum RoutePointType
    {
        Start,
        Normal,
        Battle,
        Event,
        Boss
    }
}
