namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // ACTIONS AREA
        // ------------------------------------------------------------
        // Player and enemy action resolution methods live here.
        // This file should contain the readable flow of actions.
        // Damage calculation, KO, status panels, preview, and animation can be called from here.
        // ============================================================

        // Player actions
        private void HandleSkillClicked(SkillData skill)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (!CanUseSkill(_active, skill))
            {
                return;
            }

            BattleUnit actor = _active;
            BattleUnit linkPartner = GetLinkPartnerForSkill(actor, skill);
            string userDisplayName = BuildSkillUserDisplayName(actor, linkPartner);

            EnterResolvingAction();
            ShowActionOverlay(skill.SkillName, userDisplayName);

            StartCoroutine(ResolvePlayerSkillAfterIntroDelay(skill, actor, linkPartner, userDisplayName));
        }


        private void HandleItemClicked(InventoryItem inventoryItem)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (inventoryItem == null || inventoryItem.Item == null)
            {
                return;
            }

            if (inventoryItem.Count <= 0)
            {
                Debug.Log($"[Item] No {inventoryItem.Item.ItemName} left. Item cannot be used.");
                return;
            }

            switch (inventoryItem.Item.Kind)
            {
                case ItemKind.Pass:
                    HandlePassItem(inventoryItem);
                    return;

                case ItemKind.Heal:
                default:
                    HandleHealItem(inventoryItem);
                    return;
            }
        }


        private void HandleRotateClicked()
        {
            if (!CanAcceptRotateCommand())
            {
                return;
            }

            _formation.RotateAlliesClockwise();
            SyncBoardUnitGridPositions();

            _formationSettling = true;
            _lastRotateTime = Time.time;

            if (commandPanel != null)
            {
                commandPanel.SetInteractable(false);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }

            RedrawBoard();
            ReapplySkillHoverPreviewIfNeeded();
        }


        private void HandleMouseWheelRotateInput()
        {
            if (!enableMouseWheelRotate || !CanAcceptRotateCommand())
            {
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < Mathf.Max(0.0001f, mouseWheelRotateThreshold))
            {
                return;
            }

            // Current rotation supports the same direction as the existing Rotate button.
            // Wheel up rotates once. Wheel down rotates the same rotate operation three times,
            // which is equivalent to reverse rotation in a four-cell formation.
            bool reverse = invertMouseWheelRotate ? scroll > 0f : scroll < 0f;
            int rotateCount = reverse ? 3 : 1;
            for (int i = 0; i < rotateCount; i++)
            {
                HandleRotateClicked();
            }
        }

        private IEnumerator ResolvePlayerSkillAfterIntroDelay(SkillData skill, BattleUnit actor, BattleUnit linkPartner, string userDisplayName)
        {
            float delay = Mathf.Max(0f, actionIntroDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (_battleEnded || actor == null || actor.IsDead || skill == null)
            {
                yield break;
            }

            _active = actor;
            BeginDeferredHpBarFill();
            ConsumeSkillMP(actor, skill, linkPartner);

            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(actor, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            yield return PlaySkillAnimationIfAny(skill);

            ApplySkillDamage(skill);
            ApplySkillEffect(skill);
            RedrawBoard();
            ReapplySkillHoverPreviewDuringActionIfNeeded();

            if (_battleEnded)
            {
                RedrawBoard();
                ReapplySkillHoverPreviewDuringActionIfNeeded();
                yield break;
            }

            StartCoroutine(FinishPlayerActionAfterDelay());
        }

        // Skill effects and damage
        private void ApplySkillDamage(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            List<DefeatedEnemyInfo> defeatedEnemies = new();
            List<GridPos> targets = GetSkillDamageTargetPositions(skill);

            for (int i = 0; i < targets.Count; i++)
            {
                DamageEnemyAt(targets[i], skill.Damage, defeatedEnemies);
            }

            ResolveDefeatedEnemies(defeatedEnemies);
        }


        private void ApplySkillHeal(SkillData skill)
        {
            if (skill == null || skill.HealAmount <= 0)
            {
                return;
            }

            List<BattleUnit> healTargets = GetSkillEffectTargets(skill);
            for (int i = 0; i < healTargets.Count; i++)
            {
                HealAllyUnit(healTargets[i], skill.HealAmount);
            }
        }


        private void ShowPendingActionValuePopups()
        {
            HideActiveActionValuePopups();

            if (_pendingActionValuePopups.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingActionValuePopups.Count; i++)
            {
                ActionValuePopup popup = _pendingActionValuePopups[i];
                if (popup == null)
                {
                    continue;
                }

                TMP_Text cellLabel = GetBoardCellLabel(popup.IsAllyBoard, popup.Position);
                if (cellLabel == null || cellLabel.transform.parent == null)
                {
                    continue;
                }

                GameObject popupObject = new GameObject("ActionValuePopup");
                popupObject.transform.SetParent(cellLabel.transform.parent, false);

                RectTransform rect = popupObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(120f, 48f);

                BattleUnit popupUnit = _grid.GetUnit(popup.IsAllyBoard, popup.Position);
                rect.anchoredPosition = GetDamagePopupOffset(popupUnit);

                TMP_Text label = popupObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 28f;
                label.raycastTarget = false;
                label.text = popup.Text;
                ApplyActionValuePopupColor(label, popup.Text);

                _activeActionValuePopupLabels.Add(label);
            }
        }
    }
}
