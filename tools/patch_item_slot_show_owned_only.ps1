$ErrorActionPreference = 'Stop'

$itemSlotPath = 'Assets/Scripts/Battle/ItemSlotView.cs'
$commandPath = 'Assets/Scripts/Battle/CommandPanelController.cs'

foreach ($path in @($itemSlotPath, $commandPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

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

# ItemSlotView: allow CommandPanelController to hide empty slots.
$itemSlotText = Get-Content -Path $itemSlotPath -Raw -Encoding UTF8
$itemSlotText = InsertBeforeIfMissing $itemSlotText 'public void SetVisible(bool visible)' '        public void SetItem(' @'
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

'@ 'ItemSlotView.SetVisible'
Set-Content -Path $itemSlotPath -Value $itemSlotText -Encoding UTF8

# CommandPanelController: bind only currently owned items to visible ItemSlotViews.
$text = Get-Content -Path $commandPath -Raw -Encoding UTF8

$text = ReplaceOptional $text @'
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
'@ @'
        private void BindItemSlotViews()
        {
            if (itemSlotViews == null)
            {
                return;
            }

            List<InventoryItem> visibleItems = GetVisibleInventoryItems();

            for (int i = 0; i < itemSlotViews.Length; i++)
            {
                ItemSlotView slotView = itemSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                InventoryItem inventoryItem = i < visibleItems.Count ? visibleItems[i] : null;
                bool visible = inventoryItem != null;

                slotView.SetVisible(visible);
                if (visible)
                {
                    slotView.SetItem(inventoryItem, OnItemClicked, HandleItemHovered, ClearDescription);
                }
            }
        }
'@ 'BindItemSlotViews owned only'

$helper = @'
        private List<InventoryItem> GetVisibleInventoryItems()
        {
            var visibleItems = new List<InventoryItem>();

            if (_inventoryItems == null)
            {
                return visibleItems;
            }

            for (int i = 0; i < _inventoryItems.Count; i++)
            {
                InventoryItem inventoryItem = _inventoryItems[i];
                if (inventoryItem == null || inventoryItem.Item == null || inventoryItem.Count <= 0)
                {
                    continue;
                }

                visibleItems.Add(inventoryItem);
            }

            return visibleItems;
        }

'@

$text = InsertBeforeIfMissing $text 'private List<InventoryItem> GetVisibleInventoryItems()' '        private void HandleItemHovered(InventoryItem inventoryItem)' $helper 'GetVisibleInventoryItems'

Set-Content -Path $commandPath -Value $text -Encoding UTF8
Write-Host 'Patched item slots to show currently owned items only.'
