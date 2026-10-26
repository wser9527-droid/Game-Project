using System;
using UnityEngine;

namespace StarhookRecovery
{
    public enum SeaRule
    {
        Shallows,
        Reef,
        OpenSea
    }

    public enum CargoRule
    {
        LightBox,
        CrateCluster,
        TwinDocks
    }

    public enum AnchorRule
    {
        LongRope,
        ShortRope,
        BluntAnchor
    }

    [Serializable]
    public struct SlotForecastResult
    {
        public SeaRule sea;
        public CargoRule cargo;
        public AnchorRule anchor;
        public int seed;

        public string DisplayText()
        {
            return DisplayName(sea) + " / " + DisplayName(cargo) + " / " + DisplayName(anchor);
        }

        public static string DisplayName(SeaRule rule)
        {
            switch (rule)
            {
                case SeaRule.Shallows:
                    return "Calm Orbit";
                case SeaRule.Reef:
                    return "Debris Field";
                case SeaRule.OpenSea:
                    return "Deep Space";
                default:
                    return rule.ToString();
            }
        }

        public static string DisplayName(CargoRule rule)
        {
            switch (rule)
            {
                case CargoRule.LightBox:
                    return "Light Cargo";
                case CargoRule.CrateCluster:
                    return "Cargo Cluster";
                case CargoRule.TwinDocks:
                    return "Docking Bay";
                default:
                    return rule.ToString();
            }
        }

        public static string DisplayName(AnchorRule rule)
        {
            switch (rule)
            {
                case AnchorRule.LongRope:
                    return "Long Tether";
                case AnchorRule.ShortRope:
                    return "Short Tether";
                case AnchorRule.BluntAnchor:
                    return "Static Hook";
                default:
                    return rule.ToString();
            }
        }
    }

    public struct LevelGenerationRequest
    {
        public int endlessLevel;
        public int salt;
        public SlotForecastResult forecast;
    }

    [Serializable]
    public struct GridPoint : IEquatable<GridPoint>
    {
        public int x;
        public int y;

        public GridPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(GridPoint other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static bool operator ==(GridPoint left, GridPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPoint left, GridPoint right)
        {
            return !left.Equals(right);
        }
    }

    [Serializable]
    public sealed class LevelData
    {
        public string id;
        public string title;
        public int width;
        public int height;
        public int shipColumn;
        public GridPoint[] walls = Array.Empty<GridPoint>();
        public GridPoint[] boxes = Array.Empty<GridPoint>();
        public GridPoint[] targets = Array.Empty<GridPoint>();
        public GridPoint[] shallowTiles = Array.Empty<GridPoint>();
        public GridPoint[] dockTiles = Array.Empty<GridPoint>();
        public int minReachableAnchorY;
    }

    [Serializable]
    public sealed class LevelSet
    {
        public LevelData[] levels = Array.Empty<LevelData>();
    }
}
