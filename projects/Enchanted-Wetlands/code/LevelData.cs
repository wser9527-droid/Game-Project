using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackoutGrid
{
    [Serializable]
    public sealed class LevelDatabase
    {
        public const int PlayableLevelCount = 100;
        public List<LevelData> levels = new List<LevelData>();

        public static bool IsPlayable(int levelId) => levelId >= 1 && levelId <= PlayableLevelCount;

        public static LevelDatabase Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("Data/levels");
            if (asset == null)
            {
                Debug.LogError("Enchanted Wetlands level database missing at Resources/Data/levels.json.");
                return new LevelDatabase();
            }

            LevelDatabase database = JsonUtility.FromJson<LevelDatabase>(asset.text);
            return database ?? new LevelDatabase();
        }
    }

    [Serializable]
    public sealed class LevelData
    {
        public int id;
        public string title;
        public string chapter;
        public int width;
        public int height;
        public int moveLimit;
        public bool hardMoveLimit;
        public List<TileData> tiles = new List<TileData>();
        public List<LightSourceData> sources = new List<LightSourceData>();
        public List<TargetData> targets = new List<TargetData>();
    }

    [Serializable]
    public sealed class TileData
    {
        public int x;
        public int y;
        public string type;
        public int rotation;
        public int solutionRotation;
        public bool locked;
        public string filterColor;
    }

    [Serializable]
    public sealed class LightSourceData
    {
        public int x;
        public int y;
        public string direction;
        public string color;
    }

    [Serializable]
    public sealed class TargetData
    {
        public string id;
        public int x;
        public int y;
        public string color;
        public string pattern;
    }
}
