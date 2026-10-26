// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/DreamRepair/Scripts/DreamRepairGame.cs
// Dependencies: Members of DreamRepairGame, UnityEngine, skill definitions and visual/audio feedback.

// Source lines 5039-5126
        private void UpdateGestureInput()
        {
            if (!TutorialAllowsGestureInput)
            {
                if (inputActive)
                {
                    inputActive = false;
                    gesturePoints.Clear();
                    EndLiveGesture(DreamTraceEdgeColor(0.34f));
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                inputActive = true;
                gesturePoints.Clear();
                var point = ScreenToWorld(Input.mousePosition);
                gesturePoints.Add(point);
                lastGestureSampleTime = Time.time;
                BeginLiveGesture(point);
                PlaySfx("draw_line", drawLineClip, 0.42f, 0.08f);
            }
            if (inputActive && Input.GetMouseButton(0))
            {
                var point = ScreenToWorld(Input.mousePosition);
                var enoughDistance = gesturePoints.Count == 0 || Vector2.Distance(gesturePoints[^1], point) > 0.055f;
                var enoughTime = Time.time - lastGestureSampleTime >= 0.024f;
                if (enoughDistance && enoughTime)
                {
                    gesturePoints.Add(point);
                    lastGestureSampleTime = Time.time;
                    UpdateLiveGesture(point);
                }
                else
                {
                    MoveNeedle(point);
                }
            }
            if (inputActive && Input.GetMouseButtonUp(0))
            {
                inputActive = false;
                var point = ScreenToWorld(Input.mousePosition);
                if (gesturePoints.Count == 0 || Vector2.Distance(gesturePoints[^1], point) > 0.025f) gesturePoints.Add(point);
                UpdateLiveGesture(point);
                FinishGesture();
            }
        }

        private void FinishGesture()
        {
            if (gesturePoints.Count < 2) return;
            EndLiveGesture(DreamTraceColor(0.68f));
            var length = 0f;
            var min = gesturePoints[0];
            var max = gesturePoints[0];
            for (var i = 1; i < gesturePoints.Count; i++)
            {
                length += Vector2.Distance(gesturePoints[i - 1], gesturePoints[i]);
                min = Vector2.Min(min, gesturePoints[i]);
                max = Vector2.Max(max, gesturePoints[i]);
            }
            var closed = Vector2.Distance(gesturePoints[0], gesturePoints[^1]);
            var center = (min + max) * 0.5f;
            if (closed < 0.42f && max.x - min.x >= 0.7f && max.y - min.y >= 0.7f && length >= 1.8f)
            {
                if (!CanUseSkillNow(SkillId.MemoryBubble))
                {
                    RejectTutorialSkill(SkillId.MemoryBubble);
                    return;
                }
                ExecuteGesture("Ring", SkillId.MemoryBubble, center, 0);
            }
            else if (length >= 0.28f)
            {
                if (!CanUseSkillNow(SkillId.PaperBird))
                {
                    RejectTutorialSkill(SkillId.PaperBird);
                    return;
                }
                ExecuteLineCut(gesturePoints);
            }
            else
            {
                lastSkill = "Line too short";
                FlashNeedle(DreamTraceEdgeColor(0.92f));
            }
        }
