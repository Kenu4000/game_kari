# Battle UI Generator Warning Resolved

## Date

2026-05-12

## Summary

`Assets/Editor/BattleUICreator.cs` was updated on `main` to replace the obsolete Unity API call.

## Result

- `Object.FindObjectOfType<Canvas>()` was replaced with `Object.FindFirstObjectByType<Canvas>()`.
- Unity was rechecked after pulling/pushing the change.
- Console status after running `Tools > Create Battle UI`:
  - no red errors
  - no warnings
- PR #7 and PR #8 were closed and not merged.

## Current status

The Battle UI generator is locally validated with no Console errors or warnings.
