$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceRegexRequired($src, $pattern, $replacement, $label) {
    $result = [regex]::Replace($src, $pattern, $replacement, 1)
    if ($result -eq $src) { throw "Patch anchor not found: $label" }
    Write-Host "Replaced: $label"
    return $result
}

$apply = @'
        private void ApplySkillHoverSilhouette(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Material material = GetSkillHoverSilhouetteMaterial();
            if (material != null)
            {
                image.material = material;
            }

            float resolvedAlpha = Mathf.Clamp01(alpha);
            image.color = new Color(0.5f, 0.5f, 0.5f, resolvedAlpha);

            bool showOutline = resolvedAlpha >= 0.99f;
            SetSkillHoverSilhouetteOutlineVisible(image, showOutline, resolvedAlpha);
        }

'@
$text = ReplaceRegexRequired $text '(?s)        private void ApplySkillHoverSilhouette\(Image image, float alpha\)\s*\{.*?\n        \}\s*\n' $apply 'ApplySkillHoverSilhouette hide outline for alpha'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched overlap alpha to hide outline layers when silhouette is transparent.'
