$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) { Write-Host "Already exists: $label"; return $src }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) { Write-Host "Already replaced or not found: $label"; return $src }
    return $src.Replace($old, $new)
}

$text = InsertBeforeIfMissing $text 'private float hpBarAnimationSeconds' '        [SerializeField] private float defeatSinkDistance = 18f;' @'
        [SerializeField] private float hpBarAnimationSeconds = 0.35f;

'@ 'hp bar animation field'

$text = ReplaceOptional $text @'
            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.localScale = new Vector3(rate, 1f, 1f);
'@ @'
            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetAnimationSeconds(hpBarAnimationSeconds);
            animator.SetFill(rate);
'@ 'SetBarFill uses HPBarFillAnimator'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched HP bar fill animation.'
