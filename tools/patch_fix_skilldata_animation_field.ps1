$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/SkillData.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

# Remove broken literal quote/backtick/newline artifacts around HealAmount/Animation/EffectType.
$pattern = '(?s)public\s+int\s+HealAmount;.*?public\s+SkillEffectTargetType\s+EffectTarget;'
$replacement = @'
public int HealAmount;
        public SkillAnimationData Animation;
        public SkillEffectType EffectType;
        public SkillEffectTargetType EffectTarget;
'@

$fixed = [regex]::Replace($text, $pattern, $replacement, 1)
if ($fixed -eq $text) {
    throw 'Patch anchor not found: SkillData HealAmount/Animation/EffectType block'
}

Set-Content -Path $path -Value $fixed -Encoding UTF8
Write-Host 'Fixed SkillData Animation field syntax.'
