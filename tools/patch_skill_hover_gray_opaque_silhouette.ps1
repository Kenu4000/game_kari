$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

# Default silhouette color: gray and fully opaque.
$text = ReplaceOptional $text `
'        [SerializeField] private Color skillHoverInactiveSpriteColor = new Color(0.55f, 0.68f, 0.72f, 0.85f);' `
'        [SerializeField] private Color skillHoverInactiveSpriteColor = new Color(0.5f, 0.5f, 0.5f, 1f);' `
'set default skill hover silhouette to opaque gray'

# Non-focused silhouettes should always be fully opaque. Only overlap processing may lower alpha later.
$text = ReplaceOptional $text `
'                ApplySkillHoverSilhouette(image, skillHoverInactiveSpriteColor.a);' `
'                ApplySkillHoverSilhouette(image, 1f);' `
'make inactive silhouettes opaque by default'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched skill hover silhouettes to opaque gray by default.'
