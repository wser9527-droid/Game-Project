// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/DreamRepair/Scripts/DreamRepairGame.cs
// Dependencies: DreamRepairGame.safeAreaRoot, lastSafeArea, UpdateSlotMachineResponsiveLayout and UnityEngine UI.

// Source lines 3337-3366
        private RectTransform CreateSafeAreaRoot(RectTransform parent)
        {
            var go = new GameObject("Safe Area Root");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null) return;
            var safeArea = Screen.safeArea;
            if (safeArea == lastSafeArea) return;
            lastSafeArea = safeArea;
            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;
            min.x /= Mathf.Max(1, Screen.width);
            min.y /= Mathf.Max(1, Screen.height);
            max.x /= Mathf.Max(1, Screen.width);
            max.y /= Mathf.Max(1, Screen.height);
            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            UpdateSlotMachineResponsiveLayout();
        }
