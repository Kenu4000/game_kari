$ErrorActionPreference = 'Stop'

$slotPath = 'Assets/Scripts/Battle/SwapSlotView.cs'
$commandPath = 'Assets/Scripts/Battle/CommandPanelController.cs'

foreach ($path in @($slotPath, $commandPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
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

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

# SwapSlotView: allow CommandPanelController to hide empty slots.
$slotText = Get-Content -Path $slotPath -Raw -Encoding UTF8
$slotText = InsertBeforeIfMissing $slotText 'public void SetVisible(bool visible)' '        public void SetUnit(BattleUnit unit, Action<BattleUnit> onClicked)' @'
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

'@ 'SwapSlotView.SetVisible'
Set-Content -Path $slotPath -Value $slotText -Encoding UTF8

# CommandPanelController: hide slots that have no reserve unit.
$text = Get-Content -Path $commandPath -Raw -Encoding UTF8
$text = ReplaceOptional $text @'
        private void BindSwapSlotViews()
        {
            for (int i = 0; i < swapSlotViews.Length; i++)
            {
                SwapSlotView slotView = swapSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                slotView.SetUnit(GetReserveAt(i), OnReserveClicked);
            }
        }
'@ @'
        private void BindSwapSlotViews()
        {
            for (int i = 0; i < swapSlotViews.Length; i++)
            {
                SwapSlotView slotView = swapSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                BattleUnit reserve = GetReserveAt(i);
                bool visible = reserve != null && !reserve.IsDead && reserve.Data != null;
                slotView.SetVisible(visible);

                if (visible)
                {
                    slotView.SetUnit(reserve, OnReserveClicked);
                }
            }
        }
'@ 'BindSwapSlotViews hide empty slots'

Set-Content -Path $commandPath -Value $text -Encoding UTF8
Write-Host 'Patched swap slots to hide empty slots.'
