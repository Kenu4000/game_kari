$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }

    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }

    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$field = @'
        [SerializeField] private float enemyStatusKoFadeDelaySeconds = 0.0f;

'@
$text = InsertBeforeIfMissing $text 'enemyStatusKoFadeDelaySeconds' '        [SerializeField] private float defeatFadeSeconds' $field 'enemy status KO fade delay field'

# Keep KO enemy status slots visible until KO fade removes the unit.
$text = ReplaceOptional $text '            int livingEnemyCount = CountLivingUnits(_enemies);' '            int livingEnemyCount = CountStatusVisibleEnemyUnits();' 'enemy status count includes KO-pending units'

$countHelper = @'
        private int CountStatusVisibleEnemyUnits()
        {
            int count = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

'@
$text = InsertBeforeIfMissing $text 'private int CountStatusVisibleEnemyUnits()' '        private void ResizeEnemyStatusPanel(int visibleEnemyCount)' $countHelper 'CountStatusVisibleEnemyUnits helper'

# Replace any direct status alpha interpolation inside KO fade with the delay-aware helper.
$text = [regex]::Replace(
    $text,
    'group\.alpha\s*=\s*Mathf\.Lerp\(statusStartAlphas\[i\],\s*0f,\s*eased\);',
    'ApplyEnemyStatusKoFadeAlpha(group, statusStartAlphas[i], elapsed, duration);'
)

# Replace previous partial v2 calculation too, if it exists.
$text = [regex]::Replace(
    $text,
    '(?s)float statusDelay = Mathf\.Max\(0f, enemyStatusKoFadeDelaySeconds\);\s*float statusT = duration <= 0f \? 1f : Mathf\.Clamp01\(\(elapsed - statusDelay\) / Mathf\.Max\(0\.0001f, duration - statusDelay\)\);\s*float statusEased = 1f - Mathf\.Pow\(1f - statusT, 2f\);\s*group\.alpha = Mathf\.Lerp\(statusStartAlphas\[i\], 0f, statusEased\);',
    'ApplyEnemyStatusKoFadeAlpha(group, statusStartAlphas[i], elapsed, duration);'
)

$helper = @'
        private void ApplyEnemyStatusKoFadeAlpha(CanvasGroup group, float startAlpha, float elapsed, float duration)
        {
            if (group == null)
            {
                return;
            }

            float delay = Mathf.Max(0f, enemyStatusKoFadeDelaySeconds);
            if (elapsed < delay)
            {
                group.alpha = startAlpha;
                return;
            }

            float remaining = Mathf.Max(0.0001f, duration - delay);
            float t = duration <= 0f ? 1f : Mathf.Clamp01((elapsed - delay) / remaining);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            group.alpha = Mathf.Lerp(startAlpha, 0f, eased);
        }

'@
$text = InsertBeforeIfMissing $text 'private void ApplyEnemyStatusKoFadeAlpha(CanvasGroup group, float startAlpha, float elapsed, float duration)' '        private CanvasGroup GetOrAddEnemyStatusCanvasGroup(BattleUnit unit)' $helper 'ApplyEnemyStatusKoFadeAlpha helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy status KO fade delay with explicit helper.'
