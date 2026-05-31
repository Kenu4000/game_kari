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
            SetSkillHoverSilhouetteOutlineVisible(image, true, resolvedAlpha);
        }

'@
$text = ReplaceRegexRequired $text '(?s)        private void ApplySkillHoverSilhouette\(Image image, float alpha\)\s*\{.*?\n        \}\s*\n' $apply 'ApplySkillHoverSilhouette'

$normal = @'
        private void ApplyNormalBoardSpriteMaterial(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.material = null;
            SetSkillHoverSilhouetteOutlineVisible(image, false, 1f);
        }

'@
$text = ReplaceRegexRequired $text '(?s)        private (?:static )?void ApplyNormalBoardSpriteMaterial\(Image image\)\s*\{.*?\n        \}\s*\n' $normal 'ApplyNormalBoardSpriteMaterial'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched silhouette to fixed gray and restored alpha propagation.'
