$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function AddBefore($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Anchor not found: $label" }
    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceIfFound($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Not found or already changed: $label"
        return $src
    }
    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$field = @'
        [SerializeField] private float enemyStatusKoFadeDelaySeconds = 0.0f;

'@
$text = AddBefore $text 'enemyStatusKoFadeDelaySeconds' '        [SerializeField] private float defeatFadeSeconds' $field 'enemy status fade delay field'

$text = ReplaceIfFound $text '            int livingEnemyCount = CountLivingUnits(_enemies);' '            int livingEnemyCount = CountStatusVisibleEnemyUnits();' 'enemy status visible count'

$helper = @'
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
$text = AddBefore $text 'private int CountStatusVisibleEnemyUnits()' '        private void ResizeEnemyStatusPanel(int visibleEnemyCount)' $helper 'status visible count helper'

$oldFade = @'
                    group.alpha = Mathf.Lerp(statusStartAlphas[i], 0f, eased);
'@
$newFade = @'
                    float statusDelay = Mathf.Max(0f, enemyStatusKoFadeDelaySeconds);
                    float statusT = duration <= 0f ? 1f : Mathf.Clamp01((elapsed - statusDelay) / Mathf.Max(0.0001f, duration - statusDelay));
                    float statusEased = 1f - Mathf.Pow(1f - statusT, 2f);
                    group.alpha = Mathf.Lerp(statusStartAlphas[i], 0f, statusEased);
'@
$text = ReplaceIfFound $text $oldFade $newFade 'enemy status fade delay calculation'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy status fade delay.'
