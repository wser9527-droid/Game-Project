// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/DreamRepair/Tests/DreamRepairPlayModeTests.cs
// Dependencies: Original DreamRepairPlayModeTests fixture, DreamRepairGame, NUnit and Unity Test Framework.

// Source lines 10-53
        [UnityTest]
        public IEnumerator SmokeFlowReachesFirstBoss()
        {
            var game = new GameObject("PlayMode Dream Repair").AddComponent<DreamRepairGame>();
            yield return null;
            game.StartRun();
            yield return null;

            Assert.AreEqual(2, game.UnifiedSkillSlotCountForTest);
            Assert.IsTrue(game.HasGrowthStatsBarForTest);
            Assert.IsTrue(game.GrowthStatsBarVisibleForTest);
            Assert.IsTrue(game.GrowthStatsBarRaycastSafeForTest);
            Assert.AreEqual(new Vector2(0f, -16f), game.TopHudBarPositionForTest);
            Assert.AreEqual(new Vector2(354f, 66f), game.TopHudBarSizeForTest);
            Assert.AreEqual(new Vector2(0f, -88f), game.GrowthStatsBarPositionForTest);
            Assert.AreEqual(new Vector2(354f, 28f), game.GrowthStatsBarSizeForTest);
            Assert.IsTrue(game.TopHudAndGrowthStatsSeparatedForTest);
            Assert.LessOrEqual(game.GrowthStatsBarBackgroundAlphaForTest, 0.01f);
            Assert.IsTrue(game.GrowthStatsBarHasNoBackingFrameForTest);
            Assert.GreaterOrEqual(game.GrowthStatsLabelFontSizeForTest, 14f);
            StringAssert.Contains("LINE Lv.1", game.GrowthStatsTextForTest);
            StringAssert.Contains("RING Lv.1", game.GrowthStatsTextForTest);
            StringAssert.Contains("DMG +0%", game.GrowthStatsTextForTest);
            StringAssert.Contains("CRIT 8%", game.GrowthStatsTextForTest);
            game.CastLineForTest();
            yield return null;
            game.CastCircleForTest();
            yield return null;

            Assert.AreEqual(1, game.CurrentLevelNumber);
            Assert.AreEqual(30, game.TotalOpenLevels);

            game.JumpToLevelForTest(5);
            yield return null;
            Assert.AreEqual("brokenMoonToyKing", game.CurrentBossId);
            Assert.AreEqual(376.96f, game.CurrentBossHp, 0.01f);

            game.JumpToLevelForTest(10);
            yield return null;
            Assert.AreEqual("brokenMoonToyKing", game.CurrentBossId);
            Assert.AreEqual(608f, game.CurrentBossHp, 0.01f);

            Object.Destroy(game.gameObject);
        }
