$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
        private void BindUI()
        {
            commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
            commandPanel.OnSkillClicked += HandleSkillClicked;
            commandPanel.OnSkillHovered += HandleSkillHover;
            commandPanel.OnHoverExit += ClearTargetPreview;
            commandPanel.OnReserveClicked += HandleSwap;
            commandPanel.OnItemClicked += HandleItemClicked;
            rotateButton.onClick.AddListener(HandleRotateClicked);
        }
'@

$new = @'
        private void BindUI()
        {
            if (commandPanel != null)
            {
                commandPanel.OnSkillClicked -= HandleSkillClicked;
                commandPanel.OnSkillHovered -= HandleSkillHover;
                commandPanel.OnHoverExit -= ClearTargetPreview;
                commandPanel.OnReserveClicked -= HandleSwap;
                commandPanel.OnItemClicked -= HandleItemClicked;

                commandPanel.OnSkillClicked += HandleSkillClicked;
                commandPanel.OnSkillHovered += HandleSkillHover;
                commandPanel.OnHoverExit += ClearTargetPreview;
                commandPanel.OnReserveClicked += HandleSwap;
                commandPanel.OnItemClicked += HandleItemClicked;

                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
            }

            if (rotateButton != null)
            {
                rotateButton.onClick.RemoveListener(HandleRotateClicked);
                rotateButton.onClick.AddListener(HandleRotateClicked);
            }
        }
'@

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
}
elseif ($text.Contains('commandPanel.OnReserveClicked -= HandleSwap;')) {
    Write-Host 'BindUI already binds events before setup.'
}
else {
    throw 'Patch anchor not found: BindUI'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched BindUI to subscribe CommandPanel events before Setup.'
