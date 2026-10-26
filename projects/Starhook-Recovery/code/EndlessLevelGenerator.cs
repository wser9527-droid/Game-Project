using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarhookRecovery
{
    [Serializable]
    public sealed class EndlessSettings
    {
        public int minWidth = 5;
        public int minHeight = 5;
        public int maxWidth = 8;
        public int maxHeight = 5;
        public int maxBoxes = 3;
        public int maxWalls = 10;
        public int minReverseMoves = 4;
        public int maxReverseMoves = 18;
        public int generationRetries = 80;
        public float forecastSpinDuration = 1.4f;
        public float clearAdvanceDelay = 1f;
        public int baseSeed = 72931;
    }

    public struct GeneratedLevelResult
    {
        public LevelData level;
        public int seed;
        public int difficulty;
        public bool generatedByFallback;
        public SlotForecastResult forecast;
        public int minReachableAnchorY;
        public bool bluntAnchor;
    }

    public static class EndlessLevelGenerator
    {
        private static readonly SeaRule[] SeaRules = { SeaRule.Shallows, SeaRule.Reef, SeaRule.OpenSea };
        private static readonly CargoRule[] CargoRules = { CargoRule.LightBox, CargoRule.CrateCluster, CargoRule.TwinDocks };
        private static readonly AnchorRule[] AnchorRules = { AnchorRule.LongRope, AnchorRule.ShortRope, AnchorRule.BluntAnchor };
        private static readonly GridPoint[] Directions =
        {
            new GridPoint(1, 0),
            new GridPoint(-1, 0),
            new GridPoint(0, 1),
            new GridPoint(0, -1)
        };

        public static SlotForecastResult DrawForecast(int endlessLevel, int salt, EndlessSettings settings)
        {
            settings = NormalizeSettings(settings);

            int difficulty = Mathf.Max(1, endlessLevel);
            int seed = settings.baseSeed + difficulty * 7919 + salt * 104729;
            System.Random rng = new System.Random(seed ^ 0x2a53);
            return new SlotForecastResult
            {
                sea = SeaRules[Mathf.Abs(difficulty - 1 + salt) % SeaRules.Length],
                cargo = CargoRules[Mathf.Abs(difficulty * 2 + salt) % CargoRules.Length],
                anchor = AnchorRules[rng.Next(0, AnchorRules.Length)],
                seed = seed
            };
        }

        public static GeneratedLevelResult Generate(int endlessLevel, int salt, EndlessSettings settings)
        {
            SlotForecastResult forecast = DrawForecast(endlessLevel, salt, settings);
            return Generate(new LevelGenerationRequest { endlessLevel = endlessLevel, salt = salt, forecast = forecast }, settings);
        }

        public static GeneratedLevelResult Generate(LevelGenerationRequest request, EndlessSettings settings)
        {
            settings = NormalizeSettings(settings);

            int difficulty = Mathf.Max(1, request.endlessLevel);
            int seedBase = request.forecast.seed;
            for (int attempt = 0; attempt < Mathf.Max(1, settings.generationRetries); attempt++)
            {
                int seed = seedBase + attempt * 997;
                System.Random rng = new System.Random(seed);
                if (TryGenerate(difficulty, seed, rng, settings, request.forecast, false, attempt, out GeneratedLevelResult result))
                {
                    return result;
                }
            }

            System.Random fallbackRng = new System.Random(seedBase ^ 0x51f0a7);
            if (TryGenerate(difficulty, seedBase, fallbackRng, settings, request.forecast, true, settings.generationRetries, out GeneratedLevelResult fallback))
            {
                return fallback;
            }

            return BuildSafeFallbackLevel(difficulty, seedBase, settings, request.forecast);
        }

        public static EndlessSettings NormalizeSettings(EndlessSettings settings)
        {
            EndlessSettings source = settings ?? new EndlessSettings();
            EndlessSettings normalized = new EndlessSettings
            {
                minWidth = Mathf.Clamp(source.minWidth, 5, 8),
                minHeight = Mathf.Clamp(Mathf.Max(source.minHeight, 5), 5, 5),
                maxWidth = Mathf.Clamp(source.maxWidth, 5, 8),
                maxHeight = Mathf.Clamp(Mathf.Max(source.maxHeight, 5), 5, 5),
                maxBoxes = Mathf.Clamp(source.maxBoxes, 2, 3),
                maxWalls = Mathf.Clamp(source.maxWalls, 0, 10),
                minReverseMoves = Mathf.Clamp(source.minReverseMoves, 1, 18),
                maxReverseMoves = Mathf.Clamp(source.maxReverseMoves, 1, 18),
                generationRetries = Mathf.Max(1, source.generationRetries),
                forecastSpinDuration = Mathf.Max(0.1f, source.forecastSpinDuration),
                clearAdvanceDelay = Mathf.Max(0.1f, source.clearAdvanceDelay),
                baseSeed = source.baseSeed
            };

            if (normalized.maxWidth < normalized.minWidth)
            {
                normalized.maxWidth = normalized.minWidth;
            }

            if (normalized.maxHeight < normalized.minHeight)
            {
                normalized.maxHeight = normalized.minHeight;
            }

            if (normalized.maxReverseMoves < normalized.minReverseMoves)
            {
                normalized.maxReverseMoves = normalized.minReverseMoves;
            }

            return normalized;
        }

        public static bool ValidateGeneratedLevel(LevelData level, SlotForecastResult forecast, out string issue)
        {
            if (!ValidateGeneratedLevel(level, out issue))
            {
                return false;
            }

            if (forecast.anchor == AnchorRule.ShortRope)
            {
                foreach (GridPoint point in level.boxes)
                {
                    if (point.y < level.minReachableAnchorY)
                    {
                        issue = "short tether cargo outside reachable depth";
                        return false;
                    }
                }

                foreach (GridPoint point in level.targets)
                {
                    if (point.y < level.minReachableAnchorY)
                    {
                        issue = "short tether salvage interface outside reachable depth";
                        return false;
                    }
                }
            }

            if (forecast.cargo == CargoRule.CrateCluster && level.boxes.Length < 2)
            {
                issue = "cargo cluster should generate at least two cargo containers";
                return false;
            }

            if (forecast.cargo == CargoRule.TwinDocks && level.dockTiles.Length == 0)
            {
                issue = "docking bay should provide station platform visual tiles";
                return false;
            }

            if (forecast.sea == SeaRule.Shallows && level.shallowTiles.Length == 0)
            {
                issue = "calm orbit should generate nebula turbulence visual tiles";
                return false;
            }

            if (forecast.sea == SeaRule.Reef && level.walls.Length == 0)
            {
                issue = "debris field should generate blocking debris tiles";
                return false;
            }

            return true;
        }

        public static bool ValidateGeneratedLevel(LevelData level, out string issue)
        {
            issue = string.Empty;
            if (level == null)
            {
                issue = "sector is null";
                return false;
            }

            if (level.width < 5 || level.width > 8 || level.height < 5 || level.height > 5)
            {
                issue = "sector grid size outside endless bounds";
                return false;
            }

            if (level.boxes == null || level.targets == null || level.walls == null || level.shallowTiles == null || level.dockTiles == null)
            {
                issue = "point arrays must not be null";
                return false;
            }

            if (level.boxes.Length < 1 || level.boxes.Length > 3 || level.targets.Length != level.boxes.Length)
            {
                issue = "cargo/interface count outside endless bounds";
                return false;
            }

            HashSet<GridPoint> occupied = new HashSet<GridPoint>();
            if (!AddAll("debris", level.walls, level.width, level.height, occupied, out issue))
            {
                return false;
            }

            if (!AddAll("cargo", level.boxes, level.width, level.height, occupied, out issue))
            {
                return false;
            }

            if (!AddAll("salvage interface", level.targets, level.width, level.height, occupied, out issue))
            {
                return false;
            }

            if (!ValidateVisualTiles("nebula turbulence", level.shallowTiles, level.width, level.height, occupied, out issue))
            {
                return false;
            }

            if (!ValidateVisualTiles("station platform", level.dockTiles, level.width, level.height, occupied, out issue))
            {
                return false;
            }

            if (level.shipColumn < 0 || level.shipColumn >= level.width)
            {
                issue = "shipColumn outside board";
                return false;
            }

            foreach (GridPoint box in level.boxes)
            {
                if (box.x == level.shipColumn)
                {
                    issue = "cargo overlaps starting grab column";
                    return false;
                }
            }

            bool alreadySolved = true;
            HashSet<GridPoint> targetSet = new HashSet<GridPoint>(level.targets);
            foreach (GridPoint box in level.boxes)
            {
                if (!targetSet.Contains(box))
                {
                    alreadySolved = false;
                    break;
                }
            }

            if (alreadySolved)
            {
                issue = "generated sector starts solved";
                return false;
            }

            return true;
        }

        private static bool TryGenerate(int difficulty, int seed, System.Random rng, EndlessSettings settings, SlotForecastResult forecast, bool fallback, int attempt, out GeneratedLevelResult result)
        {
            GenerationModifiers modifiers = GenerationModifiers.From(forecast);
            int relief = fallback ? 3 : attempt / 16;
            int width = Mathf.Clamp(settings.minWidth + (difficulty - 1) / 8 + modifiers.widthBonus, settings.minWidth, settings.maxWidth);
            int height = Mathf.Clamp(settings.minHeight + (difficulty - 1) / 12 + modifiers.heightBonus, settings.minHeight, settings.maxHeight);
            int minReachableY = forecast.anchor == AnchorRule.ShortRope ? Mathf.Clamp(height - 3, 0, height - 1) : 0;

            int baseBoxCount = 1 + (difficulty >= 8 ? 1 : 0) + (difficulty >= 22 ? 1 : 0);
            int boxCount = Mathf.Clamp(baseBoxCount + modifiers.boxDelta, 1, settings.maxBoxes);
            if (forecast.cargo == CargoRule.CrateCluster)
            {
                boxCount = Mathf.Max(2, boxCount);
            }
            else if (forecast.cargo == CargoRule.LightBox)
            {
                boxCount = Mathf.Min(2, boxCount);
            }

            if (difficulty >= 8)
            {
                boxCount = Mathf.Max(2, boxCount);
            }

            int baseWallCount = fallback ? Mathf.Max(0, difficulty / 8) : (difficulty - 1) / 3;
            int wallCount = Mathf.Clamp(baseWallCount + modifiers.wallDelta - relief, 0, settings.maxWalls);
            if (forecast.sea == SeaRule.Reef)
            {
                wallCount = Mathf.Max(1, wallCount);
            }

            int reverseMoves = Mathf.Clamp(settings.minReverseMoves + difficulty / 2 + modifiers.reverseMoveDelta - relief, settings.minReverseMoves, settings.maxReverseMoves);
            List<GridPoint> walls = new List<GridPoint>();
            HashSet<GridPoint> occupied = new HashSet<GridPoint>();
            int playableCells = width * (height - minReachableY);
            int wallBudget = Mathf.Min(wallCount, playableCells - boxCount * 2 - 2);
            for (int i = 0; i < wallBudget; i++)
            {
                if (TryFindEmpty(width, height, minReachableY, occupied, rng, out GridPoint wall))
                {
                    walls.Add(wall);
                    occupied.Add(wall);
                }
            }

            List<GridPoint> targets = new List<GridPoint>();
            int targetCount = boxCount;
            for (int i = 0; i < targetCount; i++)
            {
                if (!TryFindEmpty(width, height, minReachableY, occupied, rng, out GridPoint target))
                {
                    result = default;
                    return false;
                }

                targets.Add(target);
                occupied.Add(target);
            }

            List<GridPoint> boxes = new List<GridPoint>();
            for (int i = 0; i < boxCount; i++)
            {
                boxes.Add(targets[i]);
            }

            for (int step = 0; step < reverseMoves; step++)
            {
                if (!TryReverseMoveBox(width, height, minReachableY, walls, boxes, rng))
                {
                    if (!fallback)
                    {
                        result = default;
                        return false;
                    }

                    break;
                }
            }

            GridPoint[] dockTiles = BuildDockTiles(width, height, minReachableY, walls, boxes, targets, forecast, rng);
            GridPoint[] shallowTiles = BuildShallowTiles(width, height, minReachableY, walls, boxes, targets, dockTiles, modifiers.shallowTileCount, rng);
            LevelData level = new LevelData
            {
                id = "endless_" + difficulty.ToString("000"),
                title = "Sector " + difficulty,
                width = width,
                height = height,
                shipColumn = ChooseShipColumnAwayFromBoxes(width, boxes),
                walls = walls.ToArray(),
                boxes = boxes.ToArray(),
                targets = targets.ToArray(),
                shallowTiles = shallowTiles,
                dockTiles = dockTiles,
                minReachableAnchorY = minReachableY
            };

            if (!ValidateGeneratedLevel(level, forecast, out _))
            {
                result = default;
                return false;
            }

            if (!ValidateChallengeFloor(level, difficulty))
            {
                result = default;
                return false;
            }

            result = new GeneratedLevelResult
            {
                level = level,
                seed = seed,
                difficulty = difficulty,
                generatedByFallback = fallback,
                forecast = forecast,
                minReachableAnchorY = minReachableY,
                bluntAnchor = forecast.anchor == AnchorRule.BluntAnchor
            };
            return true;
        }

        private static GeneratedLevelResult BuildSafeFallbackLevel(int difficulty, int seed, EndlessSettings settings, SlotForecastResult forecast)
        {
            int width = Mathf.Clamp(settings.minWidth, 5, 8);
            int height = Mathf.Clamp(settings.minHeight, 5, 5);
            int minReachableY = forecast.anchor == AnchorRule.ShortRope ? Mathf.Clamp(height - 3, 0, height - 1) : 0;
            int boxCount = difficulty >= 8 || forecast.cargo == CargoRule.CrateCluster ? 2 : 1;
            int row = Mathf.Clamp(height - 1, minReachableY, height - 1);

            GridPoint[] boxes = boxCount == 2
                ? new[] { new GridPoint(1, row), new GridPoint(3, row) }
                : new[] { new GridPoint(1, row) };
            GridPoint[] targets = boxCount == 2
                ? new[] { new GridPoint(1, minReachableY), new GridPoint(3, minReachableY) }
                : new[] { new GridPoint(3, minReachableY) };

            LevelData level = new LevelData
            {
                id = "endless_" + difficulty.ToString("000") + "_safe",
                title = "Sector " + difficulty,
                width = width,
                height = height,
                shipColumn = ChooseShipColumnAwayFromBoxes(width, new List<GridPoint>(boxes)),
                walls = forecast.sea == SeaRule.Reef ? new[] { new GridPoint(width - 1, height - 1) } : Array.Empty<GridPoint>(),
                boxes = boxes,
                targets = targets,
                shallowTiles = forecast.sea == SeaRule.Shallows ? new[] { new GridPoint(width - 1, minReachableY) } : Array.Empty<GridPoint>(),
                dockTiles = forecast.cargo == CargoRule.TwinDocks ? new[] { new GridPoint(width - 1, row) } : Array.Empty<GridPoint>(),
                minReachableAnchorY = minReachableY
            };

            return new GeneratedLevelResult
            {
                level = level,
                seed = seed,
                difficulty = difficulty,
                generatedByFallback = true,
                forecast = forecast,
                minReachableAnchorY = minReachableY,
                bluntAnchor = forecast.anchor == AnchorRule.BluntAnchor
            };
        }

        private static bool ValidateChallengeFloor(LevelData level, int difficulty)
        {
            if (difficulty >= 8 && level.boxes.Length < 2)
            {
                return false;
            }

            bool hasExtraElement = level.walls.Length > 0 || level.shallowTiles.Length > 0 || level.dockTiles.Length > 0;
            if (difficulty >= 6 && level.boxes.Length == 1 && !hasExtraElement)
            {
                return false;
            }

            if (difficulty >= 8 && TotalBoxTargetDistance(level) < 4)
            {
                return false;
            }

            return true;
        }

        private static int TotalBoxTargetDistance(LevelData level)
        {
            int total = 0;
            for (int i = 0; i < level.boxes.Length; i++)
            {
                int nearest = int.MaxValue;
                for (int j = 0; j < level.targets.Length; j++)
                {
                    int distance = Mathf.Abs(level.boxes[i].x - level.targets[j].x) + Mathf.Abs(level.boxes[i].y - level.targets[j].y);
                    nearest = Mathf.Min(nearest, distance);
                }

                if (nearest < int.MaxValue)
                {
                    total += nearest;
                }
            }

            return total;
        }

        private static GridPoint[] BuildDockTiles(int width, int height, int minReachableY, List<GridPoint> walls, List<GridPoint> boxes, List<GridPoint> targets, SlotForecastResult forecast, System.Random rng)
        {
            if (forecast.cargo != CargoRule.TwinDocks)
            {
                return Array.Empty<GridPoint>();
            }

            HashSet<GridPoint> occupied = new HashSet<GridPoint>();
            foreach (GridPoint point in walls) occupied.Add(point);
            foreach (GridPoint point in boxes) occupied.Add(point);
            foreach (GridPoint point in targets) occupied.Add(point);

            List<GridPoint> docks = new List<GridPoint>();
            int dockCount = Mathf.Min(2, Mathf.Max(1, width * (height - minReachableY) - occupied.Count));
            for (int i = 0; i < dockCount; i++)
            {
                if (TryFindEmpty(width, height, minReachableY, occupied, rng, out GridPoint dock))
                {
                    docks.Add(dock);
                    occupied.Add(dock);
                }
            }

            return docks.ToArray();
        }

        private static GridPoint[] BuildShallowTiles(int width, int height, int minReachableY, List<GridPoint> walls, List<GridPoint> boxes, List<GridPoint> targets, GridPoint[] dockTiles, int requestedCount, System.Random rng)
        {
            int count = Mathf.Clamp(requestedCount, 0, Mathf.Max(0, width * (height - minReachableY) - walls.Count - boxes.Count));
            List<GridPoint> shallows = new List<GridPoint>();
            HashSet<GridPoint> occupied = new HashSet<GridPoint>();
            foreach (GridPoint point in walls) occupied.Add(point);
            foreach (GridPoint point in boxes) occupied.Add(point);
            foreach (GridPoint point in targets) occupied.Add(point);
            foreach (GridPoint point in dockTiles) occupied.Add(point);

            for (int i = 0; i < count; i++)
            {
                if (TryFindEmpty(width, height, minReachableY, occupied, rng, out GridPoint shallow))
                {
                    shallows.Add(shallow);
                    occupied.Add(shallow);
                }
            }

            return shallows.ToArray();
        }

        private static bool TryReverseMoveBox(int width, int height, int minReachableY, List<GridPoint> walls, List<GridPoint> boxes, System.Random rng)
        {
            foreach (int boxIndex in ShuffledIndexes(boxes.Count, rng))
            {
                foreach (GridPoint direction in ShuffledDirections(rng))
                {
                    GridPoint current = boxes[boxIndex];
                    GridPoint next = new GridPoint(current.x + direction.x, current.y + direction.y);
                    if (!Inside(next, width, height) || next.y < minReachableY || Contains(walls, next) || ContainsBox(boxes, next, boxIndex))
                    {
                        continue;
                    }

                    boxes[boxIndex] = next;
                    return true;
                }
            }

            return false;
        }

        private static int ChooseShipColumnAwayFromBoxes(int width, List<GridPoint> boxes)
        {
            bool[] boxColumns = new bool[Mathf.Max(0, width)];
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].x >= 0 && boxes[i].x < width)
                {
                    boxColumns[boxes[i].x] = true;
                }
            }

            int center = width / 2;
            for (int offset = 0; offset < width; offset++)
            {
                int right = center + offset;
                if (right < width && !boxColumns[right])
                {
                    return right;
                }

                int left = center - offset;
                if (left >= 0 && !boxColumns[left])
                {
                    return left;
                }
            }

            return Mathf.Clamp(center, 0, Mathf.Max(0, width - 1));
        }

        private static bool TryFindEmpty(int width, int height, int minReachableY, HashSet<GridPoint> occupied, System.Random rng, out GridPoint point)
        {
            int attempts = Mathf.Max(20, width * height * 4);
            for (int i = 0; i < attempts; i++)
            {
                point = new GridPoint(rng.Next(0, width), rng.Next(minReachableY, height));
                if (!occupied.Contains(point))
                {
                    return true;
                }
            }

            for (int y = minReachableY; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    point = new GridPoint(x, y);
                    if (!occupied.Contains(point))
                    {
                        return true;
                    }
                }
            }

            point = default;
            return false;
        }

        private static bool AddAll(string label, GridPoint[] points, int width, int height, HashSet<GridPoint> occupied, out string issue)
        {
            foreach (GridPoint point in points)
            {
                if (!Inside(point, width, height))
                {
                    issue = label + " outside board";
                    return false;
                }

                if (!occupied.Add(point))
                {
                    issue = label + " overlaps occupied point";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool ValidateVisualTiles(string label, GridPoint[] points, int width, int height, HashSet<GridPoint> occupied, out string issue)
        {
            HashSet<GridPoint> visualTiles = new HashSet<GridPoint>();
            foreach (GridPoint point in points)
            {
                if (!Inside(point, width, height))
                {
                    issue = label + " outside board";
                    return false;
                }

                if (occupied.Contains(point))
                {
                    issue = label + " overlaps blocking point";
                    return false;
                }

                if (!visualTiles.Add(point))
                {
                    issue = label + " duplicates visual tile";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool Inside(GridPoint point, int width, int height)
        {
            return point.x >= 0 && point.x < width && point.y >= 0 && point.y < height;
        }

        private static bool Contains(List<GridPoint> points, GridPoint point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == point)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsBox(List<GridPoint> boxes, GridPoint point, int ignoredIndex)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (i != ignoredIndex && boxes[i] == point)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int> ShuffledIndexes(int count, System.Random rng)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < count; i++)
            {
                indexes.Add(i);
            }

            for (int i = indexes.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                int temp = indexes[i];
                indexes[i] = indexes[j];
                indexes[j] = temp;
            }

            return indexes;
        }

        private static List<GridPoint> ShuffledDirections(System.Random rng)
        {
            List<GridPoint> directions = new List<GridPoint>(Directions);
            for (int i = directions.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                GridPoint temp = directions[i];
                directions[i] = directions[j];
                directions[j] = temp;
            }

            return directions;
        }

        private struct GenerationModifiers
        {
            public int widthBonus;
            public int heightBonus;
            public int boxDelta;
            public int wallDelta;
            public int reverseMoveDelta;
            public int shallowTileCount;

            public static GenerationModifiers From(SlotForecastResult forecast)
            {
                GenerationModifiers modifiers = new GenerationModifiers();
                switch (forecast.sea)
                {
                    case SeaRule.Shallows:
                        modifiers.wallDelta = -2;
                        modifiers.shallowTileCount = 6;
                        modifiers.reverseMoveDelta = -1;
                        break;
                    case SeaRule.Reef:
                        modifiers.wallDelta = 3;
                        modifiers.reverseMoveDelta = 1;
                        break;
                    case SeaRule.OpenSea:
                        modifiers.widthBonus = 1;
                        modifiers.heightBonus = 1;
                        modifiers.wallDelta = -1;
                        modifiers.reverseMoveDelta = 2;
                        break;
                }

                switch (forecast.cargo)
                {
                    case CargoRule.LightBox:
                        modifiers.boxDelta -= 1;
                        modifiers.reverseMoveDelta -= 1;
                        break;
                    case CargoRule.CrateCluster:
                        modifiers.boxDelta += 1;
                        modifiers.reverseMoveDelta += 2;
                        break;
                    case CargoRule.TwinDocks:
                        modifiers.reverseMoveDelta -= 1;
                        break;
                }

                switch (forecast.anchor)
                {
                    case AnchorRule.LongRope:
                        modifiers.heightBonus += 1;
                        modifiers.reverseMoveDelta += 2;
                        break;
                    case AnchorRule.ShortRope:
                        modifiers.heightBonus += 1;
                        modifiers.wallDelta -= 1;
                        break;
                    case AnchorRule.BluntAnchor:
                        modifiers.wallDelta -= 1;
                        modifiers.reverseMoveDelta -= 1;
                        break;
                }

                return modifiers;
            }
        }
    }
}
