$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceLoop($src, $old, $new, $label) {
    $count = 0
    while ($src.Contains($old)) {
        $src = $src.Replace($old, $new)
        $count++
    }

    if ($count -gt 0) {
        Write-Host "Replaced $count occurrence(s): $label"
    }
    else {
        Write-Host "No replacement needed: $label"
    }

    return $src
}

# Remove duplicated local declarations caused by applying the KO status fade patch more than once.
$dupDeclarations = @'
            var statusCanvasGroups = new List<CanvasGroup>();
            var statusStartAlphas = new List<float>();
            var statusCanvasGroups = new List<CanvasGroup>();
            var statusStartAlphas = new List<float>();
'@
$singleDeclarations = @'
            var statusCanvasGroups = new List<CanvasGroup>();
            var statusStartAlphas = new List<float>();
'@
$text = ReplaceLoop $text $dupDeclarations $singleDeclarations 'duplicate status fade declarations'

# Remove duplicated capture block caused by applying the KO status fade patch more than once.
$dupCapture = @'
                CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup(unit);
                if (statusGroup != null && !statusCanvasGroups.Contains(statusGroup))
                {
                    statusCanvasGroups.Add(statusGroup);
                    statusStartAlphas.Add(statusGroup.alpha);
                }

                CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup(unit);
                if (statusGroup != null && !statusCanvasGroups.Contains(statusGroup))
                {
                    statusCanvasGroups.Add(statusGroup);
                    statusStartAlphas.Add(statusGroup.alpha);
                }
'@
$singleCapture = @'
                CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup(unit);
                if (statusGroup != null && !statusCanvasGroups.Contains(statusGroup))
                {
                    statusCanvasGroups.Add(statusGroup);
                    statusStartAlphas.Add(statusGroup.alpha);
                }
'@
$text = ReplaceLoop $text $dupCapture $singleCapture 'duplicate status fade capture block'

# Also handle the same duplicated declarations with CRLF/LF variations by a narrow regex.
$text = [regex]::Replace(
    $text,
    '(?s)(\s*var statusCanvasGroups = new List<CanvasGroup>\(\);\s*var statusStartAlphas = new List<float>\(\);)\s*var statusCanvasGroups = new List<CanvasGroup>\(\);\s*var statusStartAlphas = new List<float>\(\);',
    '$1'
)

$text = [regex]::Replace(
    $text,
    '(?s)(\s*CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup\(unit\);\s*if \(statusGroup != null && !statusCanvasGroups\.Contains\(statusGroup\)\)\s*\{\s*statusCanvasGroups\.Add\(statusGroup\);\s*statusStartAlphas\.Add\(statusGroup\.alpha\);\s*\})\s*CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup\(unit\);\s*if \(statusGroup != null && !statusCanvasGroups\.Contains\(statusGroup\)\)\s*\{\s*statusCanvasGroups\.Add\(statusGroup\);\s*statusStartAlphas\.Add\(statusGroup\.alpha\);\s*\}',
    '$1'
)

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Removed duplicate enemy status fade local variables if present.'
