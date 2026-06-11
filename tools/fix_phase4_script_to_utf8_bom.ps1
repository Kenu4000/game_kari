$ErrorActionPreference = 'Stop'

$path = '.\tools\patch_battle_readable_refactor_phase4_japanese_comments.ps1'

if (!(Test-Path $path)) {
    throw "File not found: $path"
}

$fullPath = (Resolve-Path $path).Path
$bytes = [System.IO.File]::ReadAllBytes($fullPath)
$text = [System.Text.Encoding]::UTF8.GetString($bytes)
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText($fullPath, $text, $utf8Bom)

Write-Host 'Converted phase4 script to UTF-8 with BOM.'
Write-Host 'Now run: powershell -ExecutionPolicy Bypass -File .\tools\patch_battle_readable_refactor_phase4_japanese_comments.ps1'
