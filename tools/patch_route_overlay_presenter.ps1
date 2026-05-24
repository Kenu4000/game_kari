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

# Replace route overlay fields with one presenter field.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private RouteOverlayView _routeMovementPanel;
        private RouteOverlayView _routeEventPanel;
        private RouteOverlayView _battlePreparationPanel;
'@ `
    -New @'
        private readonly RouteOverlayPresenter _routeOverlayPresenter = new();
'@ `
    -Label "route overlay presenter field"

# EnsureRouteOverlayPanels delegates to presenter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void EnsureRouteOverlayPanels()" `
    -Replacement @'
        private void EnsureRouteOverlayPanels()
        {
            _routeOverlayPresenter.Ensure(GetOverlayCanvas(), FindUiGameObjectByName);
        }
'@ `
    -Label "EnsureRouteOverlayPanels"

# HideRouteOverlayPanels delegates to presenter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void HideRouteOverlayPanels()" `
    -Replacement @'
        private void HideRouteOverlayPanels()
        {
            EnsureRouteOverlayPanels();
            _routeOverlayPresenter.HideAll();
        }
'@ `
    -Label "HideRouteOverlayPanels"

# PrepareRouteOverlay no longer needs a panel parameter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void PrepareRouteOverlayForOverlay(RouteOverlayView panel)" `
    -Replacement @'
        private void PrepareRouteOverlayForOverlay()
        {
            EnsureRouteOverlayPanels();

            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;

            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();
            HideResultPanel();
            HideRouteOverlayPanels();
        }
'@ `
    -Label "PrepareRouteOverlayForOverlay"

# Route movement display through presenter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ShowRouteMovementPanel()" `
    -Replacement @'
        private void ShowRouteMovementPanel()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = true;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay();
            _routeOverlayPresenter.ShowMovement(BuildRouteMovementText(), HandleRouteMovementMoveClicked);

            Debug.Log("[Route] Movement panel shown.");
        }
'@ `
    -Label "ShowRouteMovementPanel"

# Route event display through presenter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ShowRouteEventPanel(RoutePointData point)" `
    -Replacement @'
        private void ShowRouteEventPanel(RoutePointData point)
        {
            _showingRouteEvent = true;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay();

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Route Event"
                : point.DisplayName;

            _routeOverlayPresenter.ShowEvent(BuildRouteEventText(point), HandleRouteEventNextClicked);

            Debug.Log($"[Route] Event shown: {displayName}");
        }
'@ `
    -Label "ShowRouteEventPanel"

# Battle preparation display through presenter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ShowBattlePreparationPanel(RoutePointData point)" `
    -Replacement @'
        private void ShowBattlePreparationPanel(RoutePointData point)
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = true;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay();

            string title = point != null && point.PointType == RoutePointType.Boss
                ? "BOSS PREPARATION"
                : "BATTLE PREPARATION";

            _routeOverlayPresenter.ShowPreparation(title, BuildBattlePreparationText(point), CanScoutRoutePoint(point), HandlePreparationScoutClicked, HandleBattlePreparationStartClicked);

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Battle Point"
                : point.DisplayName;

            Debug.Log($"[Preparation] Shown for {displayName}.");
        }
'@ `
    -Label "ShowBattlePreparationPanel"

# Refresh preparation panel through presenter.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void RefreshBattlePreparationPanel(RoutePointData point)" `
    -Replacement @'
        private void RefreshBattlePreparationPanel(RoutePointData point)
        {
            EnsureRouteOverlayPanels();
            _routeOverlayPresenter.RefreshPreparation(BuildBattlePreparationText(point), CanScoutRoutePoint(point), HandlePreparationScoutClicked, HandleBattlePreparationStartClicked);
        }
'@ `
    -Label "RefreshBattlePreparationPanel"

# Add CanScout helper by replacing RefreshBattlePreparationButtons.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void RefreshBattlePreparationButtons(RoutePointData point)" `
    -Replacement @'
        private void RefreshBattlePreparationButtons(RoutePointData point)
        {
            EnsureRouteOverlayPanels();
            _routeOverlayPresenter.SetPreparationButtons(CanScoutRoutePoint(point), HandlePreparationScoutClicked, HandleBattlePreparationStartClicked);
        }

        private bool CanScoutRoutePoint(RoutePointData point)
        {
            return point != null
                && point.HasBattleData
                && !IsRoutePointScouted(point)
                && _kakeraStock > 0;
        }
'@ `
    -Label "RefreshBattlePreparationButtons"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to use RouteOverlayPresenter."
