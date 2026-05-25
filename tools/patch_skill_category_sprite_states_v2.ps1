$ErrorActionPreference = "Stop"

$skillPath = "Assets/Scripts/Battle/SkillData.cs"
$commandPath = "Assets/Scripts/Battle/CommandPanelController.cs"
$visualPath = "Assets/Scripts/UI/UISpriteStateVisual.cs"

foreach ($path in @($skillPath, $commandPath, $visualPath)) {
    if (!(Test-Path $path)) {
        throw "Required file not found: $path"
    }
}

function Replace-Optional {
    param([string]$Source, [string]$Old, [string]$New, [string]$Label)
    if (!$Source.Contains($Old)) {
        Write-Host "Already replaced or not found: $Label"
        return $Source
    }
    return $Source.Replace($Old, $New)
}

function Insert-Before-Optional {
    param([string]$Source, [string]$Anchor, [string]$Insertion, [string]$AlreadyContains, [string]$Label)
    if ($Source.Contains($AlreadyContains)) {
        Write-Host "Already inserted: $Label"
        return $Source
    }
    $index = $Source.IndexOf($Anchor)
    if ($index -lt 0) {
        Write-Host "Anchor not found, skipped: $Label"
        return $Source
    }
    return $Source.Substring(0, $index) + $Insertion + $Source.Substring($index)
}

# SkillData: add category enum and field if missing.
$skillText = Get-Content -Path $skillPath -Raw -Encoding UTF8
$skillText = Insert-Before-Optional `
    -Source $skillText `
    -Anchor "    public enum SkillTargetPattern" `
    -Insertion @'
    public enum SkillCategory
    {
        Attack,
        Heal,
        Change,
        Defense
    }

'@ `
    -AlreadyContains "public enum SkillCategory" `
    -Label "SkillCategory enum"

$skillText = Replace-Optional `
    -Source $skillText `
    -Old @'
        public string SkillId;
        public string SkillName;
        [TextArea] public string Description;
        public SkillTargetPattern TargetPattern;
'@ `
    -New @'
        public string SkillId;
        public string SkillName;
        [TextArea] public string Description;
        public SkillCategory Category = SkillCategory.Attack;
        public SkillTargetPattern TargetPattern;
'@ `
    -Label "SkillData Category field"
Set-Content -Path $skillPath -Value $skillText -Encoding UTF8

# UISpriteStateVisual: add runtime sprite-set method if missing.
$visualText = Get-Content -Path $visualPath -Raw -Encoding UTF8
$visualText = Insert-Before-Optional `
    -Source $visualText `
    -Anchor "        public void SetSelected(bool selected)" `
    -Insertion @'
        public void SetSprites(Sprite normal, Sprite hover, Sprite selected, Sprite disabled)
        {
            normalSprite = normal != null ? normal : normalSprite;
            hoverSprite = hover;
            selectedSprite = selected;
            disabledSprite = disabled;
            ApplyCurrentSprite();
        }

'@ `
    -AlreadyContains "public void SetSprites(Sprite normal" `
    -Label "UISpriteStateVisual.SetSprites"
Set-Content -Path $visualPath -Value $visualText -Encoding UTF8

# CommandPanelController: robust updates.
$commandText = Get-Content -Path $commandPath -Raw -Encoding UTF8

if (!$commandText.Contains("using GameKari.UI;")) {
    $commandText = $commandText.Replace("using UnityEngine.UI;", "using UnityEngine.UI;`r`nusing GameKari.UI;")
}

$commandText = Replace-Optional `
    -Source $commandText `
    -Old @'
    public class CommandPanelController : MonoBehaviour
    {
'@ `
    -New @'
    public class CommandPanelController : MonoBehaviour
    {
        [Serializable]
        private sealed class SkillCategorySpriteSet
        {
            public SkillCategory Category;
            public Sprite NormalSprite;
            public Sprite HoverSprite;
            public Sprite SelectedSprite;
            public Sprite DisabledSprite;
        }

'@ `
    -Label "SkillCategorySpriteSet class"

$commandText = Replace-Optional `
    -Source $commandText `
    -Old @'
        [Header("Fixed Skill Buttons")]
        [SerializeField] private Button[] skillButtons = new Button[4];
'@ `
    -New @'
        [Header("Fixed Skill Buttons")]
        [SerializeField] private Button[] skillButtons = new Button[4];

        [Header("Skill Button Sprites By Category")]
        [SerializeField] private SkillCategorySpriteSet[] skillCategorySprites = new SkillCategorySpriteSet[4];
'@ `
    -Label "skillCategorySprites field"

$commandText = Replace-Optional `
    -Source $commandText `
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

$commandText = Replace-Optional `
    -Source $commandText `
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

$commandText = Replace-Optional `
    -Source $commandText `
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

$commandText = Replace-Optional `
    -Source $commandText `
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

$commandText = Replace-Optional `
    -Source $commandText `
    -Old @'
                string label = BuildSkillButtonLabel(skill);
                SetButtonLabel(button, label);
                ApplySkillButtonVisualState(button, skill);
'@ `
    -New @'
                string label = BuildSkillButtonLabel(skill);
                SetButtonLabel(button, label);
                ApplySkillCategorySprites(button, skill);
                ApplySkillButtonVisualState(button, skill);
'@ `
    -Label "ApplySkillCategorySprites call"

$commandText = Replace-Optional `
    -Source $commandText `
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
    -Label "skill unavailable visual"

$commandText = Replace-Optional `
    -Source $commandText `
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
    -Label "ResetButtonVisualState disabled clear"

$commandText = Insert-Before-Optional `
    -Source $commandText `
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

'@ `
    -AlreadyContains "private enum CommandPanelMode" `
    -Label "command panel sprite state helpers"

Set-Content -Path $commandPath -Value $commandText -Encoding UTF8
Write-Host "Applied robust skill category sprite state patch."
