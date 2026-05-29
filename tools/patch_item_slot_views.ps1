$ErrorActionPreference = 'Stop'

$itemPath = 'Assets/Scripts/Battle/ItemData.cs'
$commandPath = 'Assets/Scripts/Battle/CommandPanelController.cs'

foreach ($path in @($itemPath, $commandPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

function ReplaceMethod($src, $signature, $replacement, $label) {
    $start = $src.IndexOf($signature)
    if ($start -lt 0) {
        if ($src.Contains($replacement.Trim())) {
            Write-Host "Already patched: $label"
            return $src
        }
        throw "Patch anchor not found: $label"
    }

    $open = $src.IndexOf('{', $start)
    if ($open -lt 0) { throw "Method body start not found: $label" }

    $depth = 0
    $end = -1
    for ($i = $open; $i -lt $src.Length; $i++) {
        if ($src[$i] -eq '{') { $depth++ }
        elseif ($src[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { $end = $i + 1; break }
        }
    }

    if ($end -lt 0) { throw "Method body end not found: $label" }
    while ($end -lt $src.Length -and ($src[$end] -eq [char]13 -or $src[$end] -eq [char]10)) { $end++ }
    return $src.Substring(0, $start) + $replacement + $src.Substring($end)
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

# ItemData: add icon field for ItemSlotView.
$itemText = Get-Content -Path $itemPath -Raw -Encoding UTF8
$itemText = ReplaceOptional $itemText @'
        [TextArea] public string Description;
        public ItemKind Kind;
'@ @'
        [TextArea] public string Description;
        public Sprite Icon;
        public ItemKind Kind;
'@ 'ItemData Icon field'
Set-Content -Path $itemPath -Value $itemText -Encoding UTF8

# CommandPanelController: add item slot views and prefer them when configured.
$text = Get-Content -Path $commandPath -Raw -Encoding UTF8

$text = ReplaceOptional $text @'
        [Header("Fixed Item Buttons")]
        [SerializeField] private Button[] itemButtons = new Button[2];
'@ @'
        [Header("Fixed Item Buttons")]
        [SerializeField] private Button[] itemButtons = new Button[2];

        [Header("Item Slot Views")]
        [SerializeField] private ItemSlotView[] itemSlotViews = new ItemSlotView[4];
'@ 'itemSlotViews field'

$text = ReplaceOptional $text @'
            BindSkillButtons();
            BindSwapButtons();
            BindFixedItemButtons();
'@ @'
            BindSkillButtons();
            BindSwapButtons();
            BindItemUi();
'@ 'Setup item binding'

$bindItemUi = @'
        private void BindItemUi()
        {
            if (HasItemSlotViews())
            {
                BindItemSlotViews();
                return;
            }

            BindFixedItemButtons();
        }

        private bool HasItemSlotViews()
        {
            if (itemSlotViews == null)
            {
                return false;
            }

            for (int i = 0; i < itemSlotViews.Length; i++)
            {
                if (itemSlotViews[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void BindItemSlotViews()
        {
            if (itemSlotViews == null)
            {
                return;
            }

            for (int i = 0; i < itemSlotViews.Length; i++)
            {
                ItemSlotView slotView = itemSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                slotView.SetItem(GetItemAt(i), OnItemClicked, HandleItemHovered, ClearDescription);
            }
        }

        private void HandleItemHovered(InventoryItem inventoryItem)
        {
            if (descriptionText != null)
            {
                descriptionText.text = BuildItemDescription(inventoryItem);
            }
        }

'@

$text = InsertBeforeIfMissing $text 'private void BindItemUi()' '        private void BindFixedItemButtons()' $bindItemUi 'BindItemUi helpers'

Set-Content -Path $commandPath -Value $text -Encoding UTF8
Write-Host 'Patched item slot view support.'
