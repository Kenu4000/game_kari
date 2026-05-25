$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/CommandPanelController.cs"
if (!(Test-Path $path)) {
    throw "CommandPanelController.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Required {
    param([string]$Source, [string]$Old, [string]$New, [string]$Label)
    if (!$Source.Contains($Old)) {
        if ($Source.Contains($New.Trim())) {
            Write-Host "Already replaced: $Label"
            return $Source
        }
        throw "Patch anchor not found: $Label"
    }
    return $Source.Replace($Old, $New)
}

function Insert-Before-IfMissing {
    param([string]$Source, [string]$Needle, [string]$Anchor, [string]$Insertion, [string]$Label)
    if ($Source.Contains($Needle)) {
        Write-Host "Already exists: $Label"
        return $Source
    }

    $index = $Source.IndexOf($Anchor)
    if ($index -lt 0) {
        throw "Patch anchor not found: $Label"
    }

    return $Source.Substring(0, $index) + $Insertion + $Source.Substring($index)
}

function Replace-MethodByName {
    param([string]$Source, [string]$Signature, [string]$Replacement, [string]$Label)
    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        if ($Source.Contains($Replacement.Trim())) {
            Write-Host "Already patched: $Label"
            return $Source
        }
        throw "Patch anchor not found: $Label"
    }

    $braceStart = $Source.IndexOf("{", $start)
    if ($braceStart -lt 0) { throw "Method body start not found: $Label" }

    $depth = 0
    $end = -1
    for ($i = $braceStart; $i -lt $Source.Length; $i++) {
        $char = $Source[$i]
        if ($char -eq '{') { $depth++ }
        elseif ($char -eq '}') {
            $depth--
            if ($depth -eq 0) {
                $end = $i + 1
                break
            }
        }
    }

    if ($end -lt 0) { throw "Method body end not found: $Label" }
    while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
        $end++
    }

    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

$text = Replace-Required `
    -Source $text `
    -Old @'
        [Header("Fixed Swap Buttons")]
        [SerializeField] private Button[] swapButtons = new Button[4];
'@ `
    -New @'
        [Header("Fixed Swap Buttons")]
        [SerializeField] private Button[] swapButtons = new Button[4];

        [Header("Swap Slot Views")]
        [SerializeField] private SwapSlotView[] swapSlotViews = new SwapSlotView[4];
'@ `
    -Label "swapSlotViews field"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void BindSwapButtons()" `
    -Replacement @'
        private void BindSwapButtons()
        {
            if (HasSwapSlotViews())
            {
                BindSwapSlotViews();
                return;
            }

            BindLegacySwapButtons();
        }

        private bool HasSwapSlotViews()
        {
            if (swapSlotViews == null)
            {
                return false;
            }

            for (int i = 0; i < swapSlotViews.Length; i++)
            {
                if (swapSlotViews[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

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

        private void BindLegacySwapButtons()
        {
            if (swapButtons == null)
            {
                return;
            }

            for (int i = 0; i < swapButtons.Length; i++)
            {
                Button button = swapButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();

                BattleUnit reserve = GetReserveAt(i);
                if (reserve == null)
                {
                    SetButtonLabel(button, "-");
                    button.interactable = false;
                    button.gameObject.SetActive(true);
                    continue;
                }

                button.gameObject.SetActive(true);
                button.interactable = true;
                SetButtonLabel(button, $"{reserve.Name} HP:{reserve.CurrentHP}");

                button.onClick.AddListener(() => OnReserveClicked?.Invoke(reserve));
            }
        }
'@ `
    -Label "BindSwapButtons"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched CommandPanelController to support SwapSlotView arrays."
