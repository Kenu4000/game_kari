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

$replacement = @'
        private void StartBattleAtCurrentRoutePoint()
        {
            if (_questProgress == null)
            {
                ReturnToBase();
                return;
            }

            RoutePointData point = _questProgress.CurrentBattleRoutePoint;
            if (point == null)
            {
                ReturnToBase();
                return;
            }

            StopAllCoroutines();

            ResetOverlayAndPreviewBeforeBattleStart();
            ResetRouteAndResultFlagsForBattleStart();
            ResetBattleRuntimeStateForBattleStart();
            ReplaceEnemyWave(_questProgress.CurrentWave);
            StartCurrentWaveProgress();
            EnterInitialActorForStartedBattle();
            RestoreBattleCommandUi();
            RedrawBoard();

            Debug.Log($"[Route] Started battle point: {point.DisplayName} ({point.PointType}), WaveIndex={_questProgress.CurrentWaveIndex}.");
        }

        private void ResetOverlayAndPreviewBeforeBattleStart()
        {
            HideResultPanel();
            HideRouteOverlayPanels();
            HideActionOverlay();
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            ClearPendingActionFlashTargets();
            ClearPendingActionValuePopups();
        }

        private void ResetRouteAndResultFlagsForBattleStart()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;
        }

        private void ResetBattleRuntimeStateForBattleStart()
        {
            _battleEnded = false;
            _phase = BattlePhase.CommandSelect;
            _formationSettling = false;
            _hoveredSkill = null;

            _actedUnits.Clear();
            _turnNumbers.Clear();
            _previewEnemyActionStates.Clear();
        }

        private void StartCurrentWaveProgress()
        {
            EnsureWaveProgress();
            _waveProgress.StartWave();
            RecoverAllAllyMP();
            RebuildTurnOrder();
        }

        private void EnterInitialActorForStartedBattle()
        {
            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                EnterCommandSelect(nextAlly);
                return;
            }

            CheckBattleEnd();
        }

        private void RestoreBattleCommandUi()
        {
            if (commandPanel != null)
            {
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
                commandPanel.SetInteractable(true);
            }

            if (rotateButton != null)
            {
                rotateButton.gameObject.SetActive(true);
                rotateButton.interactable = true;
            }

            SetCommandUiVisible(true);
        }
'@

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void StartBattleAtCurrentRoutePoint()" `
    -Replacement $replacement `
    -Label "StartBattleAtCurrentRoutePoint"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Split route battle start flow into helper methods."
