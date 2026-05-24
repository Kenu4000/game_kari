using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    /// <summary>
    /// Runtime-created overlay panel used by route movement, route events,
    /// and battle preparation screens.
    /// </summary>
    internal sealed class RouteOverlayView
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
}
