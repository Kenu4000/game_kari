$ErrorActionPreference = "Stop"

$skillPath = "Assets/Scripts/Battle/SkillData.cs"
$battlePath = "Assets/Scripts/Battle/BattleUIManager.cs"
$commandPath = "Assets/Scripts/Battle/CommandPanelController.cs"

foreach ($path in @($skillPath, $battlePath, $commandPath)) {
    if (!(Test-Path $path)) {
        throw "Required file not found: $path"
    }
}

function Replace-Optional {
    param([string]$Source, [string]$Old, [string]$New, [string]$Label)
    if (!$Source.Contains($Old)) {
        Write-Host "Already replaced or not found: $Label"
        return $Source
    }
    return $Source.Replace($Old, $New)
}

function Insert-Before-IfMissing {
    param([string]$Source, [string]$Needle, [string]$Anchor, [string]$Insertion, [string]$Label)
    if ($Source.Contains($Needle)) {
        Write-Host "Already exists: $Label"
        return $Source
    }

    $index = $Source.IndexOf($Anchor)
    if ($index -lt 0) {
        throw "Patch anchor not found: $Label"
    }

    return $Source.Substring(0, $index) + $Insertion + $Source.Substring($index)
}

function Replace-MethodByName {
    param([string]$Source, [string]$Signature, [string]$Replacement, [string]$Label)
    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        if ($Source.Contains($Replacement.Trim())) {
            Write-Host "Already patched: $Label"
            return $Source
        }
        throw "Patch anchor not found: $Label"
    }

    $braceStart = $Source.IndexOf("{", $start)
    if ($braceStart -lt 0) { throw "Method body start not found: $Label" }

    $depth = 0
    $end = -1
    for ($i = $braceStart; $i -lt $Source.Length; $i++) {
        $char = $Source[$i]
        if ($char -eq '{') { $depth++ }
        elseif ($char -eq '}') {
            $depth--
            if ($depth -eq 0) {
                $end = $i + 1
                break
            }
        }
    }

    if ($end -lt 0) { throw "Method body end not found: $Label" }
    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

# SkillData: add Heal effect type and HealAmount field.
$skillText = Get-Content -Path $skillPath -Raw -Encoding UTF8
$skillText = Replace-Optional `
    -Source $skillText `
    -Old @'
    public enum SkillEffectType
    {
        None,
        ApplyBuff
    }
'@ `
    -New @'
    public enum SkillEffectType
    {
        None,
        ApplyBuff,
        Heal
    }
'@ `
    -Label "SkillEffectType Heal"

$skillText = Insert-Before-IfMissing `
    -Source $skillText `
    -Needle "public int HealAmount;" `
    -Anchor "        public SkillEffectType EffectType;" `
    -Insertion @'
        public int HealAmount;
'@ `
    -Label "SkillData HealAmount"

Set-Content -Path $skillPath -Value $skillText -Encoding UTF8

# BattleUIManager: implement heal effect targets and heal application.
$battleText = Get-Content -Path $battlePath -Raw -Encoding UTF8

$battleText = Replace-MethodByName `
    -Source $battleText `
    -Signature "        private List<BattleUnit> GetSkillEffectTargets(SkillData skill)" `
    -Replacement @'
        private List<BattleUnit> GetSkillEffectTargets(SkillData skill)
        {
            var targets = new List<BattleUnit>();

            if (skill == null || _active == null || _active.IsDead)
            {
                return targets;
            }

            switch (skill.EffectTarget)
            {
                case SkillEffectTargetType.Self:
                    targets.Add(_active);
                    break;

                case SkillEffectTargetType.Target:
                    BattleUnit forwardAlly = TryGetForwardAlly(_active);
                    if (forwardAlly != null)
                    {
                        targets.Add(forwardAlly);
                    }
                    break;

                case SkillEffectTargetType.AllAllies:
                    AddLivingUnits(_allies, targets);
                    break;

                case SkillEffectTargetType.AllEnemies:
                    AddLivingUnits(_enemies, targets);
                    break;
            }

            return targets;
        }
'@ `
    -Label "GetSkillEffectTargets"

$battleText = Replace-MethodByName `
    -Source $battleText `
    -Signature "        private void ApplySkillEffect(SkillData skill)" `
    -Replacement @'
        private void ApplySkillEffect(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            switch (skill.EffectType)
            {
                case SkillEffectType.None:
                    return;

                case SkillEffectType.ApplyBuff:
                    List<BattleUnit> buffTargets = GetSkillEffectTargets(skill);
                    for (int i = 0; i < buffTargets.Count; i++)
                    {
                        ApplyBuff(buffTargets[i], skill.BuffType, skill.BuffTurns);
                    }
                    return;

                case SkillEffectType.Heal:
                    ApplySkillHeal(skill);
                    return;
            }
        }
'@ `
    -Label "ApplySkillEffect"

$battleText = Insert-Before-IfMissing `
    -Source $battleText `
    -Needle "private void ApplySkillHeal(SkillData skill)" `
    -Anchor "        private void DamageEnemyAt(GridPos pos, int damage, List<DefeatedEnemyInfo> defeatedEnemies)" `
    -Insertion @'
        private void ApplySkillHeal(SkillData skill)
        {
            if (skill == null || skill.HealAmount <= 0)
            {
                return;
            }

            List<BattleUnit> healTargets = GetSkillEffectTargets(skill);
            for (int i = 0; i < healTargets.Count; i++)
            {
                HealAllyUnit(healTargets[i], skill.HealAmount);
            }
        }

        private void HealAllyUnit(BattleUnit target, int healAmount)
        {
            if (target == null || target.IsDead || target.Data == null || healAmount <= 0)
            {
                return;
            }

            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Min(target.Data.MaxHP, target.CurrentHP + healAmount);
            int healed = target.CurrentHP - beforeHp;

            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}");
            Debug.Log($"[Heal] {target.Name} recovered {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}");
        }

        private static void AddLivingUnits(List<BattleUnit> source, List<BattleUnit> destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                BattleUnit unit = source[i];
                if (unit != null && !unit.IsDead && unit.Data != null && !destination.Contains(unit))
                {
                    destination.Add(unit);
                }
            }
        }

'@ `
    -Label "heal helpers"

Set-Content -Path $battlePath -Value $battleText -Encoding UTF8

# CommandPanelController: show Heal amount in description.
$commandText = Get-Content -Path $commandPath -Raw -Encoding UTF8
$commandText = Replace-Optional `
    -Source $commandText `
    -Old @'
                case SkillEffectType.ApplyBuff:
                    return $"Effect: {skill.BuffType} {skill.BuffTurns} turns";

                default:
                    return string.Empty;
'@ `
    -New @'
                case SkillEffectType.ApplyBuff:
                    return $"Effect: {skill.BuffType} {skill.BuffTurns} turns";

                case SkillEffectType.Heal:
                    return $"Heal: {Mathf.Max(0, skill.HealAmount)}";

                default:
                    return string.Empty;
'@ `
    -Label "CommandPanel Heal description"

Set-Content -Path $commandPath -Value $commandText -Encoding UTF8

Write-Host "Patched skill heal/buff effects for Inspector-configurable heal, change, and guard skills."
