$ErrorActionPreference = 'Stop'

# Battle readable refactor phase 5
# ASCII-safe script. Japanese replacement text is stored as Base64 UTF-8.
# This avoids Windows PowerShell 5.1 parsing problems with BOM-less Japanese ps1 files.

function Decode-Utf8Base64([string]$value) {
    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($value))
}

function Write-Utf8NoBom([string]$path, [string]$content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

$items = @(
    'Assets/Scripts/Battle/BattleUIManager.Actions.cs|// Player actions|Ly8g44OX44Os44Kk44Ok44O86KGM5YuV',
    'Assets/Scripts/Battle/BattleUIManager.Actions.cs|// Enemy actions|Ly8g5pW16KGM5YuV',
    'Assets/Scripts/Battle/BattleUIManager.Actions.cs|// Skill effect application|Ly8g44K544Kt44Or5Yq55p6c44Gu6YGp55So',
    'Assets/Scripts/Battle/BattleUIManager.Actions.cs|// Action value popups|Ly8g6KGM5YuV57WQ5p6c44Od44OD44OX44Ki44OD44OX',
    'Assets/Scripts/Battle/BattleUIManager.Animation.cs|// Skill animation bridge|Ly8g44K544Kt44Or44Ki44OL44Oh44O844K344On44Oz5qmL5rih44GX',
    'Assets/Scripts/Battle/BattleUIManager.Animation.cs|// Legacy/simple animation helpers|Ly8g5pen5byP44O757Ch5piT44Ki44OL44Oh44O844K344On44Oz6KOc5Yqp',
    'Assets/Scripts/Battle/BattleUIManager.Animation.cs|// Auto replacement animation|Ly8g6Ieq5YuV6KOc5YWF44Ki44OL44Oh44O844K344On44Oz',
    'Assets/Scripts/Battle/BattleUIManager.KO.cs|// Ally defeat|Ly8g5ZGz5pa55pKD56C05Yem55CG',
    'Assets/Scripts/Battle/BattleUIManager.KO.cs|// Enemy defeat and replacement|Ly8g5pW15pKD56C044Go6KOc5YWF5Yem55CG',
    'Assets/Scripts/Battle/BattleUIManager.KO.cs|// Shared defeat helpers|Ly8g5pKD56C05Yem55CG44Gu5YWx6YCa6KOc5Yqp',
    'Assets/Scripts/Battle/BattleUIManager.Preview.cs|// Target preview|Ly8g5a++6LGh44OX44Os44OT44Ol44O8',
    'Assets/Scripts/Battle/BattleUIManager.Preview.cs|// Skill hover sprite preview|Ly8g44K544Kt44OraG92ZXLmmYLjga7jgrnjg5fjg6njgqTjg4jjg5fjg6zjg5Pjg6Xjg7w=',
    'Assets/Scripts/Battle/BattleUIManager.Preview.cs|// Enemy action preview|Ly8g5pW16KGM5YuV44OX44Os44OT44Ol44O8',
    'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs|// Status panel redraw|Ly8g44K544OG44O844K/44K55qyE44Gu5YaN5o+P55S7',
    'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs|// HP bar animation|Ly8gSFDjg5Djg7zjgqLjg4vjg6Hjg7zjgrfjg6fjg7M=',
    'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs|// Floating HP bar|Ly8g5rWu5YuVSFDjg5Djg7w=',
    'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs|// Fade helpers|Ly8g44OV44Kn44O844OJ6KOc5Yqp',
    'Assets/Scripts/Battle/BattleUIManager.Turns.cs|// Phase transitions|Ly8g44OV44Kn44O844K66YG356e7',
    'Assets/Scripts/Battle/BattleUIManager.Turns.cs|// Turn order|Ly8g6KGM5YuV6aCG',
    'Assets/Scripts/Battle/BattleUIManager.Turns.cs|// Battle end|Ly8g5oim6ZeY57WC5LqG'
)

$grouped = @{}
foreach ($line in $items) {
    $parts = $line.Split('|')
    $path = $parts[0]
    $old = $parts[1]
    $new = Decode-Utf8Base64 $parts[2]

    if (!$grouped.ContainsKey($path)) {
        $grouped[$path] = @()
    }
    $grouped[$path] += ,@($old, $new)
}

foreach ($path in $grouped.Keys) {
    if (!(Test-Path $path)) {
        Write-Host "Skip missing file: $path"
        continue
    }

    $text = Get-Content -Path $path -Raw -Encoding UTF8
    $changed = $false

    foreach ($pair in $grouped[$path]) {
        $old = $pair[0]
        $new = $pair[1]
        if ($text.Contains($old)) {
            $text = $text.Replace($old, $new)
            $changed = $true
            Write-Host "Replaced header: $old"
        }
    }

    if ($changed) {
        Write-Utf8NoBom $path $text
    } else {
        Write-Host "No matching headers in: $path"
    }
}

Write-Host 'Phase 5 Japanese header translation completed.'
