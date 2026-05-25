$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/CommandPanelController.cs"
if (!(Test-Path $path)) {
    throw "CommandPanelController.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Optional {
    param([string]$Source, [string]$Old, [string]$New, [string]$Label)
    if (!$Source.Contains($Old)) {
        Write-Host "Already replaced or not found: $Label"
        return $Source
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

# Replace direct GetComponent return behavior with auto-add behavior.
$text = Replace-Optional `
    -Source $text `
    -Old @'
            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                visual.SetSelected(selected);
            }
'@ `
    -New @'
            UISpriteStateVisual visual = EnsureButtonSpriteStateVisual(button);
            if (visual != null)
            {
                visual.SetSelected(selected);
            }
'@ `
    -Label "SetButtonSelected auto visual"

$text = Replace-Optional `
    -Source $text `
    -Old @'
            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                visual.SetDisabledVisual(disabled);
            }
'@ `
    -New @'
            UISpriteStateVisual visual = EnsureButtonSpriteStateVisual(button);
            if (visual != null)
            {
                visual.SetDisabledVisual(disabled);
            }
'@ `
    -Label "SetButtonDisabledVisual auto visual"

$text = Replace-Optional `
    -Source $text `
    -Old @'
            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual == null)
            {
                return;
            }
'@ `
    -New @'
            UISpriteStateVisual visual = EnsureButtonSpriteStateVisual(button);
            if (visual == null)
            {
                return;
            }
'@ `
    -Label "ApplySkillCategorySprites auto visual"

$helper = @'
        private static UISpriteStateVisual EnsureButtonSpriteStateVisual(Button button)
        {
            if (button == null)
            {
                return null;
            }

            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                return visual;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                return null;
            }

            return button.gameObject.AddComponent<UISpriteStateVisual>();
        }

'@

$text = Insert-Before-IfMissing `
    -Source $text `
    -Needle "private static UISpriteStateVisual EnsureButtonSpriteStateVisual(Button button)" `
    -Anchor "        private void SetButtonAlpha(Button button, float alpha)" `
    -Insertion $helper `
    -Label "EnsureButtonSpriteStateVisual helper"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched CommandPanelController to auto-add UISpriteStateVisual to buttons."
