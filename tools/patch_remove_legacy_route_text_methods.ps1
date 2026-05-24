$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        Write-Host "Already removed or not found: $Label"
        return $Source
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

    while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
        $end++
    }

    return $Source.Remove($start, $end - $start)
}

$methods = @(
    @{ Signature = "        private string GetQuestRouteTitleText()"; Label = "GetQuestRouteTitleText" },
    @{ Signature = "        private string GetCurrentRoutePointText()"; Label = "GetCurrentRoutePointText" },
    @{ Signature = "        private string BuildRouteBarText()"; Label = "BuildRouteBarText" },
    @{ Signature = "        private static string GetRoutePointSymbol(RoutePointData point)"; Label = "GetRoutePointSymbol" },
    @{ Signature = "        private string GetNextImportantRoutePointText()"; Label = "GetNextImportantRoutePointText" },
    @{ Signature = "        private static string GetRoutePointDisplayName(RoutePointData point)"; Label = "GetRoutePointDisplayName" },
    @{ Signature = "        private string BuildPreparationActionHintText(RoutePointData point)"; Label = "BuildPreparationActionHintText" },
    @{ Signature = "        private string BuildEnemyScoutStateText(RoutePointData point)"; Label = "BuildEnemyScoutStateText" },
    @{ Signature = "        private static string BuildEnemyPlacementSummary(WaveData wave)"; Label = "BuildEnemyPlacementSummary" }
)

foreach ($method in $methods) {
    $text = Remove-MethodByName -Source $text -Signature $method.Signature -Label $method.Label
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Removed legacy route text helper methods from BattleUIManager.cs."
