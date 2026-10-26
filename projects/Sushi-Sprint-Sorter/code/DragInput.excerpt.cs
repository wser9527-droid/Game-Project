// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/SorterGame/Scripts/SorterGameManager.cs
// Dependencies: Original SorterGameManager fields and helpers, SorterLetter, camera, physics and UI callbacks.

// Source lines 631-749
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                SorterLetter hitLetter = HitTestLetter(Input.mousePosition);
                if (hitLetter != null)
                {
                    BeginDrag(hitLetter);
                    DragPointer(Input.mousePosition);
                }
            }

            if (draggedLetter != null && Input.GetMouseButton(0))
            {
                DragPointer(Input.mousePosition);
            }

            if (draggedLetter != null && Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        private void BeginDrag(SorterLetter letter)
        {
            if (draggedLetter != null && draggedLetter != letter)
            {
                draggedLetter.CancelDrag();
            }

            draggedLetter = letter;
            CurrentLetter = letter;
            draggedLetter.BeginDrag();
            UpdateLaneDragHighlight();
            RefreshUi();
        }

        private void DragPointer(Vector2 screenPosition)
        {
            if (draggedLetter == null)
            {
                return;
            }

            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(mainCamera.transform.position.z)));
            draggedLetter.DragToWorldX(world.x);
            UpdateLaneDragHighlight();
            RefreshUi();
        }

        private void EndDrag()
        {
            if (draggedLetter == null)
            {
                return;
            }

            draggedLetter.EndDragToNearestLane();
            draggedLetter = null;
            HideLaneDragHighlights();
            RefreshUi();
        }

        private SorterLetter HitTestLetter(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            SorterLetter bestLetter = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                SorterLetter letter = hits[i].collider.GetComponent<SorterLetter>();
                if (letter == null || !letters.Contains(letter))
                {
                    continue;
                }

                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    bestLetter = letter;
                }
            }

            return bestLetter != null ? bestLetter : FindNearestLetterNearScreenPoint(screenPosition);
        }

        private SorterLetter FindNearestLetterNearScreenPoint(Vector2 screenPosition)
        {
            SorterLetter nearestLetter = null;
            float nearestScreenDistance = touchForgivenessPixels;
            float nearestY = float.NegativeInfinity;
            for (int i = 0; i < letters.Count; i++)
            {
                SorterLetter candidate = letters[i];
                if (candidate == null)
                {
                    continue;
                }

                Vector3 candidateScreen = mainCamera.WorldToScreenPoint(candidate.transform.position);
                if (candidateScreen.z < 0f)
                {
                    continue;
                }

                float distance = Vector2.Distance(screenPosition, new Vector2(candidateScreen.x, candidateScreen.y));
                bool closer = distance < nearestScreenDistance - 0.01f;
                bool sameDistanceButLeading = Mathf.Abs(distance - nearestScreenDistance) <= 0.01f && candidate.transform.position.y > nearestY;
                if (closer || sameDistanceButLeading)
                {
                    nearestScreenDistance = distance;
                    nearestY = candidate.transform.position.y;
                    nearestLetter = candidate;
                }
            }

            return nearestLetter;
        }
