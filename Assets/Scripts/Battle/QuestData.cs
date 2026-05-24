using System.Collections.Generic;

namespace GameKari.Battle
{
    /// <summary>
    /// 1クエスト分の進行用ランタイムデータ。
    /// 現時点ではScriptableObject化せず、DefaultQuestFactoryで仮生成する。
    /// </summary>
    public sealed class QuestData
    {
        public string QuestName = "Default Route Quest";

        // 固定ルート上の地点一覧。
        // 表示上はWaveではなく、Battle / Event / Boss地点として扱う。
        public readonly List<RoutePointData> RoutePoints = new();

        // 既存戦闘実装との互換用。
        // RoutePointData.WaveIndexから参照される固定戦闘データとして使う。
        public readonly List<WaveData> Waves = new();


        // 1Turn Kill時の生存者HP回復量。
        public int OneTurnClearPartyHeal = 5;
    }
}



