// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/Scripts/LightPuzzleController.cs
// Dependencies: Original LightPuzzleController fields and UI helpers, LightPuzzleModel, BeamRenderer, SaveManager and PuzzleTool.

// Source lines 34-40
        public bool IsPreviewing => previewRoutine != null;
        public bool CanUseExtraMoves => isActiveAndEnabled && level != null && !solved && !extraMovesUsed && level.moveLimit > 0
            && SaveManager.GetToolCount(PuzzleTool.ExtraMoves) > 0;
        public bool CanUsePreview => isActiveAndEnabled && level != null && !solved && !previewUsed && beamRenderer != null
            && SaveManager.GetToolCount(PuzzleTool.Preview) > 0;
        public bool CanOpenExtraMovesShop => isActiveAndEnabled && level != null && !solved && !extraMovesUsed && level.moveLimit > 0;
        public bool CanOpenPreviewShop => isActiveAndEnabled && level != null && !solved && !previewUsed && beamRenderer != null;

// Source lines 203-227
        public bool ShowPreview()
        {
            if (!CanUsePreview)
            {
                return false;
            }

            if (!SaveManager.TryConsumeTool(PuzzleTool.Preview)) return false;
            previewUsed = true;

            if (swapSelection != null)
            {
                ApplyTileVisual(swapSelection);
                swapSelection = null;
            }

            swapMode = false;
            if (previewRoutine != null)
            {
                StopCoroutine(previewRoutine);
            }

            previewRoutine = StartCoroutine(PreviewRoutine());
            return true;
        }

// Source lines 700-752
        private IEnumerator PreviewRoutine()
        {
            LightSimulationResult preview = LightPuzzleModel.Simulate(level, solutionTile => solutionTile.solutionRotation);
            UpdateWaterConnections(preview, tile => tile.solutionRotation);
            foreach (Coroutine rotation in rotateRoutines.Values)
            {
                if (rotation != null)
                {
                    StopCoroutine(rotation);
                }
            }

            rotateRoutines.Clear();
            foreach (TileData tile in orderedTiles)
            {
                if (tileViews.TryGetValue(LightPuzzleModel.Key(tile.x, tile.y), out TileView view))
                {
                    view.Rect.localEulerAngles = new Vector3(0f, 0f, -90f * Normalize(tile.solutionRotation));
                }
            }

            beamRenderer.Render(level, preview, tileSize, boardOffset);
            beamRenderer.Pulse();

            float elapsed = 0f;
            while (elapsed < 1.35f)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.PingPong(elapsed * 3.5f, 1f);
                foreach (TileData tile in orderedTiles)
                {
                    if (!tileViews.TryGetValue(LightPuzzleModel.Key(tile.x, tile.y), out TileView view))
                    {
                        continue;
                    }

                    int mask = preview.CellMask(tile.x, tile.y);
                    view.Image.color = mask == 0
                        ? TileColor(tile)
                        : Color.Lerp(TileColor(tile), new Color(1f, 0.94f, 0.35f, 1f), pulse * 0.42f);
                }

                yield return null;
            }

            foreach (TileData tile in orderedTiles)
            {
                ApplyTileVisual(tile);
            }

            Evaluate();
            previewRoutine = null;
        }
