$ErrorActionPreference = 'Stop'

# This patch is intentionally conservative.
# Goal:
# - Create/switch to a refactor branch locally.
# - Make BattleUIManager partial so we can split it safely later.
# - Add readable scaffold partial files with heavy comments.
# - Add a reading guide doc.
# - Do NOT change battle logic in this first pass.

$branchName = 'refactor/battle-readable'
$mainFile = 'Assets/Scripts/Battle/BattleUIManager.cs'
$battleDir = 'Assets/Scripts/Battle'
$docDir = 'docs/design'
$docPath = Join-Path $docDir 'battle_readable_refactor_guide.md'

function Ensure-Directory([string]$path) {
    if (!(Test-Path $path)) {
        New-Item -ItemType Directory -Path $path | Out-Null
    }
}

function Write-Utf8NoBom([string]$path, [string]$content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

if (!(Test-Path $mainFile)) {
    throw "Required file not found: $mainFile"
}

# Create or switch to a local refactor branch.
# This keeps main as a safe return point.
$currentBranch = (git rev-parse --abbrev-ref HEAD).Trim()
$branchExists = $false
try {
    git rev-parse --verify $branchName *> $null
    if ($LASTEXITCODE -eq 0) { $branchExists = $true }
} catch {
    $branchExists = $false
}

if ($currentBranch -ne $branchName) {
    if ($branchExists) {
        git switch $branchName
    } else {
        git switch -c $branchName
    }
}

$text = Get-Content -Path $mainFile -Raw -Encoding UTF8

# Make the manager partial. This alone should not change runtime behavior.
$text = $text -replace 'public class BattleUIManager : MonoBehaviour', 'public partial class BattleUIManager : MonoBehaviour'

# Normalize a few known compressed lines. These are readability-only edits.
$text = $text -replace 'private Transform\[\] turnOrderSlotPositions = new Transform\[8\];\s*\[SerializeField\] private TurnOrderSlotView turnOrderSlotTemplate;', "private Transform[] turnOrderSlotPositions = new Transform[8];`r`n        [SerializeField] private TurnOrderSlotView turnOrderSlotTemplate;"
$text = $text -replace 'private float actionIntroDelaySeconds = 0\.5f;\s*\[SerializeField\] private float actionResolveDelaySeconds = 0\.35f;', "private float actionIntroDelaySeconds = 0.5f;`r`n        [SerializeField] private float actionResolveDelaySeconds = 0.35f;"
$text = $text -replace 'public int MaxHP;\s*public string Text;', "public int MaxHP;`r`n            public string Text;"

# Add major reading comments only once.
if ($text -notmatch 'READING GUIDE: Inspector references') {
    $text = $text -replace '(\s*// Serialized references\s*)', @'

        // ============================================================
        // READING GUIDE: Inspector references
        // ------------------------------------------------------------
        // This area contains objects assigned from the Unity Inspector.
        // These fields are mostly "where is the UI object?" references.
        // They should not contain battle rules.
        //
        // Safe to read as:
        //   "Which UI parts does BattleUIManager know about?"
        // Not safe to read as:
        //   "How does battle logic work?"
        // ============================================================
        $1
'@
}

if ($text -notmatch 'READING GUIDE: Runtime battle state') {
    $text = $text -replace '(\s*// Runtime state\s*)', @'

        // ============================================================
        // READING GUIDE: Runtime battle state
        // ------------------------------------------------------------
        // These fields are the current battle data held while the battle
        // screen is open: units, reserves, active actor, turn order, flags.
        //
        // Important rule for future refactor:
        //   - View code may read these fields.
        //   - Rule code may change these fields.
        //   - Animation code should avoid changing these fields directly.
        //
        // If a bug changes HP, turn order, KO state, or current actor,
        // start reading from this area and the methods that modify it.
        // ============================================================
        $1
'@
}

if ($text -notmatch 'READING GUIDE: Constants and small helper types') {
    $text = $text -replace '(\s*// Constants and phase types\s*)', @'

        // ============================================================
        // READING GUIDE: Constants and small helper types
        // ------------------------------------------------------------
        // Constants define visual sizes, colors, timings, and fixed limits.
        // Nested helper classes below are local data containers used only
        // by BattleUIManager.
        //
        // Do not put complicated rules here. Put complicated rules into
        // named methods so the flow can be followed from top to bottom.
        // ============================================================
        $1
'@
}

if ($text -notmatch 'READING GUIDE: Reference binding') {
    $text = $text -replace '(\s*private void ApplyBattleUIReferences\(\)\s*\{)', @'

        // ============================================================
        // READING GUIDE: Reference binding
        // ------------------------------------------------------------
        // ApplyBattleUIReferences connects the serialized fields above to
        // BattleUIReferences in the scene.
        //
        // This method should only decide "which UI object to use".
        // It should not apply battle rules, damage, KO, turn order, or
        // animation timing.
        // ============================================================
$1
'@
}

Write-Utf8NoBom $mainFile $text

# Create readable partial scaffolds. They contain comments only for now.
# This lets us move methods in later patches without changing class access.
$partialFiles = @{
    'BattleUIManager.Actions.cs' = @'
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // ACTIONS AREA
        // ------------------------------------------------------------
        // Future destination for methods that resolve player/enemy actions.
        //
        // Examples to move here later:
        //   - skill button clicked
        //   - player skill resolution
        //   - enemy action resolution
        //   - item/pass resolution
        //
        // Rule for this file:
        //   This file may decide what happens when an action is chosen,
        //   but it should call smaller helpers for view updates, damage,
        //   KO, and animation.
        // ============================================================
    }
}
'@
    'BattleUIManager.Animation.cs' = @'
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // ANIMATION AREA
        // ------------------------------------------------------------
        // Future destination for battle animation bridge methods.
        //
        // SkillAnimationPlayer itself is already separated under:
        //   Assets/Scripts/Battle/Animation/
        //
        // This partial should only connect BattleUIManager state to the
        // animation player. It should not calculate damage or KO.
        // ============================================================
    }
}
'@
    'BattleUIManager.KO.cs' = @'
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // KO / REPLACEMENT AREA
        // ------------------------------------------------------------
        // Future destination for defeated-unit handling.
        //
        // This is the most fragile area. Move code here slowly.
        // Known sensitive rules:
        //   - Ally KO can trigger reserve replacement.
        //   - Enemy KO can trigger backline compacting.
        //   - StatusPanel timing must not make HP bars look restored.
        //
        // First pass should add comments before changing behavior.
        // ============================================================
    }
}
'@
    'BattleUIManager.Preview.cs' = @'
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // PREVIEW AREA
        // ------------------------------------------------------------
        // Future destination for hover/target preview methods.
        //
        // Examples to move here later:
        //   - skill target highlight
        //   - hover silhouette
        //   - overlap alpha while previewing
        //   - enemy action preview visuals
        //
        // Rule for this file:
        //   Preview code should change only visuals.
        //   It should not change HP, MP, KO, turn order, or reserves.
        // ============================================================
    }
}
'@
    'BattleUIManager.StatusPanels.cs' = @'
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // STATUS PANEL AREA
        // ------------------------------------------------------------
        // Future destination for ally/enemy status panel drawing.
        //
        // Important distinction:
        //   - Battle data owns actual HP.
        //   - Status panels only display that HP.
        //
        // If HP display looks wrong but internal HP is correct, check here.
        // If internal HP is wrong, check Actions/Damage/KO instead.
        // ============================================================
    }
}
'@
    'BattleUIManager.Turns.cs' = @'
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // TURNS AREA
        // ------------------------------------------------------------
        // Future destination for turn-order and phase methods.
        //
        // Examples to move here later:
        //   - active unit selection
        //   - command select phase
        //   - resolving action phase
        //   - battle ended phase
        //
        // Rule for this file:
        //   Turn code should decide "whose turn is next" and "what phase
        //   are we in". It should not draw every UI detail directly.
        // ============================================================
    }
}
'@
}

foreach ($name in $partialFiles.Keys) {
    $path = Join-Path $battleDir $name
    if (!(Test-Path $path)) {
        Write-Utf8NoBom $path $partialFiles[$name]
    }
}

Ensure-Directory $docDir
$doc = @'
# Battle readable refactor guide

This document is a reading guide for the battle code refactor.
The goal is not to make an advanced architecture. The goal is to make the code understandable and debuggable.

## Refactor principle

Keep `main` as the working version.
Do large edits on `refactor/battle-readable`.
If the branch becomes too broken, switch back to `main`.

```powershell
git switch main
```

## First-pass rule

The first pass must not change battle behavior.
It may:

- make `BattleUIManager` partial
- add readable comments
- add partial scaffold files
- normalize obviously compressed lines

It must not:

- change KO rules
- change enemy replacement rules
- change turn order rules
- change damage timing
- change status panel timing

## File map

### BattleUIManager.cs

Still contains most existing code.
This is the old large manager and should be reduced gradually.

### BattleUIManager.Actions.cs

Future home for player/enemy action resolution.

### BattleUIManager.Animation.cs

Future home for methods that call `SkillAnimationPlayer`.
The animation player itself stays in `Assets/Scripts/Battle/Animation/`.

### BattleUIManager.KO.cs

Future home for KO, reserve replacement, and enemy compacting.
This is fragile and should be moved late.

### BattleUIManager.Preview.cs

Future home for hover, target preview, silhouette preview, and enemy preview visuals.
Preview code should not change battle data.

### BattleUIManager.StatusPanels.cs

Future home for status panel drawing and HP bar display.

### BattleUIManager.Turns.cs

Future home for turn selection and phase transitions.

## Debug checklist after each refactor commit

1. Battle starts.
2. Skill list opens.
3. Hover preview appears.
4. Rotate still works.
5. Skill animation plays.
6. Damage number appears.
7. HP bar changes.
8. KO does not break status panels.
9. Enemy turn proceeds.
10. Wave clear still works.

## Comment style

Comments should explain why something exists, not only what the line does.

Good:

```csharp
// KO fade must finish before status panel compaction.
// Otherwise the reused HP bar can look like a defeated enemy healed.
```

Bad:

```csharp
// Set alpha to 0.
```
'@
Write-Utf8NoBom $docPath $doc

Write-Host 'Created readable battle refactor phase 1.'
Write-Host "Current branch should be: $branchName"
Write-Host 'Next: open Unity and verify that behavior did not change.'
