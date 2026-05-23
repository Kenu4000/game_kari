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
                OneTurnClearPartyHeal = 5
            };

            quest.Waves.Add(DefaultWaveFactory.CreateDefaultWave());
            quest.Waves.Add(DefaultWaveFactory.CreateSecondWave());
            quest.Waves.Add(DefaultWaveFactory.CreateThirdWave());

            AddDefaultRoutePoints(quest);

            return quest;
        }
        private static void AddDefaultRoutePoints(QuestData quest)
        {
            if (quest == null)
            {
                return;
            }

            quest.RoutePoints.Add(CreateRoutePoint(RoutePointType.Start, "Start", -1));
            quest.RoutePoints.Add(CreateRoutePoint(RoutePointType.Normal, "Open Road", -1));
            quest.RoutePoints.Add(CreateRoutePoint(RoutePointType.Battle, "Raid 1", 0));
            quest.RoutePoints.Add(CreateRoutePoint(RoutePointType.Event, "Signal Event", -1, "A suspicious signal appears on the route."));
            quest.RoutePoints.Add(CreateRoutePoint(RoutePointType.Battle, "Raid 2", 1));
            quest.RoutePoints.Add(CreateRoutePoint(RoutePointType.Boss, "Boss", 2));
        }

        private static RoutePointData CreateRoutePoint(
            RoutePointType pointType,
            string displayName,
            int waveIndex,
            string eventText = "")
        {
            return new RoutePointData
            {
                PointType = pointType,
                DisplayName = displayName,
                WaveIndex = waveIndex,
                EventText = eventText
            };
        }
    }
}






