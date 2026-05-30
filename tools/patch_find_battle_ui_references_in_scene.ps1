$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
            if (refs == null)
            {
                refs = GetComponentInChildren<BattleUIReferences>(true);
            }

            if (refs == null)
            {
                return;
            }
'@

$new = @'
            if (refs == null)
            {
                refs = GetComponentInChildren<BattleUIReferences>(true);
            }

            if (refs == null)
            {
                BattleUIReferences[] sceneRefs = FindObjectsOfType<BattleUIReferences>(true);
                if (sceneRefs != null && sceneRefs.Length > 0)
                {
                    refs = sceneRefs[0];
                }
            }

            if (refs == null)
            {
                Debug.LogWarning("[BattleUI] BattleUIReferences was not found. Some Inspector-bound UI references may not update.");
                return;
            }
'@

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
}
elseif ($text.Contains('FindObjectsOfType<BattleUIReferences>(true)')) {
    Write-Host 'Scene fallback is already present.'
}
else {
    throw 'Patch anchor not found: ApplyBattleUIReferences refs lookup'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched BattleUIManager to find BattleUIReferences in the active scene.'
