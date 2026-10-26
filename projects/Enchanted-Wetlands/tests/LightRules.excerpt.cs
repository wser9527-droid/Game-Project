// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/Tests/EditMode/LumenGlassEditModeTests.cs
// Dependencies: Original EnchantedWetlandsEditModeTests fixture, UnityEngine, NUnit, BeamRenderer, LightPuzzleModel, LevelDatabase and Resources/Data/levels.json.

// Source lines 95-163
    [Test]
    public void ChannelLightFollowsCornerAndTeeOpenings()
    {
        var masks = new Dictionary<string, int>
        {
            [LightPuzzleModel.Key(1, 1)] = LightPuzzleModel.Red | LightPuzzleModel.Blue,
            [LightPuzzleModel.Key(1, 0)] = LightPuzzleModel.Blue,
            [LightPuzzleModel.Key(2, 1)] = LightPuzzleModel.Red,
            [LightPuzzleModel.Key(1, 2)] = LightPuzzleModel.Red
        };
        var result = new LightSimulationResult(masks, new List<BeamSegmentData>
        {
            new BeamSegmentData(new Vector2(1, 1), new Vector2(1, 0), LightPuzzleModel.Blue),
            new BeamSegmentData(new Vector2(1, 1), new Vector2(2, 1), LightPuzzleModel.Red),
            new BeamSegmentData(new Vector2(1, 1), new Vector2(1, 2), LightPuzzleModel.Red)
        });

        List<BeamSegmentData> channels = BeamRenderer.BuildChannelSegments(result);
        Assert.That(channels, Has.Count.EqualTo(9));
        Assert.That(channels[0].Start, Is.EqualTo(new Vector2(1, 1)));
        Assert.That(channels[0].End.y, Is.EqualTo(0.56f).Within(0.001f));
        Assert.That(channels[3].End.x, Is.EqualTo(1.44f).Within(0.001f));
        Assert.That(channels[6].End.y, Is.EqualTo(1.44f).Within(0.001f));
        foreach (BeamSegmentData channel in channels)
        {
            Assert.That(channel.Start.x == channel.End.x || channel.Start.y == channel.End.y, Is.True,
                "Every lit piece must stay on a tile's horizontal or vertical water groove.");
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DisconnectedOrFilteredSourceDoesNotDrawLightIntoAnOtherwiseLitStone(bool filtered)
    {
        var level = new LevelData
        {
            width = 2,
            height = 1,
            tiles = new List<TileData>
            {
                new TileData { x = 0, y = 0, type = filtered ? "straight" : "end", rotation = 1, filterColor = filtered ? "blue" : null },
                new TileData { x = 1, y = 0, type = "straight", rotation = 1 }
            },
            sources = new List<LightSourceData>
            {
                new LightSourceData { x = 0, y = 0, color = "red", direction = "right" },
                new LightSourceData { x = 1, y = 0, color = "blue", direction = "left" }
            }
        };
        LightSimulationResult result = LightPuzzleModel.Simulate(level, tile => tile.rotation);
        Assert.That(result.CellMask(0, 0), Is.EqualTo(LightPuzzleModel.Blue));
        Assert.That(result.Beams, Has.None.Matches<BeamSegmentData>(beam => beam.Start == new Vector2(-0.58f, 0f)),
            "A disconnected or filtered-out red source must not draw a path into this blue-lit stone.");
        Assert.That(result.Beams, Has.All.Matches<BeamSegmentData>(beam => beam.ColorMask != 0));
    }

    [Test]
    public void SourceLightEndsAtTileOpening()
    {
        var result = new LightSimulationResult(
            new Dictionary<string, int> { [LightPuzzleModel.Key(0, 0)] = LightPuzzleModel.Blue },
            new List<BeamSegmentData> { new BeamSegmentData(new Vector2(-0.58f, 0), Vector2.zero, LightPuzzleModel.Blue) });

        List<BeamSegmentData> channels = BeamRenderer.BuildChannelSegments(result);
        Assert.That(channels, Has.Count.EqualTo(2));
        Assert.That(channels[0].Start, Is.EqualTo(Vector2.zero));
        Assert.That(channels[0].End.x, Is.EqualTo(-0.44f).Within(0.001f));
        Assert.That(channels[1].End, Is.EqualTo(new Vector2(-0.58f, 0)));
    }

// Source lines 448-459
    [Test]
    public void EveryLevelStartsUnsolvedButSolutionRotationsSolveIt()
    {
        LevelDatabase database = LevelDatabase.Load();
        foreach (LevelData level in database.levels)
        {
            LightSimulationResult initial = LightPuzzleModel.Simulate(level, tile => tile.rotation);
            LightSimulationResult solved = LightPuzzleModel.Simulate(level, tile => tile.solutionRotation);
            Assert.That(LightPuzzleModel.IsSolved(level, initial), Is.False, "Level " + level.id + " should not start solved.");
            Assert.That(LightPuzzleModel.IsSolved(level, solved), Is.True, "Level " + level.id + " solution should light every target.");
        }
    }

// Source lines 590-602
    [Test]
    public void SolutionDoesNotRouteDistractorColorIntoTarget()
    {
        LevelDatabase database = LevelDatabase.Load();
        foreach (LevelData level in database.levels)
        {
            LightSimulationResult solved = LightPuzzleModel.Simulate(level, tile => tile.solutionRotation);
            TargetData target = level.targets[0];
            int actual = solved.CellMask(target.x, target.y);
            int required = LightPuzzleModel.ColorMask(target.color);
            Assert.That(actual, Is.EqualTo(required), "Level " + level.id + " should not send the third color into the target.");
        }
    }
