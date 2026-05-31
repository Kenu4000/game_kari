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
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

# Ensure Inspector field exists even if the previous player-side patch has not been applied yet.
$text = InsertBeforeIfMissing $text 'private float actionIntroDelaySeconds' '        [SerializeField] private float actionResolveDelaySeconds = 0.35f;' @'
        [SerializeField] private float actionIntroDelaySeconds = 0.5f;
'@ 'action intro delay field'

$oldEnemyCoroutine = @'
        private IEnumerator ResolveEnemyActionAndAdvance(BattleUnit enemy)
        {
            if (enemy == null || enemy.IsDead || _battleEnded)
            {
                yield break;
            }

            EnterResolvingAction();

            EnemyActionState action = GetPreviewEnemyActionState(enemy);

            _actedUnits.Add(enemy);
            ExecuteEnemyAction(enemy, action);
            ClearPreviewEnemyActionState(enemy);
            RedrawBoard();

            yield return PlayPendingActionFlashOrDelay();

            if (_battleEnded)
            {
                yield break;
            }

            AdvanceToNextActor();
        }
'@

$newEnemyCoroutine = @'
        private IEnumerator ResolveEnemyActionAndAdvance(BattleUnit enemy)
        {
            if (enemy == null || enemy.IsDead || _battleEnded)
            {
                yield break;
            }

            EnterResolvingAction();

            EnemyActionState action = GetPreviewEnemyActionState(enemy);
            if (action == null || action.Skill == null)
            {
                ClearPreviewEnemyActionState(enemy);
                AdvanceToNextActor();
                yield break;
            }

            ShowActionOverlay(action.Skill.SkillName, enemy.Name);

            float delay = Mathf.Max(0f, actionIntroDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (enemy == null || enemy.IsDead || _battleEnded)
            {
                yield break;
            }

            _actedUnits.Add(enemy);
            ExecuteEnemyAction(enemy, action);
            ClearPreviewEnemyActionState(enemy);
            RedrawBoard();

            yield return PlayPendingActionFlashOrDelay();

            if (_battleEnded)
            {
                yield break;
            }

            AdvanceToNextActor();
        }
'@

if ($text.Contains($oldEnemyCoroutine)) {
    $text = $text.Replace($oldEnemyCoroutine, $newEnemyCoroutine)
}
elseif ($text.Contains('float delay = Mathf.Max(0f, actionIntroDelaySeconds);') -and $text.Contains('private IEnumerator ResolveEnemyActionAndAdvance(BattleUnit enemy)')) {
    Write-Host 'Enemy intro delay already appears to be applied.'
}
else {
    throw 'Patch anchor not found: ResolveEnemyActionAndAdvance block'
}

$oldExecuteOverlay = '            ShowActionOverlay(action.Skill.SkillName, enemy.Name);' + [Environment]::NewLine
if ($text.Contains($oldExecuteOverlay + '            PrepareEnemyActionFlashTargets(enemy, action);')) {
    $text = $text.Replace($oldExecuteOverlay + '            PrepareEnemyActionFlashTargets(enemy, action);', '            PrepareEnemyActionFlashTargets(enemy, action);')
    Write-Host 'Removed duplicate enemy ShowActionOverlay from ExecuteEnemyAction.'
}
else {
    Write-Host 'No duplicate enemy ShowActionOverlay found in ExecuteEnemyAction, or already removed.'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy action intro delay before resolution.'
