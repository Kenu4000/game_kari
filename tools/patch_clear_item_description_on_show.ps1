$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/CommandPanelController.cs'
if (!(Test-Path $path)) { throw 'CommandPanelController.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

$text = ReplaceOptional $text @'
        private void Start()
        {
            ClearDescription();
            ShowSkills();
        }
'@ @'
        private void Start()
        {
            ClearDescription();
            ClearItemDescription();
            ShowSkills();
        }
'@ 'Start clears item description'

$text = ReplaceOptional $text @'
        public void ShowItems()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, false, true);
            UpdateRootButtonSelection(CommandPanelMode.Items);
        }
'@ @'
        public void ShowItems()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            ClearItemDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, false, true);
            UpdateRootButtonSelection(CommandPanelMode.Items);
        }
'@ 'ShowItems clears item description'

$text = ReplaceOptional $text @'
            _inventoryItems = inventoryItems ?? new List<InventoryItem>();

            BindSkillButtons();
'@ @'
            _inventoryItems = inventoryItems ?? new List<InventoryItem>();
            ClearItemDescription();

            BindSkillButtons();
'@ 'Setup clears item description'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched CommandPanelController to clear item description text on start/setup/show.'
