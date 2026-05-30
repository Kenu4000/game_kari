$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/CommandPanelController.cs'
if (!(Test-Path $path)) { throw 'CommandPanelController.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
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

$text = ReplaceOptional $text @'
        [Header("Item Slot Views")]
        [SerializeField] private ItemSlotView[] itemSlotViews = new ItemSlotView[4];
'@ @'
        [Header("Item Slot Views")]
        [SerializeField] private ItemSlotView[] itemSlotViews = new ItemSlotView[4];

        [Header("Generated Item Slots")]
        [SerializeField] private Transform itemSlotContainer;
        [SerializeField] private ItemSlotView itemSlotTemplate;
        [SerializeField] private bool hideItemSlotTemplateOnPlay = true;
'@ 'generated item slot fields'

$text = InsertBeforeIfMissing $text 'private readonly List<ItemSlotView> _generatedItemSlotViews' '        private List<InventoryItem> _inventoryItems = new();' @'
        private readonly List<ItemSlotView> _generatedItemSlotViews = new();

'@ 'generated item slot list'

$text = ReplaceOptional $text @'
        private void BindItemUi()
        {
            if (HasItemSlotViews())
            {
                BindItemSlotViews();
                return;
            }

            BindFixedItemButtons();
        }
'@ @'
        private void BindItemUi()
        {
            if (CanGenerateItemSlots())
            {
                BindGeneratedItemSlotViews();
                return;
            }

            if (HasItemSlotViews())
            {
                BindItemSlotViews();
                return;
            }

            BindFixedItemButtons();
        }
'@ 'BindItemUi generation priority'

$helpers = @'
        private bool CanGenerateItemSlots()
        {
            return itemSlotContainer != null && itemSlotTemplate != null;
        }

        private void BindGeneratedItemSlotViews()
        {
            List<InventoryItem> visibleItems = GetVisibleInventoryItems();
            EnsureGeneratedItemSlotCapacity(visibleItems.Count);

            if (itemSlotTemplate != null && hideItemSlotTemplateOnPlay)
            {
                itemSlotTemplate.SetVisible(false);
            }

            for (int i = 0; i < _generatedItemSlotViews.Count; i++)
            {
                ItemSlotView slotView = _generatedItemSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                bool visible = i < visibleItems.Count;
                slotView.SetVisible(visible);
                if (visible)
                {
                    slotView.SetItem(visibleItems[i], OnItemClicked, HandleItemHovered, ClearDescription);
                }
            }
        }

        private void EnsureGeneratedItemSlotCapacity(int requiredCount)
        {
            if (itemSlotContainer == null || itemSlotTemplate == null)
            {
                return;
            }

            for (int i = _generatedItemSlotViews.Count; i < requiredCount; i++)
            {
                ItemSlotView slotView = Instantiate(itemSlotTemplate, itemSlotContainer);
                slotView.name = $"ItemSlot_{i + 1}";
                slotView.SetVisible(true);
                _generatedItemSlotViews.Add(slotView);
            }
        }

'@

$text = InsertBeforeIfMissing $text 'private bool CanGenerateItemSlots()' '        private bool HasItemSlotViews()' $helpers 'generated item slot helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched item slot prefab generation support.'
