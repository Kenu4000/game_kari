namespace GameKari.Battle
{
    /// <summary>
    /// 現在の仮クエストを作るFactory。
    /// まだクエスト選択やScriptableObject化は行わない。
    /// </summary>
    public static class DefaultQuestFactory
    {
        public static QuestData CreateDefaultQuest()
        {
            QuestData quest = new QuestData
            {
                TargetDistance = 100,
                BaseWaveDistance = 20,
                OneTurnClearPartyHeal = 5
            };

            quest.Waves.Add(DefaultWaveFactory.CreateDefaultWave());

            return quest;
        }
    }
}
