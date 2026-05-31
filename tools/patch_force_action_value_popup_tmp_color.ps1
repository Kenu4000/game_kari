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
    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$oldColorBlock = @'
                label.text = popup.Text;
                label.color = popup.Text.StartsWith("+")
                    ? healPopupColor
                    : damagePopupColor;

                _activeActionValuePopupLabels.Add(label);
'@
$newColorBlock = @'
                label.text = popup.Text;
                ApplyActionValuePopupColor(label, popup.Text);

                _activeActionValuePopupLabels.Add(label);
'@
$text = ReplaceOptional $text $oldColorBlock $newColorBlock 'replace action value popup color assignment'

$helper = @'
        private void ApplyActionValuePopupColor(TMP_Text label, string text)
        {
            if (label == null)
            {
                return;
            }

            Color color = !string.IsNullOrEmpty(text) && text.StartsWith("+")
                ? healPopupColor
                : damagePopupColor;

            label.enableVertexGradient = false;
            label.color = color;
            label.faceColor = color;

            if (label.fontSharedMaterial != null)
            {
                Material material = new Material(label.fontSharedMaterial);
                material.name = "ActionValuePopup_TMP_Material_Instance";
                if (material.HasProperty("_FaceColor"))
                {
                    material.SetColor("_FaceColor", color);
                }

                label.fontMaterial = material;
            }

            label.SetAllDirty();
        }

'@
$text = InsertBeforeIfMissing $text 'private void ApplyActionValuePopupColor(TMP_Text label, string text)' '        private void HideActiveActionValuePopups()' $helper 'ApplyActionValuePopupColor helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched action value popup to force TMP face color and material face color.'
