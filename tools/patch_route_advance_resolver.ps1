$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Replacement,
        [string]$Label
    )

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        if ($Source.Contains($Replacement.Trim())) {
            Write-Host "Already patched: $Label"
            return $Source
        }

        throw "Patch anchor not found: $Label"
    }

    $braceStart = $Source.IndexOf("{", $start)
    if ($braceStart -lt 0) {
        throw "Method body start not found: $Label"
    }

    $depth = 0
    $end = -1

    for ($i = $braceStart; $i -lt $Source.Length; $i++) {
        $char = $Source[$i]

        if ($char -eq '{') {
            $depth++
        }
        elseif ($char -eq '}') {
            $depth--
            if ($depth -eq 0) {
                $end = $i + 1
                break
            }
        }
    }

    if ($end -lt 0) {
        throw "Method body end not found: $Label"
    }

    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ContinueRouteAdvance()" `
    -Replacement @'
        private void ContinueRouteAdvance()
        {
            int routeCount = _questProgress == null || _questProgress.Quest == null || _questProgress.Quest.RoutePoints == null
                ? 0
                : _questProgress.Quest.RoutePoints.Count;
            bool hasNext = _questProgress != null && _questProgress.HasNextRoutePoint;
            int currentIndex = _questProgress == null ? -1 : _questProgress.CurrentRoutePointIndex;

            Debug.Log($"[Route] Advance start. CurrentIndex={currentIndex}, RouteCount={routeCount}, HasNext={hasNext}.");

            RouteAdvanceResult result = RouteAdvanceResolver.Advance(_questProgress);
            if (result != null)
            {
                for (int i = 0; i < result.Logs.Count; i++)
                {
                    Debug.Log(result.Logs[i]);
                }
            }

            if (result == null)
            {
                ShowQuestResultPanel();
                return;
            }

            switch (result.DestinationType)
            {
                case RouteAdvanceDestinationType.Event:
                    ShowRouteEventPanel(result.Point);
                    return;

                case RouteAdvanceDestinationType.BattlePreparation:
                    ShowBattlePreparationPanel(result.Point);
                    return;

                case RouteAdvanceDestinationType.QuestResult:
                default:
                    ShowQuestResultPanel();
                    return;
            }
        }
'@ `
    -Label "ContinueRouteAdvance"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to use RouteAdvanceResolver."
