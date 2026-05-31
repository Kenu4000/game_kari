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

# Do not begin HP bar deferral when the overlay is shown. Intro delay may happen after this.
$text = ReplaceOptional $text '            BeginDeferredHpBarFill();' '            // HP bar deferral starts immediately before the actual HP-changing resolution.' 'remove early HP deferral from EnterResolvingAction'

# Player skill: start deferral after intro delay, immediately before MP/action resolution.
$oldPlayer = @'
            _active = actor;
            ConsumeSkillMP(actor, skill, linkPartner);
'@
$newPlayer = @'
            _active = actor;
            BeginDeferredHpBarFill();
            ConsumeSkillMP(actor, skill, linkPartner);
'@
$text = ReplaceOptional $text $oldPlayer $newPlayer 'begin HP deferral before player skill resolution'

# Enemy action: start deferral after intro delay, immediately before enemy action resolution.
$oldEnemy = @'
            _actedUnits.Add(enemy);
            ExecuteEnemyAction(enemy, action);
'@
$newEnemy = @'
            BeginDeferredHpBarFill();
            _actedUnits.Add(enemy);
            ExecuteEnemyAction(enemy, action);
'@
$text = ReplaceOptional $text $oldEnemy $newEnemy 'begin HP deferral before enemy action resolution'

# Item heal: no intro coroutine currently, so begin just before HP-changing resolution.
$oldItemHeal = @'
            EnterResolvingAction();

            int beforeHp = target.CurrentHP;
'@
$newItemHeal = @'
            EnterResolvingAction();
            BeginDeferredHpBarFill();

            int beforeHp = target.CurrentHP;
'@
$text = ReplaceOptional $text $oldItemHeal $newItemHeal 'begin HP deferral before item heal resolution'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Moved HP bar deferral to actual action resolution timing.'
