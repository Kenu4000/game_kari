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
    -Signature "        private string BuildRouteMovementText()" `
    -Replacement @'
        private string BuildRouteMovementText()
        {
            return RouteOverlayTextBuilder.BuildRouteMovementText(_questProgress);
        }
'@ `
    -Label "BuildRouteMovementText"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private string BuildRouteEventText(RoutePointData point)" `
    -Replacement @'
        private string BuildRouteEventText(RoutePointData point)
        {
            return RouteOverlayTextBuilder.BuildRouteEventText(point);
        }
'@ `
    -Label "BuildRouteEventText"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private string BuildBattlePreparationText(RoutePointData point)" `
    -Replacement @'
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
