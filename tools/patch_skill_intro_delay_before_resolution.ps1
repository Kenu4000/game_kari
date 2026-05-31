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

# Add inspector field.
$text = InsertBeforeIfMissing $text 'private float actionIntroDelaySeconds' '        [SerializeField] private float actionResolveDelaySeconds = 0.35f;' @'
        [SerializeField] private float actionIntroDelaySeconds = 0.5f;
'@ 'action intro delay field'

$oldHandle = @'
        private void HandleSkillClicked(SkillData skill)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (!CanUseSkill(_active, skill))
            {
                return;
            }

            BattleUnit linkPartner = GetLinkPartnerForSkill(_active, skill);

            EnterResolvingAction();
            ConsumeSkillMP(_active, skill, linkPartner);

            ShowActionOverlay(skill.SkillName, BuildSkillUserDisplayName(_active, linkPartner));
            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(_active, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {BuildSkillUserDisplayName(_active, linkPartner)}.");

            ApplySkillDamage(skill);
            ApplySkillEffect(skill);

            if (_battleEnded)
            {
                RedrawBoard();
                return;
            }

            StartCoroutine(FinishPlayerActionAfterDelay());
        }
'@

$newHandle = @'
        private void HandleSkillClicked(SkillData skill)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (!CanUseSkill(_active, skill))
            {
                return;
            }

            BattleUnit actor = _active;
            BattleUnit linkPartner = GetLinkPartnerForSkill(actor, skill);
            string userDisplayName = BuildSkillUserDisplayName(actor, linkPartner);

            EnterResolvingAction();
            ShowActionOverlay(skill.SkillName, userDisplayName);

            StartCoroutine(ResolvePlayerSkillAfterIntroDelay(skill, actor, linkPartner, userDisplayName));
        }

        private IEnumerator ResolvePlayerSkillAfterIntroDelay(SkillData skill, BattleUnit actor, BattleUnit linkPartner, string userDisplayName)
        {
            float delay = Mathf.Max(0f, actionIntroDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (_battleEnded || actor == null || actor.IsDead || skill == null)
            {
                yield break;
            }

            _active = actor;
            ConsumeSkillMP(actor, skill, linkPartner);

            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(actor, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            ApplySkillDamage(skill);
            ApplySkillEffect(skill);

            if (_battleEnded)
            {
                RedrawBoard();
                yield break;
            }

            StartCoroutine(FinishPlayerActionAfterDelay());
        }
'@

if ($text.Contains($oldHandle)) {
    $text = $text.Replace($oldHandle, $newHandle)
}
elseif ($text.Contains('private IEnumerator ResolvePlayerSkillAfterIntroDelay')) {
    Write-Host 'Skill intro delay resolver already exists.'
}
else {
    throw 'Patch anchor not found: HandleSkillClicked block'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched skill intro delay before action resolution.'
