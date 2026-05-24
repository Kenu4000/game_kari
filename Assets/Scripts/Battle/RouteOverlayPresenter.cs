using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GameKari.Battle
{
    /// <summary>
    /// Owns the temporary route overlay panels and applies their view state.
    /// BattleUIManager remains responsible for battle phase/state transitions.
    /// </summary>
    internal sealed class RouteOverlayPresenter
    {
        private readonly RouteOverlayView _movementPanel = new("RouteMovementPanel");
        private readonly RouteOverlayView _eventPanel = new("RouteEventPanel");
        private readonly RouteOverlayView _preparationPanel = new("BattlePreparationPanel");

        public TMP_Text PreparationBodyText => _preparationPanel.BodyText;

        public void Ensure(Canvas canvas, Func<string, GameObject> findExistingPanel)
        {
            if (canvas == null)
            {
                return;
            }

            _movementPanel.Ensure(canvas, findExistingPanel?.Invoke("RouteMovementPanel"));
            _eventPanel.Ensure(canvas, findExistingPanel?.Invoke("RouteEventPanel"));
            _preparationPanel.Ensure(canvas, findExistingPanel?.Invoke("BattlePreparationPanel"));
        }

        public void HideAll()
        {
            _movementPanel.SetVisible(false);
            _eventPanel.SetVisible(false);
            _preparationPanel.SetVisible(false);
        }

        public void ShowMovement(string body, UnityAction onMoveClicked)
        {
            HideAll();
            _movementPanel.SetVisible(true);
            _movementPanel.Show(new Color(0.05f, 0.10f, 0.18f, 0.90f), TextAlignmentOptions.TopLeft, 40f, 23f, "MOVEMENT / ROUTE", body);
            _movementPanel.SetButtons(false, string.Empty, false, null, "Move", onMoveClicked);
        }

        public void ShowEvent(string body, UnityAction onNextClicked)
        {
            HideAll();
            _eventPanel.SetVisible(true);
            _eventPanel.Show(new Color(0.18f, 0.08f, 0.20f, 0.90f), TextAlignmentOptions.Center, 40f, 24f, "ROUTE EVENT", body);
            _eventPanel.SetButtons(false, string.Empty, false, null, "Next", onNextClicked);
        }

        public void ShowPreparation(string title, string body, bool canScout, UnityAction onScoutClicked, UnityAction onStartBattleClicked)
        {
            HideAll();
            _preparationPanel.SetVisible(true);
            _preparationPanel.Show(new Color(0.20f, 0.12f, 0.05f, 0.90f), TextAlignmentOptions.TopLeft, 40f, 22f, title, body);
            SetPreparationButtons(canScout, onScoutClicked, onStartBattleClicked);
        }

        public void RefreshPreparation(string body, bool canScout, UnityAction onScoutClicked, UnityAction onStartBattleClicked)
        {
            if (PreparationBodyText != null)
            {
                PreparationBodyText.text = body;
            }

            SetPreparationButtons(canScout, onScoutClicked, onStartBattleClicked);
        }

        public void SetPreparationButtons(bool canScout, UnityAction onScoutClicked, UnityAction onStartBattleClicked)
        {
            _preparationPanel.SetButtons(canScout, "Scout -1", canScout, onScoutClicked, "Start Battle", onStartBattleClicked);
        }
    }
}
