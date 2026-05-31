$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = 'BattleUIReferences[] sceneRefs = FindObjectsOfType<BattleUIReferences>(true);'
$new = 'BattleUIReferences[] sceneRefs = FindObjectsByType<BattleUIReferences>(FindObjectsInactive.Include, FindObjectsSortMode.None);'

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
    Set-Content -Path $path -Value $text -Encoding UTF8
    Write-Host 'Replaced obsolete FindObjectsOfType with FindObjectsByType.'
    exit 0
}

if ($text.Contains($new)) {
    Write-Host 'FindObjectsByType is already used.'
    exit 0
}

throw 'Patch anchor not found: FindObjectsOfType<BattleUIReferences>(true)'
