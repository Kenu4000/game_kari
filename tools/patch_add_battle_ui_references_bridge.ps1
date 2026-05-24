$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Required {
    param([string]$Source, [string]$Old, [string]$New, [string]$Label)
    if (!$Source.Contains($Old)) {
        Write-Host "Already replaced or not found: $Label"
        return $Source
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

# Add optional grouped UI reference field before legacy individual fields.
$text = Replace-Required `
    -Source $text `
    -Old @'
        // Serialized references
        [Header("Controllers")]
        [SerializeField] private CommandPanelController commandPanel;
'@ `
    -New @'
        // Serialized references
        [Header("Battle UI References")]
        [SerializeField] private BattleUIReferences uiReferences;

        [Header("Controllers")]
        [SerializeField] private CommandPanelController commandPanel;
'@ `
    -Label "uiReferences field"

# Apply references before bootstrap/bind.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void Start()
        {
            BootstrapBattle();
            BindUI();
'@ `
    -New @'
        private void Start()
        {
            ApplyBattleUIReferences();
            BootstrapBattle();
            BindUI();
'@ `
    -Label "Start apply references"

# Add helper methods before Battle setup section.
$text = Insert-Before `
    -Source $text `
    -Anchor "        // Battle setup" `
    -Insertion @'
        private void ApplyBattleUIReferences()
        {
            BattleUIReferences refs = uiReferences;
            if (refs == null)
            {
                refs = GetComponentInChildren<BattleUIReferences>(true);
            }

            if (refs == null)
            {
                return;
            }

            commandPanel = refs.commandPanel != null ? refs.commandPanel : commandPanel;
            rotateButton = refs.rotateButton != null ? refs.rotateButton : rotateButton;

            enemyFrontTop = refs.enemyFrontTop != null ? refs.enemyFrontTop : enemyFrontTop;
            enemyBackTop = refs.enemyBackTop != null ? refs.enemyBackTop : enemyBackTop;
            enemyFrontBottom = refs.enemyFrontBottom != null ? refs.enemyFrontBottom : enemyFrontBottom;
            enemyBackBottom = refs.enemyBackBottom != null ? refs.enemyBackBottom : enemyBackBottom;

            allyFrontTop = refs.allyFrontTop != null ? refs.allyFrontTop : allyFrontTop;
            allyBackTop = refs.allyBackTop != null ? refs.allyBackTop : allyBackTop;
            allyFrontBottom = refs.allyFrontBottom != null ? refs.allyFrontBottom : allyFrontBottom;
            allyBackBottom = refs.allyBackBottom != null ? refs.allyBackBottom : allyBackBottom;

            actionSkillName = refs.actionSkillName != null ? refs.actionSkillName : actionSkillName;
            actionUserName = refs.actionUserName != null ? refs.actionUserName : actionUserName;

            enemyFTHighlight = refs.enemyFTHighlight != null ? refs.enemyFTHighlight : enemyFTHighlight;
            enemyFBHighlight = refs.enemyFBHighlight != null ? refs.enemyFBHighlight : enemyFBHighlight;

            enemyStatusPanel = refs.enemyStatusPanel != null ? refs.enemyStatusPanel : enemyStatusPanel;
            allyStatusPanel = refs.allyStatusPanel != null ? refs.allyStatusPanel : allyStatusPanel;
        }

'@ `
    -Label "ApplyBattleUIReferences method"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to consume BattleUIReferences."
