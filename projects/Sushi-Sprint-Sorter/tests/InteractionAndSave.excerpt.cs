// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/SorterGame/Tests/PlayMode/SorterGameplayPlayModeTests.cs
// Dependencies: Original test fixture and Main scene, SorterGameManager, SorterProgressStore, NUnit and Unity Test Framework.

// Source lines 457-507
        [UnityTest]
        public IEnumerator DragOneTravelerDoesNotMoveOtherTravelers()
        {
            game.StartRun();
            SorterLetter first = game.SpawnLetterForTest(MailType.Leaf, 0);
            SorterLetter second = game.SpawnLetterForTest(MailType.Air, 1);
            yield return null;

            game.BeginDragForTest(first);
            game.DragCurrentForTest(game.LaneX(2));
            game.EndDragForTest();
            yield return null;

            Assert.AreEqual(2, first.CurrentLane);
            Assert.AreEqual(1, second.CurrentLane);
        }

        [UnityTest]
        public IEnumerator TapNearSushiStartsDragWithinForgivenessRadius()
        {
            game.StartRun();
            SorterLetter letter = game.SpawnLetterForTest(MailType.Leaf, 0);
            yield return null;

            BoxCollider collider = letter.GetComponent<BoxCollider>();
            Assert.NotNull(collider);
            Assert.Greater(collider.size.x, 1f);
            Assert.Greater(collider.size.y, 1f);

            Vector3 screenCenter = Camera.main.WorldToScreenPoint(letter.transform.position);
            game.BeginDragAtScreenPositionForTest(new Vector2(screenCenter.x + 54f, screenCenter.y + 38f));

            Assert.IsTrue(letter.IsDragging);
            Assert.AreSame(letter, game.CurrentLetter);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FarTapDoesNotStartDrag()
        {
            game.StartRun();
            SorterLetter letter = game.SpawnLetterForTest(MailType.Leaf, 0);
            yield return null;

            Vector3 screenCenter = Camera.main.WorldToScreenPoint(letter.transform.position);
            game.BeginDragAtScreenPositionForTest(new Vector2(screenCenter.x + 240f, screenCenter.y + 240f));
            yield return null;

            Assert.IsFalse(letter.IsDragging);
        }


// Source lines 1039-1062
        [UnityTest]
        public IEnumerator ShopPurchasesAndEquipsPersist()
        {
            Assert.AreEqual(0, game.WalletTips);
            Assert.IsTrue(game.Progress.IsOwned(SorterProgressStore.DefaultCharacterId));
            Assert.IsTrue(game.Progress.IsOwned(SorterProgressStore.DefaultTrackId));
            Assert.AreEqual(SorterProgressStore.DefaultCharacterId, game.EquippedCharacterId);
            Assert.IsFalse(game.PurchaseShopItem("button_imp"));
            Assert.AreEqual(0, game.WalletTips);

            game.SetWalletTipsForTest(4000);
            Assert.IsTrue(game.PurchaseShopItem("button_imp"));
            Assert.AreEqual(800, game.WalletTips);
            Assert.AreEqual("button_imp", game.EquippedCharacterId);
            Assert.IsFalse(game.PurchaseShopItem("soy_bottle"));
            Assert.AreEqual(800, game.WalletTips);

            SorterProgressStore reloaded = SorterProgressStore.Load();
            Assert.AreEqual(800, reloaded.WalletTips);
            Assert.IsTrue(reloaded.IsOwned("button_imp"));
            Assert.AreEqual("button_imp", reloaded.EquippedCharacterId);
            Assert.IsFalse(reloaded.Equip("lantern_bug"));
            yield return null;
        }

// Source lines 1303-1345
        [UnityTest]
        public IEnumerator DirtyEquippedSaveIdsFallBackToDefaults()
        {
            SorterProgressStore.DeleteSaveForTest();
            PlayerPrefs.SetInt(ProgressKeyPrefix + "WalletTips", 0);
            PlayerPrefs.SetString(ProgressKeyPrefix + "OwnedItems", "moss_pup,classic_belt,ghost_chef,ghost_belt");
            PlayerPrefs.SetString(ProgressKeyPrefix + "EquippedCharacter", "ghost_chef");
            PlayerPrefs.SetString(ProgressKeyPrefix + "EquippedTrack", "ghost_belt");
            PlayerPrefs.Save();

            SorterProgressStore loaded = SorterProgressStore.Load();
            Assert.AreEqual(SorterProgressStore.DefaultCharacterId, loaded.EquippedCharacterId);
            Assert.AreEqual(SorterProgressStore.DefaultTrackId, loaded.EquippedTrackId);
            loaded.Save();

            SorterProgressStore persisted = SorterProgressStore.Load();
            Assert.AreEqual(SorterProgressStore.DefaultCharacterId, persisted.EquippedCharacterId);
            Assert.AreEqual(SorterProgressStore.DefaultTrackId, persisted.EquippedTrackId);

            PlayerPrefs.SetString(ProgressKeyPrefix + "OwnedItems", "moss_pup,classic_belt");
            PlayerPrefs.SetString(ProgressKeyPrefix + "EquippedCharacter", SorterProgressStore.DefaultTrackId);
            PlayerPrefs.SetString(ProgressKeyPrefix + "EquippedTrack", SorterProgressStore.DefaultCharacterId);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Main");
            yield return null;
            game = Object.FindFirstObjectByType<SorterGameManager>();
            Assert.NotNull(game);
            Assert.AreEqual(SorterProgressStore.DefaultCharacterId, game.EquippedCharacterId);
            Assert.AreEqual(SorterProgressStore.DefaultTrackId, game.EquippedTrackId);

            Button shop = GameObject.Find("Shop Button")?.GetComponent<Button>();
            Assert.NotNull(shop);
            shop.onClick.Invoke();
            yield return null;
            Assert.AreEqual("EQUIPPED", GameObject.Find("Shop Item Action moss_pup").GetComponentInChildren<Text>().text);

            Button beltTab = GameObject.Find("Shop Belt Tab")?.GetComponent<Button>();
            Assert.NotNull(beltTab);
            beltTab.onClick.Invoke();
            yield return null;
            Assert.AreEqual("EQUIPPED", GameObject.Find("Shop Item Action classic_belt").GetComponentInChildren<Text>().text);
        }

// Source lines 1441-1481
        [UnityTest]
        public IEnumerator DraggingPausesVerticalMovementAndReleaseRestoresIt()
        {
            game.StartRun();
            SorterLetter letter = game.SpawnLetterForTest(MailType.Leaf, 0);
            yield return null;

            game.BeginDragForTest(letter);
            float heldY = letter.transform.position.y;
            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(letter.IsDragging);
            Assert.AreEqual(heldY, letter.transform.position.y, 0.02f);

            game.DragCurrentForTest(game.LaneX(1));
            game.EndDragForTest();
            Assert.IsFalse(letter.IsDragging);
            Assert.AreEqual(1, letter.CurrentLane);

            float releasedY = letter.transform.position.y;
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(letter.transform.position.y, releasedY);
        }

        [UnityTest]
        public IEnumerator DragReleaseSnapsToNearestLane()
        {
            game.StartRun();
            SorterLetter left = game.SpawnLetterForTest(MailType.Leaf, 1);
            game.BeginDragForTest(left);
            game.DragCurrentForTest(game.LaneX(0) + 0.1f);
            game.EndDragForTest();
            yield return null;
            Assert.AreEqual(0, left.CurrentLane);

            SorterLetter right = game.SpawnLetterForTest(MailType.Air, 1);
            game.BeginDragForTest(right);
            game.DragCurrentForTest(game.LaneX(2) - 0.1f);
            game.EndDragForTest();
            yield return null;
            Assert.AreEqual(2, right.CurrentLane);
        }
