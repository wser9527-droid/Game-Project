using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackoutGrid
{
    public static class LightPuzzleModel
    {
        public const int Red = 1;
        public const int Blue = 2;
        public const int Yellow = 4;
        public const int White = Red | Blue | Yellow;

        private static readonly Vector2Int[] GridOffsets =
        {
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0)
        };

        public static LightSimulationResult Simulate(LevelData level, Func<TileData, int> rotationProvider)
        {
            return SimulateSources(level, level.sources, rotationProvider);
        }

        public static LightSimulationResult SimulateSource(LevelData level, LightSourceData source, Func<TileData, int> rotationProvider)
        {
            return SimulateSources(level, new[] { source }, rotationProvider);
        }

        private static LightSimulationResult SimulateSources(LevelData level, IEnumerable<LightSourceData> sources, Func<TileData, int> rotationProvider)
        {
            Dictionary<string, TileData> tiles = BuildTileMap(level);
            Dictionary<string, int> cellMasks = new Dictionary<string, int>();

            foreach (LightSourceData source in sources)
            {
                string key = Key(source.x, source.y);
                if (!tiles.TryGetValue(key, out TileData tile))
                {
                    continue;
                }

                int direction = DirectionIndex(source.direction);
                int ports = PortsFor(tile, rotationProvider(tile));
                if ((ports & DirectionBit(Opposite(direction))) == 0)
                {
                    continue;
                }

                int mask = ApplyFilter(tile, ColorMask(source.color));
                if (mask != 0)
                {
                    AddMask(cellMasks, key, mask);
                }
            }

            bool changed = true;
            int guard = Mathf.Max(8, level.width * level.height * 8);
            while (changed && guard-- > 0)
            {
                changed = false;
                Dictionary<string, int> snapshot = new Dictionary<string, int>(cellMasks);
                foreach (KeyValuePair<string, int> pair in snapshot)
                {
                    if (!tiles.TryGetValue(pair.Key, out TileData tile))
                    {
                        continue;
                    }

                    int ports = PortsFor(tile, rotationProvider(tile));
                    for (int direction = 0; direction < 4; direction++)
                    {
                        if ((ports & DirectionBit(direction)) == 0)
                        {
                            continue;
                        }

                        int nx = tile.x + GridOffsets[direction].x;
                        int ny = tile.y + GridOffsets[direction].y;
                        if (!IsInBounds(level, nx, ny))
                        {
                            continue;
                        }

                        string neighborKey = Key(nx, ny);
                        if (!tiles.TryGetValue(neighborKey, out TileData neighbor))
                        {
                            continue;
                        }

                        int neighborPorts = PortsFor(neighbor, rotationProvider(neighbor));
                        if ((neighborPorts & DirectionBit(Opposite(direction))) == 0)
                        {
                            continue;
                        }

                        int incoming = ApplyFilter(neighbor, pair.Value);
                        if (incoming != 0 && AddMask(cellMasks, neighborKey, incoming))
                        {
                            changed = true;
                        }
                    }
                }
            }

            return new LightSimulationResult(cellMasks, BuildBeamSegments(level, sources, tiles, cellMasks, rotationProvider));
        }

        public static bool IsSolved(LevelData level, LightSimulationResult result)
        {
            if (level.targets.Count == 0)
            {
                return false;
            }

            foreach (TargetData target in level.targets)
            {
                if (!IsTargetLit(target, result))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsTargetLit(TargetData target, LightSimulationResult result)
        {
            int actual = result.CellMask(target.x, target.y);
            int required = ColorMask(target.color);
            return required != 0 && actual == required;
        }

        public static int ColorMask(string color)
        {
            switch ((color ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "red":
                    return Red;
                case "blue":
                    return Blue;
                case "yellow":
                    return Yellow;
                case "purple":
                    return Red | Blue;
                case "green":
                    return Blue | Yellow;
                case "orange":
                    return Red | Yellow;
                case "white":
                    return White;
                default:
                    return 0;
            }
        }

        public static Color UnityColor(int mask, float alpha = 1f)
        {
            switch (mask)
            {
                case Red:
                    return new Color(1f, 0.18f, 0.36f, alpha);
                case Blue:
                    return new Color(0.18f, 0.62f, 1f, alpha);
                case Yellow:
                    return new Color(1f, 0.84f, 0.18f, alpha);
                case Red | Blue:
                    return new Color(0.72f, 0.28f, 1f, alpha);
                case Blue | Yellow:
                    return new Color(0.28f, 1f, 0.58f, alpha);
                case Red | Yellow:
                    return new Color(1f, 0.52f, 0.16f, alpha);
                case White:
                    return new Color(1f, 0.96f, 0.82f, alpha);
                default:
                    return new Color(0.22f, 0.28f, 0.42f, alpha);
            }
        }

        public static string ColorName(int mask)
        {
            switch (mask)
            {
                case Red:
                    return "Red";
                case Blue:
                    return "Blue";
                case Yellow:
                    return "Yellow";
                case Red | Blue:
                    return "Purple";
                case Blue | Yellow:
                    return "Green";
                case Red | Yellow:
                    return "Orange";
                case White:
                    return "White";
                default:
                    return "Dark";
            }
        }

        public static int PortsFor(TileData tile, int rotation)
        {
            int basePorts;
            switch ((tile.type ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "end":
                    basePorts = DirectionBit(0);
                    break;
                case "straight":
                    basePorts = DirectionBit(0) | DirectionBit(2);
                    break;
                case "corner":
                    basePorts = DirectionBit(0) | DirectionBit(1);
                    break;
                case "tee":
                    basePorts = DirectionBit(3) | DirectionBit(0) | DirectionBit(1);
                    break;
                case "cross":
                    basePorts = DirectionBit(0) | DirectionBit(1) | DirectionBit(2) | DirectionBit(3);
                    break;
                default:
                    basePorts = 0;
                    break;
            }

            int result = 0;
            int normalized = ((rotation % 4) + 4) % 4;
            for (int direction = 0; direction < 4; direction++)
            {
                if ((basePorts & DirectionBit(direction)) != 0)
                {
                    result |= DirectionBit((direction + normalized) % 4);
                }
            }

            return result;
        }

        public static int DirectionIndex(string direction)
        {
            switch ((direction ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "up":
                    return 0;
                case "right":
                    return 1;
                case "down":
                    return 2;
                case "left":
                    return 3;
                default:
                    return 1;
            }
        }

        public static bool IsInBounds(LevelData level, int x, int y)
        {
            return x >= 0 && y >= 0 && x < level.width && y < level.height;
        }

        public static string Key(int x, int y)
        {
            return x + ":" + y;
        }

        public static int DirectionBit(int direction)
        {
            return 1 << (((direction % 4) + 4) % 4);
        }

        public static int Opposite(int direction)
        {
            return (direction + 2) % 4;
        }

        private static Dictionary<string, TileData> BuildTileMap(LevelData level)
        {
            Dictionary<string, TileData> tiles = new Dictionary<string, TileData>();
            foreach (TileData tile in level.tiles)
            {
                tiles[Key(tile.x, tile.y)] = tile;
            }

            return tiles;
        }

        private static int ApplyFilter(TileData tile, int mask)
        {
            int filter = ColorMask(tile.filterColor);
            return filter == 0 ? mask : mask & filter;
        }

        private static bool AddMask(Dictionary<string, int> cellMasks, string key, int mask)
        {
            cellMasks.TryGetValue(key, out int existing);
            int combined = existing | mask;
            if (combined == existing)
            {
                return false;
            }

            cellMasks[key] = combined;
            return true;
        }

        private static List<BeamSegmentData> BuildBeamSegments(LevelData level, IEnumerable<LightSourceData> sources, Dictionary<string, TileData> tiles, Dictionary<string, int> cellMasks, Func<TileData, int> rotationProvider)
        {
            List<BeamSegmentData> beams = new List<BeamSegmentData>();
            HashSet<string> segmentKeys = new HashSet<string>();

            foreach (LightSourceData source in sources)
            {
                string key = Key(source.x, source.y);
                if (!cellMasks.TryGetValue(key, out int mask) || !tiles.TryGetValue(key, out TileData tile))
                {
                    continue;
                }

                int direction = DirectionIndex(source.direction);
                if ((PortsFor(tile, rotationProvider(tile)) & DirectionBit(Opposite(direction))) == 0)
                {
                    continue;
                }
                int sourceMask = ApplyFilter(tile, ColorMask(source.color)) & mask;
                if (sourceMask == 0) continue;
                Vector2 outside = new Vector2(source.x - GridOffsets[direction].x * 0.58f, source.y - GridOffsets[direction].y * 0.58f);
                beams.Add(new BeamSegmentData(outside, new Vector2(source.x, source.y), sourceMask));
            }

            foreach (KeyValuePair<string, int> pair in cellMasks)
            {
                if (!tiles.TryGetValue(pair.Key, out TileData tile))
                {
                    continue;
                }

                int ports = PortsFor(tile, rotationProvider(tile));
                for (int direction = 0; direction < 4; direction++)
                {
                    if ((ports & DirectionBit(direction)) == 0)
                    {
                        continue;
                    }

                    int nx = tile.x + GridOffsets[direction].x;
                    int ny = tile.y + GridOffsets[direction].y;
                    if (!IsInBounds(level, nx, ny))
                    {
                        continue;
                    }

                    string neighborKey = Key(nx, ny);
                    if (!tiles.TryGetValue(neighborKey, out TileData neighbor))
                    {
                        continue;
                    }

                    int neighborPorts = PortsFor(neighbor, rotationProvider(neighbor));
                    if ((neighborPorts & DirectionBit(Opposite(direction))) == 0 || !cellMasks.TryGetValue(neighborKey, out int neighborMask))
                    {
                        continue;
                    }

                    string segmentKey = tile.x < nx || (tile.x == nx && tile.y < ny)
                        ? tile.x + ":" + tile.y + "-" + nx + ":" + ny
                        : nx + ":" + ny + "-" + tile.x + ":" + tile.y;
                    if (!segmentKeys.Add(segmentKey))
                    {
                        continue;
                    }

                    beams.Add(new BeamSegmentData(new Vector2(tile.x, tile.y), new Vector2(nx, ny), pair.Value | neighborMask));
                }
            }

            return beams;
        }
    }

    public sealed class LightSimulationResult
    {
        public readonly Dictionary<string, int> CellMasks;
        public readonly List<BeamSegmentData> Beams;

        public LightSimulationResult(Dictionary<string, int> cellMasks, List<BeamSegmentData> beams)
        {
            CellMasks = cellMasks;
            Beams = beams;
        }

        public int CellMask(int x, int y)
        {
            return CellMasks.TryGetValue(LightPuzzleModel.Key(x, y), out int mask) ? mask : 0;
        }
    }

    public sealed class BeamSegmentData
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly int ColorMask;

        public BeamSegmentData(Vector2 start, Vector2 end, int colorMask)
        {
            Start = start;
            End = end;
            ColorMask = colorMask;
        }
    }
}
