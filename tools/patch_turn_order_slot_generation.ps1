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

# BattleUIReferences fields.
$refs = Get-Content -Path $refsPath -Raw -Encoding UTF8
$refs = InsertBeforeIfMissing $refs 'public Transform turnOrderSlotContainer;' '        public TMP_Text turnOrderBarText;' @'
        public Transform turnOrderSlotContainer;
        public TurnOrderSlotView turnOrderSlotTemplate;

'@ 'BattleUIReferences turn order slot refs'
Set-Content -Path $refsPath -Value $refs -Encoding UTF8

# BattleUIManager fields.
$text = Get-Content -Path $managerPath -Raw -Encoding UTF8
$text = InsertBeforeIfMissing $text 'private Transform turnOrderSlotContainer;' '        [SerializeField] private TMP_Text turnOrderBarText;' @'
        [SerializeField] private Transform turnOrderSlotContainer;
        [SerializeField] private TurnOrderSlotView turnOrderSlotTemplate;
        [SerializeField] private bool hideTurnOrderSlotTemplateOnPlay = true;

'@ 'BattleUIManager generated turn order fields'

$text = InsertBeforeIfMissing $text 'private readonly List<TurnOrderSlotView> _generatedTurnOrderSlotViews' '        private readonly List<TMP_Text> _activeActionValuePopupLabels = new();' @'
        private readonly List<TurnOrderSlotView> _generatedTurnOrderSlotViews = new();

'@ 'generated turn order slot list'

$text = ReplaceOptional $text @'
            turnOrderBarText = refs.turnOrderBarText != null ? refs.turnOrderBarText : turnOrderBarText;
'@ @'
            turnOrderBarText = refs.turnOrderBarText != null ? refs.turnOrderBarText : turnOrderBarText;
            turnOrderSlotContainer = refs.turnOrderSlotContainer != null ? refs.turnOrderSlotContainer : turnOrderSlotContainer;
            turnOrderSlotTemplate = refs.turnOrderSlotTemplate != null ? refs.turnOrderSlotTemplate : turnOrderSlotTemplate;
'@ 'ApplyBattleUIReferences turn order slot refs'

$text = ReplaceOptional $text @'
        private void RedrawTurnOrderBar()
        {
            if (turnOrderBarText == null)
            {
                return;
            }

            turnOrderBarText.text = BuildTurnOrderBarText();
        }
'@ @'
        private void RedrawTurnOrderBar()
        {
            if (CanGenerateTurnOrderSlots())
            {
                RedrawGeneratedTurnOrderSlots();

                if (turnOrderBarText != null)
                {
                    turnOrderBarText.gameObject.SetActive(false);
                }

                return;
            }

            if (turnOrderBarText == null)
            {
                return;
            }

            turnOrderBarText.gameObject.SetActive(true);
            turnOrderBarText.text = BuildTurnOrderBarText();
        }
'@ 'RedrawTurnOrderBar generated priority'

$helpers = @'
        private bool CanGenerateTurnOrderSlots()
        {
            return turnOrderSlotContainer != null && turnOrderSlotTemplate != null;
        }

        private void RedrawGeneratedTurnOrderSlots()
        {
            List<BattleUnit> visibleOrder = GetVisibleTurnOrderUnits();
            EnsureGeneratedTurnOrderSlotCapacity(visibleOrder.Count);

            if (turnOrderSlotTemplate != null && hideTurnOrderSlotTemplateOnPlay)
            {
                turnOrderSlotTemplate.SetVisible(false);
            }

            for (int i = 0; i < _generatedTurnOrderSlotViews.Count; i++)
            {
                TurnOrderSlotView slotView = _generatedTurnOrderSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                bool visible = i < visibleOrder.Count;
                slotView.SetVisible(visible);
                if (!visible)
                {
                    continue;
                }

                BattleUnit unit = visibleOrder[i];
                bool isAlly = _allies.Contains(unit);
                bool isCurrent = unit == _active && _phase == BattlePhase.CommandSelect && !_actedUnits.Contains(unit);
                bool isActed = _actedUnits.Contains(unit);
                slotView.SetUnit(unit, isAlly, isCurrent, isActed);
            }
        }

        private List<BattleUnit> GetVisibleTurnOrderUnits()
        {
            var units = new List<BattleUnit>();

            if (_turnOrder == null || _turnOrder.TurnOrder == null)
            {
                return units;
            }

            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;
            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit unit = order[i];
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                units.Add(unit);
            }

            return units;
        }

        private void EnsureGeneratedTurnOrderSlotCapacity(int requiredCount)
        {
            if (turnOrderSlotContainer == null || turnOrderSlotTemplate == null)
            {
                return;
            }

            for (int i = _generatedTurnOrderSlotViews.Count; i < requiredCount; i++)
            {
                TurnOrderSlotView slotView = Instantiate(turnOrderSlotTemplate, turnOrderSlotContainer);
                slotView.name = $"TurnOrderSlot_{i + 1}";
                slotView.SetVisible(true);
                _generatedTurnOrderSlotViews.Add(slotView);
            }
        }

'@

$text = InsertBeforeIfMissing $text 'private bool CanGenerateTurnOrderSlots()' '        private string BuildTurnOrderBarText()' $helpers 'generated turn order slot helpers'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched turn order slot prefab generation support.'
