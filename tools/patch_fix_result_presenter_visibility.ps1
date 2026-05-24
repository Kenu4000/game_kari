$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
            if (_resultPanelObject != null)
            {
                _resultPanelObject.SetActive(true);
            }
'@

$new = @'
            _resultPanelPresenter.SetVisible(true);
'@

if (!$text.Contains($old)) {
    if ($text.Contains($new.Trim())) {
        Write-Host "Result presenter visibility fix is already applied."
        exit 0
    }

    throw "Patch anchor not found: old ResultPanel SetActive block"
}

$text = $text.Replace($old, $new)
Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched ResultPanel visibility to use ResultPanelPresenter."
