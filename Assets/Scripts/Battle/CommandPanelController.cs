using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public class CommandPanelController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainCommandButtons;
        [SerializeField] private GameObject skillListPanel;
        [SerializeField] private GameObject swapListPanel;
        [SerializeField] private GameObject itemListPanel;

        [Header("Buttons")]
        [SerializeField] private Button fightButton;
        [SerializeField] private Button swapButton;
        [SerializeField] private Button itemButton;

        private void Awake()
        {
            BindButtons();
            ShowMain();
        }

        private void OnEnable()
        {
            ShowMain();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                ShowMain();
            }
        }

        public void ShowMain()
        {
            SetActive(mainCommandButtons, true);
            SetActive(skillListPanel, false);
            SetActive(swapListPanel, false);
            SetActive(itemListPanel, false);
        }

        public void ShowSkills()
        {
            SetActive(mainCommandButtons, true);
            SetActive(skillListPanel, true);
            SetActive(swapListPanel, false);
            SetActive(itemListPanel, false);
        }

        public void ShowSwap()
        {
            SetActive(mainCommandButtons, true);
            SetActive(skillListPanel, false);
            SetActive(swapListPanel, true);
            SetActive(itemListPanel, false);
        }

        public void ShowItems()
        {
            SetActive(mainCommandButtons, true);
            SetActive(skillListPanel, false);
            SetActive(swapListPanel, false);
            SetActive(itemListPanel, true);
        }

        private void BindButtons()
        {
            if (fightButton != null)
            {
                fightButton.onClick.RemoveListener(ShowSkills);
                fightButton.onClick.AddListener(ShowSkills);
            }

            if (swapButton != null)
            {
                swapButton.onClick.RemoveListener(ShowSwap);
                swapButton.onClick.AddListener(ShowSwap);
            }

            if (itemButton != null)
            {
                itemButton.onClick.RemoveListener(ShowItems);
                itemButton.onClick.AddListener(ShowItems);
            }
        }

        private void UnbindButtons()
        {
            if (fightButton != null) fightButton.onClick.RemoveListener(ShowSkills);
            if (swapButton != null) swapButton.onClick.RemoveListener(ShowSwap);
            if (itemButton != null) itemButton.onClick.RemoveListener(ShowItems);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
