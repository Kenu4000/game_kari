$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/CommandPanelController.cs"
if (!(Test-Path $path)) {
    throw "CommandPanelController.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-Second-And-LaterMethod {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    $first = $Source.IndexOf($Signature)
    if ($first -lt 0) {
        Write-Host "Not found: $Label"
        return $Source
    }

    $searchFrom = $first + $Signature.Length

    while ($true) {
        $start = $Source.IndexOf($Signature, $searchFrom)
        if ($start -lt 0) {
            break
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

        while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
            $end++
        }

        $Source = $Source.Remove($start, $end - $start)
        Write-Host "Removed duplicate: $Label"
        $searchFrom = $first + $Signature.Length
    }

    return $Source
}

function Remove-Second-And-LaterEnum {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    $first = $Source.IndexOf($Signature)
    if ($first -lt 0) {
        Write-Host "Not found: $Label"
        return $Source
    }

    $searchFrom = $first + $Signature.Length

    while ($true) {
        $start = $Source.IndexOf($Signature, $searchFrom)
        if ($start -lt 0) {
            break
        }

        $braceStart = $Source.IndexOf("{", $start)
        if ($braceStart -lt 0) {
            throw "Enum body start not found: $Label"
        }

        $braceEnd = $Source.IndexOf("}", $braceStart)
        if ($braceEnd -lt 0) {
            throw "Enum body end not found: $Label"
        }

        $end = $braceEnd + 1
        while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
            $end++
        }

        $Source = $Source.Remove($start, $end - $start)
        Write-Host "Removed duplicate: $Label"
        $searchFrom = $first + $Signature.Length
    }

    return $Source
}

function Insert-Before-IfMissing {
    param(
        [string]$Source,
        [string]$Needle,
        [string]$Anchor,
        [string]$Insertion,
        [string]$Label
    )

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

# Remove duplicate helper definitions accidentally inserted by repeated patches.
$text = Remove-Second-And-LaterEnum -Source $text -Signature "        private enum CommandPanelMode" -Label "CommandPanelMode"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private void UpdateRootButtonSelection(CommandPanelMode mode)" -Label "UpdateRootButtonSelection"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private static void SetButtonSelected(Button button, bool selected)" -Label "SetButtonSelected"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private static void SetButtonDisabledVisual(Button button, bool disabled)" -Label "SetButtonDisabledVisual"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private void ApplySkillCategorySprites(Button button, SkillData skill)" -Label "ApplySkillCategorySprites"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private SkillCategorySpriteSet FindSkillCategorySpriteSet(SkillCategory category)" -Label "FindSkillCategorySpriteSet"

# If previous cleanup removed too much, add only the category sprite helpers that were missing in the original error.
$categoryHelpers = @'
        private void ApplySkillCategorySprites(Button button, SkillData skill)
        {
            if (button == null || skill == null)
            {
                return;
            }

            UISpriteStateVisual visual = button.GetComponent<UISpriteStateVisual>();
            if (visual == null)
            {
                return;
            }

            SkillCategorySpriteSet spriteSet = FindSkillCategorySpriteSet(skill.Category);
            if (spriteSet == null)
            {
                return;
            }

            visual.SetSprites(
                spriteSet.NormalSprite,
                spriteSet.HoverSprite,
                spriteSet.SelectedSprite,
                spriteSet.DisabledSprite);
        }

        private SkillCategorySpriteSet FindSkillCategorySpriteSet(SkillCategory category)
        {
            if (skillCategorySprites == null)
            {
                return null;
            }

            for (int i = 0; i < skillCategorySprites.Length; i++)
            {
                SkillCategorySpriteSet spriteSet = skillCategorySprites[i];
                if (spriteSet != null && spriteSet.Category == category)
                {
                    return spriteSet;
                }
            }

            return null;
        }

'@

$text = Insert-Before-IfMissing `
    -Source $text `
    -Needle "private void ApplySkillCategorySprites(Button button, SkillData skill)" `
    -Anchor "        private void SetButtonAlpha(Button button, float alpha)" `
    -Insertion $categoryHelpers `
    -Label "missing category sprite helpers"

$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Deduplicated CommandPanelController sprite helper definitions."
