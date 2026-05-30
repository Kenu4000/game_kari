$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$refsPath = 'Assets/Scripts/Battle/BattleUIReferences.cs'

foreach ($path in @($managerPath, $refsPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

# BattleUIReferences: add editable turn order bar text reference.
$refs = Get-Content -Path $refsPath -Raw -Encoding UTF8
$refs = InsertBeforeIfMissing $refs 'public TMP_Text turnOrderBarText;' '        [Header("Generated Roots")]' @'
        [Header("Turn Order Bar")]
        public TMP_Text turnOrderBarText;

'@ 'BattleUIReferences turnOrderBarText'
Set-Content -Path $refsPath -Value $refs -Encoding UTF8

# BattleUIManager fields and reference binding.
$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

$text = InsertBeforeIfMissing $text 'private TMP_Text turnOrderBarText;' '        [Header("Status Panels")]' @'
        [Header("Turn Order Bar")]
        [SerializeField] private TMP_Text turnOrderBarText;
        [SerializeField] private string turnOrderSeparator = "  >  ";
        [SerializeField] private string turnOrderAllyPrefix = "A";
        [SerializeField] private string turnOrderEnemyPrefix = "E";
        [SerializeField] private string currentTurnPrefix = ">";
        [SerializeField] private string actedTurnPrefix = "x";

'@ 'BattleUIManager turn order bar fields'

$text = ReplaceOptional $text @'
            enemyStatusPanel = refs.enemyStatusPanel != null ? refs.enemyStatusPanel : enemyStatusPanel;
            allyStatusPanel = refs.allyStatusPanel != null ? refs.allyStatusPanel : allyStatusPanel;
'@ @'
            enemyStatusPanel = refs.enemyStatusPanel != null ? refs.enemyStatusPanel : enemyStatusPanel;
            allyStatusPanel = refs.allyStatusPanel != null ? refs.allyStatusPanel : allyStatusPanel;
            turnOrderBarText = refs.turnOrderBarText != null ? refs.turnOrderBarText : turnOrderBarText;
'@ 'ApplyBattleUIReferences turnOrderBarText'

$text = ReplaceOptional $text @'
             RedrawStatusPanels();
             RedrawActiveHighlights();
'@ @'
             RedrawStatusPanels();
             RedrawTurnOrderBar();
             RedrawActiveHighlights();
'@ 'RedrawBoard calls RedrawTurnOrderBar'

# The previous replacement can fail if indentation differs. Try exact current indentation.
$text = ReplaceOptional $text @'
            RedrawStatusPanels();
            RedrawActiveHighlights();
'@ @'
            RedrawStatusPanels();
            RedrawTurnOrderBar();
            RedrawActiveHighlights();
'@ 'RedrawBoard calls RedrawTurnOrderBar exact'

$text = ReplaceOptional $text '            SetLabel(slot, "TurnNumber", GetTurnOrderText(unit));' '            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));' 'enemy status TurnNumber -> MP'
$text = ReplaceOptional $text '            SetLabel(slot, "TurnNumber", GetTurnOrderText(unit));' '            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));' 'ally status TurnNumber -> MP'

$turnBarMethods = @'
        private void RedrawTurnOrderBar()
        {
            if (turnOrderBarText == null)
            {
                return;
            }

            turnOrderBarText.text = BuildTurnOrderBarText();
        }

        private string BuildTurnOrderBarText()
        {
            if (_turnOrder == null || _turnOrder.TurnOrder == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;

            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit unit = order[i];
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                string sidePrefix = _allies.Contains(unit) ? turnOrderAllyPrefix : turnOrderEnemyPrefix;
                string statePrefix = string.Empty;

                if (unit == _active && _phase == BattlePhase.CommandSelect && !_actedUnits.Contains(unit))
                {
                    statePrefix = currentTurnPrefix;
                }
                else if (_actedUnits.Contains(unit))
                {
                    statePrefix = actedTurnPrefix;
                }

                parts.Add($"{statePrefix}{sidePrefix}:{unit.Name}");
            }

            return string.Join(turnOrderSeparator, parts);
        }

        private static string BuildBoardMpBadgeText(BattleUnit unit)
        {
            if (unit == null || unit.IsDead || unit.Data == null)
            {
                return string.Empty;
            }

            return Mathf.Max(0, unit.CurrentMP).ToString();
        }

'@

$text = InsertBeforeIfMissing $text 'private void RedrawTurnOrderBar()' '        private void RedrawActiveHighlights()' $turnBarMethods 'turn order bar and MP badge helpers'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched turn order bar and board MP badges.'
