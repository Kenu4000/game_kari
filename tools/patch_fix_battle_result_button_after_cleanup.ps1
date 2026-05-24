$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
            if (_resultReturnButton != null)
            {
                _resultReturnButton.onClick.RemoveAllListeners();
                _resultReturnButton.onClick.AddListener(HandleBattleResultNextClicked);
            }
'@

$new = @'
            SetResultReturnButtonHandler(HandleBattleResultNextClicked);
'@

if (!$text.Contains($old)) {
    if ($text.Contains($new.Trim())) {
        Write-Host "Battle Result button fix is already applied."
        exit 0
    }

    throw "Patch anchor not found: Battle Result _resultReturnButton block"
}

$text = $text.Replace($old, $new)
Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched Battle Result button handler to use ResultPanelPresenter."
