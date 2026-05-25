$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/CommandPanelController.cs"
if (!(Test-Path $path)) {
    throw "CommandPanelController.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

if (!$text.Contains("using GameKari.UI;")) {
    $text = $text.Replace("using UnityEngine.UI;", "using UnityEngine.UI;`r`nusing GameKari.UI;")
}

$helpers = @'
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

if ($text.Contains("private void ApplySkillCategorySprites(Button button, SkillData skill)")) {
    Write-Host "Skill category sprite helper methods already exist."
}
else {
    $anchor = "        private void SetButtonAlpha(Button button, float alpha)"
    $index = $text.IndexOf($anchor)

    if ($index -lt 0) {
        $anchor = "        private string BuildSkillDescription(SkillData skill)"
        $index = $text.IndexOf($anchor)
    }

    if ($index -lt 0) {
        throw "Patch anchor not found: helper insertion point"
    }

    $text = $text.Substring(0, $index) + $helpers + $text.Substring($index)
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Fixed missing skill category sprite helper methods."
