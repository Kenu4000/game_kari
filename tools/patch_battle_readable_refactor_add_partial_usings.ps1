$ErrorActionPreference = 'Stop'

# Adds common using directives to BattleUIManager partial files.
# Phase 2 moved methods out of BattleUIManager.cs, so those files now need
# the same namespaces that were previously available in the original file.

$files = @(
    'Assets/Scripts/Battle/BattleUIManager.Actions.cs',
    'Assets/Scripts/Battle/BattleUIManager.Animation.cs',
    'Assets/Scripts/Battle/BattleUIManager.KO.cs',
    'Assets/Scripts/Battle/BattleUIManager.Preview.cs',
    'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs',
    'Assets/Scripts/Battle/BattleUIManager.Turns.cs'
)

$usings = @'
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

'@

function Write-Utf8NoBom([string]$path, [string]$content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

foreach ($file in $files) {
    if (!(Test-Path $file)) {
        Write-Host "Skip missing file: $file"
        continue
    }

    $text = Get-Content -Path $file -Raw -Encoding UTF8

    if ($text -match '^using\s+System\.Collections;') {
        Write-Host "Already has usings: $file"
        continue
    }

    if ($text -notmatch '^namespace\s+GameKari\.Battle') {
        throw "Unexpected file format, namespace not at top: $file"
    }

    Write-Utf8NoBom $file ($usings + $text)
    Write-Host "Added usings: $file"
}

Write-Host 'Done. Reopen Unity compile after this patch.'
