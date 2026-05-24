$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Required {
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Label
    )

    if (!$Source.Contains($Old)) {
        throw "Patch anchor not found: $Label"
    }

    return $Source.Replace($Old, $New)
}

# 1) Add dedicated route overlay fields.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private Button _resultReturnButton;
        private TMP_Text _resultReturnButtonText;
        private GameObject _enemyActionPreviewPanelObject;
'@ `
    -New @'
        private Button _resultReturnButton;
        private TMP_Text _resultReturnButtonText;
        private RouteOverlayView _routeMovementPanel;
        private RouteOverlayView _routeEventPanel;
        private RouteOverlayView _battlePreparationPanel;
        private GameObject _enemyActionPreviewPanelObject;
'@ `
    -Label "route overlay fields"

# 2) Add the nested route overlay view class.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private class DefeatedEnemyInfo
        {
            public BattleUnit Unit;
            public GridPos Position;
        }

        // Battle setup
'@ `
    -New @'
        private class DefeatedEnemyInfo
        {
            public BattleUnit Unit;
            public GridPos Position;
        }

        private sealed class RouteOverlayView
        {
            private readonly string _panelName;
            private GameObject _panelObject;
            private TMP_Text _titleText;
            private TMP_Text _bodyText;
            private Button _leftButton;
            private TMP_Text _leftButtonText;
            private Button _rightButton;
            private TMP_Text _rightButtonText;

            public TMP_Text BodyText => _bodyText;
            public Button LeftButton => _leftButton;
            public TMP_Text LeftButtonText => _leftButtonText;
            public Button RightButton => _rightButton;
            public TMP_Text RightButtonText => _rightButtonText;

            public RouteOverlayView(string panelName)
            {
                _panelName = panelName;
            }

            public void Ensure(Canvas canvas, GameObject existing)
            {
                if (_panelObject != null)
                {
                    return;
                }

                if (existing != null)
                {
                    _panelObject = existing;
                    BindExistingChildren();
                    ApplyLayout();
                    return;
                }

                if (canvas == null)
                {
                    return;
                }

                _panelObject = new GameObject(_panelName);
                _panelObject.transform.SetParent(canvas.transform, false);

                RectTransform panelRect = _panelObject.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.30f, 0.22f);
                panelRect.anchorMax = new Vector2(0.70f, 0.78f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                Image panelImage = _panelObject.AddComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.80f);

                _titleText = CreateLabel("Title", TextAlignmentOptions.Center, 40f, new Vector2(0f, 0.78f), new Vector2(1f, 0.94f), Vector2.zero, Vector2.zero);
                _bodyText = CreateLabel("Body", TextAlignmentOptions.TopLeft, 22f, new Vector2(0f, 0.24f), new Vector2(1f, 0.76f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
                _leftButton = CreateButton("LeftButton", out _leftButtonText);
                _rightButton = CreateButton("RightButton", out _rightButtonText);

                ApplyLayout();
                SetVisible(false);
            }

            public void SetVisible(bool visible)
            {
                if (_panelObject != null)
                {
                    _panelObject.SetActive(visible);
                }
            }

            public void Show(Color panelColor, TextAlignmentOptions bodyAlignment, float titleFontSize, float bodyFontSize, string title, string body)
            {
                SetVisible(true);

                Image panelImage = _panelObject == null ? null : _panelObject.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = panelColor;
                }

                if (_titleText != null)
                {
                    _titleText.text = title;
                    _titleText.fontSize = titleFontSize;
                    _titleText.alignment = TextAlignmentOptions.Center;
                }

                if (_bodyText != null)
                {
                    _bodyText.text = body;
                    _bodyText.fontSize = bodyFontSize;
                    _bodyText.alignment = bodyAlignment;
                }
            }

            public void SetButtons(bool showLeft, string leftText, bool leftInteractable, UnityEngine.Events.UnityAction leftHandler, string rightText, UnityEngine.Events.UnityAction rightHandler)
            {
                ConfigureButton(_leftButton, _leftButtonText, showLeft, leftText, leftInteractable, leftHandler);
                ConfigureButton(_rightButton, _rightButtonText, true, rightText, true, rightHandler);
            }

            private void BindExistingChildren()
            {
                if (_panelObject == null)
                {
                    return;
                }

                _titleText = _panelObject.transform.Find("Title")?.GetComponent<TMP_Text>();
                _bodyText = _panelObject.transform.Find("Body")?.GetComponent<TMP_Text>();
                _leftButton = _panelObject.transform.Find("LeftButton")?.GetComponent<Button>();
                _leftButtonText = _leftButton == null ? null : _leftButton.GetComponentInChildren<TMP_Text>(true);
                _rightButton = _panelObject.transform.Find("RightButton")?.GetComponent<Button>();
                _rightButtonText = _rightButton == null ? null : _rightButton.GetComponentInChildren<TMP_Text>(true);
            }

            private TMP_Text CreateLabel(string objectName, TextAlignmentOptions alignment, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
            {
                GameObject labelObject = new GameObject(objectName);
                labelObject.transform.SetParent(_panelObject.transform, false);

                RectTransform rect = labelObject.AddComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;

                TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
                label.alignment = alignment;
                label.fontSize = fontSize;
                label.raycastTarget = false;
                return label;
            }

            private Button CreateButton(string objectName, out TMP_Text buttonText)
            {
                GameObject buttonObject = new GameObject(objectName);
                buttonObject.transform.SetParent(_panelObject.transform, false);

                RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;

                Image buttonImage = buttonObject.AddComponent<Image>();
                buttonImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

                Button button = buttonObject.AddComponent<Button>();

                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(buttonObject.transform, false);

                RectTransform textRect = textObject.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                buttonText = textObject.AddComponent<TextMeshProUGUI>();
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.fontSize = 24f;
                buttonText.raycastTarget = false;

                return button;
            }

            private void ApplyLayout()
            {
                ApplyButtonLayout(_leftButton, 0.08f, 0.46f);
                ApplyButtonLayout(_rightButton, 0.54f, 0.92f);
            }

            private static void ApplyButtonLayout(Button button, float minX, float maxX)
            {
                if (button == null)
                {
                    return;
                }

                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect == null)
                {
                    return;
                }

                rect.anchorMin = new Vector2(minX, 0.08f);
                rect.anchorMax = new Vector2(maxX, 0.20f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            private static void ConfigureButton(Button button, TMP_Text label, bool visible, string text, bool interactable, UnityEngine.Events.UnityAction handler)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(visible);
                    button.interactable = interactable;
                    button.onClick.RemoveAllListeners();

                    if (handler != null)
                    {
                        button.onClick.AddListener(handler);
                    }
                }

                if (label != null)
                {
                    label.text = text ?? string.Empty;
                }
            }
        }

        // Battle setup
'@ `
    -Label "RouteOverlayView class"

# 3) Ensure route overlay panels at startup.
$text = Replace-Required `
    -Source $text `
    -Old @'
            EnsureResultPanel();
            EnsureEnemyActionPreviewPanel();
            RedrawBoard();
            HideActionOverlay();
            HideResultPanel();
            SetEnemyActionPreviewVisible(false);
'@ `
    -New @'
            EnsureResultPanel();
            EnsureRouteOverlayPanels();
            EnsureEnemyActionPreviewPanel();
            RedrawBoard();
            HideActionOverlay();
            HideResultPanel();
            HideRouteOverlayPanels();
            SetEnemyActionPreviewVisible(false);
'@ `
    -Label "startup route overlay ensure"

# 4) Add route overlay helper methods before Result UI.
$text = Replace-Required `
    -Source $text `
    -Old @'
        // Result UI
        private void EnsureResultPanel()
'@ `
    -New @'
        private Canvas GetOverlayCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && commandPanel != null)
            {
                canvas = commandPanel.GetComponentInParent<Canvas>();
            }

            return canvas;
        }

        private void EnsureRouteOverlayPanels()
        {
            Canvas canvas = GetOverlayCanvas();
            if (canvas == null)
            {
                return;
            }

            _routeMovementPanel ??= new RouteOverlayView("RouteMovementPanel");
            _routeEventPanel ??= new RouteOverlayView("RouteEventPanel");
            _battlePreparationPanel ??= new RouteOverlayView("BattlePreparationPanel");

            _routeMovementPanel.Ensure(canvas, FindUiGameObjectByName("RouteMovementPanel"));
            _routeEventPanel.Ensure(canvas, FindUiGameObjectByName("RouteEventPanel"));
            _battlePreparationPanel.Ensure(canvas, FindUiGameObjectByName("BattlePreparationPanel"));
        }

        private void HideRouteOverlayPanels()
        {
            EnsureRouteOverlayPanels();

            _routeMovementPanel?.SetVisible(false);
            _routeEventPanel?.SetVisible(false);
            _battlePreparationPanel?.SetVisible(false);
        }

        private void PrepareRouteOverlayForOverlay(RouteOverlayView panel)
        {
            EnsureRouteOverlayPanels();

            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;

            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();
            HideResultPanel();
            HideRouteOverlayPanels();

            panel?.SetVisible(true);
        }

        // Result UI
        private void EnsureResultPanel()
'@ `
    -Label "route overlay helper methods"

# 5) Result screens must hide route overlays.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void PrepareResultPanelForOverlay()
        {
            EnsureResultPanel();

            _battleEnded = true;
'@ `
    -New @'
        private void PrepareResultPanelForOverlay()
        {
            EnsureResultPanel();
            HideRouteOverlayPanels();

            _battleEnded = true;
'@ `
    -Label "result hides route overlays"

# 6) Route Movement now uses RouteMovementPanel, not ResultPanel.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void ShowRouteMovementPanel()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = true;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareResultPanelForOverlay();
            ApplyResultPanelVisualStyle(new Color(0.05f, 0.10f, 0.18f, 0.90f), TextAlignmentOptions.TopLeft, 40f, 23f);
            SetResultTitleAndBody("MOVEMENT / ROUTE", BuildRouteMovementText());
            HideResultLeftButton("Move");

            if (_resultReturnButton != null)
            {
                _resultReturnButton.onClick.RemoveAllListeners();
                _resultReturnButton.onClick.AddListener(HandleRouteMovementMoveClicked);
            }

            Debug.Log("[Route] Movement panel shown.");
        }
'@ `
    -New @'
        private void ShowRouteMovementPanel()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = true;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay(_routeMovementPanel);
            _routeMovementPanel?.Show(new Color(0.05f, 0.10f, 0.18f, 0.90f), TextAlignmentOptions.TopLeft, 40f, 23f, "MOVEMENT / ROUTE", BuildRouteMovementText());
            _routeMovementPanel?.SetButtons(false, string.Empty, false, null, "Move", HandleRouteMovementMoveClicked);

            Debug.Log("[Route] Movement panel shown.");
        }
'@ `
    -Label "route movement panel replacement"

# 7) Route Event now uses RouteEventPanel.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void ShowRouteEventPanel(RoutePointData point)
        {
            _showingRouteEvent = true;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareResultPanelForOverlay();
            ApplyResultPanelVisualStyle(new Color(0.18f, 0.08f, 0.20f, 0.90f), TextAlignmentOptions.Center, 40f, 24f);

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Route Event"
                : point.DisplayName;

            SetResultTitleAndBody("ROUTE EVENT", BuildRouteEventText(point));
            HideResultLeftButton("Next");

            if (_resultReturnButton != null)
            {
                _resultReturnButton.onClick.RemoveAllListeners();
                _resultReturnButton.onClick.AddListener(HandleRouteEventNextClicked);
            }

            Debug.Log($"[Route] Event shown: {displayName}");
        }
'@ `
    -New @'
        private void ShowRouteEventPanel(RoutePointData point)
        {
            _showingRouteEvent = true;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay(_routeEventPanel);

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Route Event"
                : point.DisplayName;

            _routeEventPanel?.Show(new Color(0.18f, 0.08f, 0.20f, 0.90f), TextAlignmentOptions.Center, 40f, 24f, "ROUTE EVENT", BuildRouteEventText(point));
            _routeEventPanel?.SetButtons(false, string.Empty, false, null, "Next", HandleRouteEventNextClicked);

            Debug.Log($"[Route] Event shown: {displayName}");
        }
'@ `
    -Label "route event panel replacement"

# 8) Battle Preparation now uses BattlePreparationPanel.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void ShowBattlePreparationPanel(RoutePointData point)
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = true;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareResultPanelForOverlay();
            ApplyResultPanelVisualStyle(new Color(0.20f, 0.12f, 0.05f, 0.90f), TextAlignmentOptions.TopLeft, 40f, 22f);

            string title = point != null && point.PointType == RoutePointType.Boss
                ? "BOSS PREPARATION"
                : "BATTLE PREPARATION";

            SetResultTitleAndBody(title, BuildBattlePreparationText(point));
            RefreshBattlePreparationButtons(point);
            SetResultReturnButtonHandler(HandleBattlePreparationStartClicked);

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Battle Point"
                : point.DisplayName;

            Debug.Log($"[Preparation] Shown for {displayName}.");
        }
'@ `
    -New @'
        private void ShowBattlePreparationPanel(RoutePointData point)
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = true;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay(_battlePreparationPanel);

            string title = point != null && point.PointType == RoutePointType.Boss
                ? "BOSS PREPARATION"
                : "BATTLE PREPARATION";

            _battlePreparationPanel?.Show(new Color(0.20f, 0.12f, 0.05f, 0.90f), TextAlignmentOptions.TopLeft, 40f, 22f, title, BuildBattlePreparationText(point));
            RefreshBattlePreparationButtons(point);

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Battle Point"
                : point.DisplayName;

            Debug.Log($"[Preparation] Shown for {displayName}.");
        }
'@ `
    -Label "battle preparation panel replacement"

# 9) Refresh Preparation body/buttons against the dedicated panel.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void RefreshBattlePreparationPanel(RoutePointData point)
        {
            if (_resultSubText != null)
            {
                _resultSubText.text = BuildBattlePreparationText(point);
            }

            RefreshBattlePreparationButtons(point);
        }
'@ `
    -New @'
        private void RefreshBattlePreparationPanel(RoutePointData point)
        {
            if (_battlePreparationPanel != null && _battlePreparationPanel.BodyText != null)
            {
                _battlePreparationPanel.BodyText.text = BuildBattlePreparationText(point);
            }

            RefreshBattlePreparationButtons(point);
        }
'@ `
    -Label "refresh preparation body"

$text = Replace-Required `
    -Source $text `
    -Old @'
        private void RefreshBattlePreparationButtons(RoutePointData point)
        {
            bool canScout = point != null
                && point.HasBattleData
                && !IsRoutePointScouted(point)
                && _kakeraStock > 0;

            if (_resultFormationButton != null)
            {
                _resultFormationButton.gameObject.SetActive(canScout);
                _resultFormationButton.interactable = canScout;
            }

            if (_resultFormationButtonText != null)
            {
                _resultFormationButtonText.text = "Scout -1";
            }

            if (_resultReturnButton != null)
            {
                _resultReturnButton.gameObject.SetActive(true);
            }

            if (_resultReturnButtonText != null)
            {
                _resultReturnButtonText.text = "Start Battle";
            }
        }
'@ `
    -New @'
        private void RefreshBattlePreparationButtons(RoutePointData point)
        {
            bool canScout = point != null
                && point.HasBattleData
                && !IsRoutePointScouted(point)
                && _kakeraStock > 0;

            EnsureRouteOverlayPanels();
            _battlePreparationPanel?.SetButtons(canScout, "Scout -1", canScout, HandlePreparationScoutClicked, "Start Battle", HandleBattlePreparationStartClicked);
        }
'@ `
    -Label "refresh preparation buttons"

# 10) Hide route overlays when starting/restarting battle.
$text = Replace-Required `
    -Source $text `
    -Old @'
            HideResultPanel();
            HideActionOverlay();
            ClearTargetPreview();
'@ `
    -New @'
            HideResultPanel();
            HideRouteOverlayPanels();
            HideActionOverlay();
            ClearTargetPreview();
'@ `
    -Label "hide route overlay on battle start"

$text = Replace-Required `
    -Source $text `
    -Old @'
            HideResultPanel();
            HideActionOverlay();
            SetCommandUiVisible(true);
'@ `
    -New @'
            HideResultPanel();
            HideRouteOverlayPanels();
            HideActionOverlay();
            SetCommandUiVisible(true);
'@ `
    -Label "hide route overlay on restart"

# 11) ShowResultPanel fallback should not leave route panels visible.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private void ShowResultPanel(string result)
        {
            EnsureResultPanel();

            if (_resultPanelObject != null)
'@ `
    -New @'
        private void ShowResultPanel(string result)
        {
            EnsureResultPanel();
            HideRouteOverlayPanels();

            if (_resultPanelObject != null)
'@ `
    -Label "fallback result hides route overlays"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager route overlay panels."
