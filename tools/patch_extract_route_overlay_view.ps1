$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

$start = $text.IndexOf("        private sealed class RouteOverlayView")
if ($start -lt 0) {
    Write-Host "RouteOverlayView nested class is already removed."
    exit 0
}

$endMarker = "`r`n`r`n        // Battle setup"
$end = $text.IndexOf($endMarker, $start)
if ($end -lt 0) {
    $endMarker = "`n`n        // Battle setup"
    $end = $text.IndexOf($endMarker, $start)
}

if ($end -lt 0) {
    throw "Could not find end marker after nested RouteOverlayView."
}

$text = $text.Remove($start, $end - $start)
Set-Content -Path $path -Value $text -Encoding UTF8

Write-Host "Removed nested RouteOverlayView from BattleUIManager.cs."
