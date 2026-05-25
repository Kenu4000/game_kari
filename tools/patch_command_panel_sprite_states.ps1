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

function Insert-Before {
    param([string]$Source, [string]$Anchor, [string]$Insertion, [string]$Label)
    if ($Source.Contains($Insertion.Trim())) {
        Write-Host "Already inserted: $Label"
        return $Source
    }
    $index = $Source.IndexOf($Anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $Label" }
    return $Source.Substring(0, $index) + $Insertion + $Source.Substring($index)
}

$text = Replace-Required `
    -Source $text `
    -Old @'
using TMPro;
using UnityEngine;
'@ `
    -New @'
using TMPro;
using UnityEngine;
using GameKari.UI;
'@ `
    -Label "using GameKari.UI"

$text = Replace-Required `
    -Source $text `
    -Old @'
        public void ShowSkills()
        {
            SetPanelStates(true, true, false, false);
        }
'@ `
    -New @'
        public void ShowSkills()
        {
            SetPanelStates(true, true, false, false);
            UpdateRootButtonSelection(CommandPanelMode.Skills);
        }
'@ `
    -Label "ShowSkills selection"

$text = Replace-Required `
    -Source $text `
    -Old @'
        public void ShowSwap()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, true, false);
        }
'@ `
    -New @'
        public void ShowSwap()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, true, false);
            UpdateRootButtonSelection(CommandPanelMode.Swap);
        }
'@ `
    -Label "ShowSwap selection"

$text = Replace-Required `
    -Source $text `
    -Old @'
        public void ShowItems()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, false, true);
        }
'@ `
    -New @'
        public void ShowItems()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, false, true);
            UpdateRootButtonSelection(CommandPanelMode.Items);
        }
'@ `
    -Label "ShowItems selection"

$text = Replace-Required `
    -Source $text `
    -Old @'
                    ResetButtonVisualState(button);
                    button.interactable = false;
'@ `
    -New @'
                    ResetButtonVisualState(button);
                    SetButtonDisabledVisual(button, true);
                    button.interactable = false;
'@ `
    -Label "null skill disabled visual"

$text = Replace-Required `
    -Source $text `
    -Old @'
            bool unavailable = !HasEnoughMP(skill) || !HasEnoughPartnerMP(skill) || (skill != null && skill.SkillKind == SkillKind.Link && !HasRequiredLinkPartner(skill));
            float alpha = unavailable ? 0.45f : 1f;

            SetButtonAlpha(button, alpha);
'@ `
    -New @'
            bool unavailable = !HasEnoughMP(skill) || !HasEnoughPartnerMP(skill) || (skill != null && skill.SkillKind == SkillKind.Link && !HasRequiredLinkPartner(skill));
            float alpha = unavailable ? 0.45f : 1f;

            SetButtonDisabledVisual(button, unavailable);
            SetButtonAlpha(button, alpha);
'@ `
    -Label "skill disabled visual"

$text = Replace-Required `
    -Source $text `
    -Old @'
        private void ResetButtonVisualState(Button button)
        {
            SetButtonAlpha(button, 1f);
        }
'@ `
    -New @'
        private void ResetButtonVisualState(Button button)
        {
            SetButtonDisabledVisual(button, false);
            SetButtonAlpha(button, 1f);
        }
'@ `
    -Label "ResetButtonVisualState"

$text = Insert-Before `
    -Source $text `
    -Anchor "        private void SetButtonAlpha(Button button, float alpha)" `
    -Insertion @'
        private enum CommandPanelMode
        {
            Skills,
            Swap,
            Items
        }

        private void UpdateRootButtonSelection(CommandPanelMode mode)
        {
            SetButtonSelected(fightButton, mode == CommandPanelMode.Skills);
            SetButtonSelected(swapButton, mode == CommandPanelMode.Swap);
            SetButtonSelected(itemButton, mode == CommandPanelMode.Items);
        }

        private static void SetButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                visual.SetSelected(selected);
            }
        }

        private static void SetButtonDisabledVisual(Button button, bool disabled)
        {
            if (button == null)
            {
                return;
            }

            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                visual.SetDisabledVisual(disabled);
            }
        }

'@ `
    -Label "sprite state helpers"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched CommandPanelController to drive UISpriteStateVisual selection/disabled states."
