$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Required {
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Label
    )

    if (!$Source.Contains($Old)) {
        throw "Patch anchor not found: $Label"
    }

    return $Source.Replace($Old, $New)
}

$text = Replace-Required `
    -Source $text `
    -Old @'
        private string BuildRouteMovementText()
        {
            return
                $"{GetQuestRouteTitleText()}\n\n" +
                $"Current\n{GetCurrentRoutePointText()}\n\n" +
                $"Route\n{BuildRouteBarText()}\n\n" +
                $"{GetNextImportantRoutePointText()}\n\n" +
                "Action\n" +
                "Button: Move";
        }
'@ `
    -New @'
        private string BuildRouteMovementText()
        {
            return RouteOverlayTextBuilder.BuildRouteMovementText(_questProgress);
        }
'@ `
    -Label "BuildRouteMovementText"

$text = Replace-Required `
    -Source $text `
    -Old @'
        private string BuildRouteEventText(RoutePointData point)
        {
            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Route Event"
                : point.DisplayName;

            string eventText = point == null || string.IsNullOrEmpty(point.EventText)
                ? "An event occurs on the route."
                : point.EventText;

            return
                $"{displayName}\n\n" +
                $"{eventText}\n\n" +
                "After Event\n" +
                "Button: Next → Movement";
        }
'@ `
    -New @'
        private string BuildRouteEventText(RoutePointData point)
        {
            return RouteOverlayTextBuilder.BuildRouteEventText(point);
        }
'@ `
    -Label "BuildRouteEventText"

$text = Replace-Required `
    -Source $text `
    -Old @'
        private string BuildBattlePreparationText(RoutePointData point)
        {
            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Battle Point"
                : point.DisplayName;

            return
                $"{displayName}\n\n" +
                $"Party\n{BuildPartyOverviewText()}\n\n" +
                $"Kakera\n{_kakeraStock}/{MaxKakeraStock}\n\n" +
                $"Enemy Info\n{BuildEnemyScoutStateText(point)}\n\n" +
                $"{BuildPreparationActionHintText(point)}\n\n" +
                "Action\n" +
                "Button: Start Battle";
        }
'@ `
    -New @'
        private string BuildBattlePreparationText(RoutePointData point)
        {
            return RouteOverlayTextBuilder.BuildBattlePreparationText(
                point,
                BuildPartyOverviewText(),
                _kakeraStock,
                MaxKakeraStock,
                IsRoutePointScouted(point),
                GetWaveDataForRoutePoint(point));
        }
'@ `
    -Label "BuildBattlePreparationText"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to delegate route overlay text to RouteOverlayTextBuilder."
