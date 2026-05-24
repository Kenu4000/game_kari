using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameKari.Battle
{
    /// <summary>
    /// Owns the runtime-created ResultPanel and applies result screen view state.
    /// </summary>
    internal sealed class ResultPanelPresenter
    {
        private GameObject _panelObject;
        private TMP_Text _titleText;
        private TMP_Text _bodyText;
        private Button _leftButton;
        private TMP_Text _leftButtonText;
        private Button _rightButton;
        private TMP_Text _rightButtonText;

        public void Ensure(Canvas canvas, Func<string, GameObject> findExistingPanel, UnityAction defaultLeftHandler, UnityAction defaultRightHandler)
        {
            if (_panelObject != null)
            {
                return;
            }

            if (canvas == null)
            {
                return;
            }

            GameObject existing = findExistingPanel?.Invoke("ResultPanel");
            if (existing != null)
            {
                _panelObject = existing;
                BindExistingChildren();
                EnsureButtons(defaultLeftHandler, defaultRightHandler);
                ApplyLayout();
                return;
            }

            _panelObject = new GameObject("ResultPanel");
            _panelObject.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = _panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.32f, 0.28f);
            panelRect.anchorMax = new Vector2(0.68f, 0.72f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = _panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.78f);

            _titleText = CreateLabel("ResultTitle", TextAlignmentOptions.Center, 38f, new Vector2(0f, 0.70f), new Vector2(1f, 0.90f));
            _bodyText = CreateLabel("ResultSubText", TextAlignmentOptions.Center, 22f, new Vector2(0f, 0.28f), new Vector2(1f, 0.68f));
            _leftButton = CreateButton("FormationButton", out _leftButtonText, defaultLeftHandler);
            _rightButton = CreateButton("ReturnButton", out _rightButtonText, defaultRightHandler);

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

        public void ApplyVisualStyle(Color panelColor, TextAlignmentOptions bodyAlignment, float titleFontSize, float bodyFontSize)
        {
            ApplyLayout();

            Image panelImage = _panelObject == null ? null : _panelObject.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = panelColor;
            }

            if (_titleText != null)
            {
                _titleText.fontSize = titleFontSize;
                _titleText.alignment = TextAlignmentOptions.Center;
            }

            if (_bodyText != null)
            {
                _bodyText.fontSize = bodyFontSize;
                _bodyText.alignment = bodyAlignment;
            }
        }

        public void SetTitleAndBody(string title, string body)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            SetBody(body);
        }

        public void SetBody(string body)
        {
            if (_bodyText != null)
            {
                _bodyText.text = body ?? string.Empty;
            }
        }

        public void SetRightButtonHandler(UnityAction handler)
        {
            if (_rightButton == null)
            {
                return;
            }

            _rightButton.onClick.RemoveAllListeners();

            if (handler != null)
            {
                _rightButton.onClick.AddListener(handler);
            }
        }

        public void SetButtons(bool showLeftButton, string leftText, bool leftInteractable, string rightText)
        {
            if (_leftButton != null)
            {
                _leftButton.gameObject.SetActive(showLeftButton);
                _leftButton.interactable = leftInteractable;
            }

            if (_leftButtonText != null)
            {
                _leftButtonText.text = leftText ?? string.Empty;
            }

            if (_rightButton != null)
            {
                _rightButton.gameObject.SetActive(true);
            }

            if (_rightButtonText != null)
            {
                _rightButtonText.text = rightText ?? string.Empty;
            }
        }

        private void BindExistingChildren()
        {
            if (_panelObject == null)
            {
                return;
            }

            TMP_Text[] labels = _panelObject.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null)
                {
                    continue;
                }

                string lowerName = label.name.ToLowerInvariant();
                if (lowerName.Contains("title"))
                {
                    _titleText = label;
                }
                else if (lowerName.Contains("sub") || lowerName == "body")
                {
                    _bodyText = label;
                }
            }

            Transform left = _panelObject.transform.Find("FormationButton");
            _leftButton = left == null ? null : left.GetComponent<Button>();
            _leftButtonText = _leftButton == null ? null : _leftButton.GetComponentInChildren<TMP_Text>(true);

            Transform right = _panelObject.transform.Find("ReturnButton");
            _rightButton = right == null ? null : right.GetComponent<Button>();
            _rightButtonText = _rightButton == null ? null : _rightButton.GetComponentInChildren<TMP_Text>(true);
        }

        private void EnsureButtons(UnityAction defaultLeftHandler, UnityAction defaultRightHandler)
        {
            if (_leftButton == null)
            {
                _leftButton = CreateButton("FormationButton", out _leftButtonText, defaultLeftHandler);
            }
            else
            {
                _leftButton.onClick.RemoveAllListeners();
                if (defaultLeftHandler != null)
                {
                    _leftButton.onClick.AddListener(defaultLeftHandler);
                }
            }

            if (_rightButton == null)
            {
                _rightButton = CreateButton("ReturnButton", out _rightButtonText, defaultRightHandler);
            }
            else
            {
                _rightButton.onClick.RemoveAllListeners();
                if (defaultRightHandler != null)
                {
                    _rightButton.onClick.AddListener(defaultRightHandler);
                }
            }
        }

        private TMP_Text CreateLabel(string objectName, TextAlignmentOptions alignment, float fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(_panelObject.transform, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = alignment;
            label.fontSize = fontSize;
            label.raycastTarget = false;
            return label;
        }

        private Button CreateButton(string objectName, out TMP_Text buttonText, UnityAction handler)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(_panelObject.transform, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            if (handler != null)
            {
                button.onClick.AddListener(handler);
            }

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
            if (_panelObject != null)
            {
                RectTransform panelRect = _panelObject.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.32f, 0.28f);
                    panelRect.anchorMax = new Vector2(0.68f, 0.72f);
                    panelRect.offsetMin = Vector2.zero;
                    panelRect.offsetMax = Vector2.zero;
                }
            }

            ApplyButtonLayout(_leftButton, 0.08f, 0.46f);
            ApplyButtonLayout(_rightButton, 0.54f, 0.92f);
        }

        private static void ApplyButtonLayout(Button button, float minX, float maxX)
        {
            if (button == null)
            {
                return;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null)
            {
                return;
            }

            buttonRect.anchorMin = new Vector2(minX, 0.08f);
            buttonRect.anchorMax = new Vector2(maxX, 0.22f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
        }
    }
}
