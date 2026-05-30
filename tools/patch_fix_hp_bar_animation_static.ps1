$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = '        private static void SetBarFill(Transform root, string barName, int current, int max)'
$new = '        private void SetBarFill(Transform root, string barName, int current, int max)'

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
}
elseif ($text.Contains($new)) {
    Write-Host 'SetBarFill is already non-static.'
}
else {
    throw 'Patch anchor not found: SetBarFill signature'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Fixed HP bar animation static access by making SetBarFill non-static.'
