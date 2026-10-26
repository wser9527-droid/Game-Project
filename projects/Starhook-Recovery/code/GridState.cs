using System.Collections.Generic;

namespace StarhookRecovery
{
    public sealed class GridState
    {
        private readonly HashSet<GridPoint> walls = new HashSet<GridPoint>();
        private readonly HashSet<GridPoint> targets = new HashSet<GridPoint>();
        private readonly List<GridPoint> boxes = new List<GridPoint>();

        public int Width { get; private set; }
        public int Height { get; private set; }
        public IReadOnlyCollection<GridPoint> Walls => walls;
        public IReadOnlyCollection<GridPoint> Targets => targets;
        public IReadOnlyList<GridPoint> Boxes => boxes;

        public void Load(LevelData level)
        {
            Width = level.width;
            Height = level.height;
            walls.Clear();
            targets.Clear();
            boxes.Clear();

            foreach (GridPoint point in level.walls)
            {
                if (IsInside(point))
                {
                    walls.Add(point);
                }
            }

            foreach (GridPoint point in level.targets)
            {
                if (IsInside(point))
                {
                    targets.Add(point);
                }
            }

            foreach (GridPoint point in level.boxes)
            {
                if (IsInside(point) && !walls.Contains(point) && IndexOfBox(point) < 0)
                {
                    boxes.Add(point);
                }
            }
        }

        public bool IsInside(GridPoint point)
        {
            return point.x >= 0 && point.x < Width && point.y >= 0 && point.y < Height;
        }

        public bool IsBlocked(GridPoint point, int ignoredBoxIndex = -1)
        {
            if (!IsInside(point) || walls.Contains(point))
            {
                return true;
            }

            int boxIndex = IndexOfBox(point);
            return boxIndex >= 0 && boxIndex != ignoredBoxIndex;
        }

        public bool ContainsWall(GridPoint point)
        {
            return walls.Contains(point);
        }

        public bool RemoveWall(GridPoint point)
        {
            return walls.Remove(point);
        }

        public bool IsEmpty(GridPoint point)
        {
            return IsInside(point) && !walls.Contains(point) && IndexOfBox(point) < 0;
        }

        public int IndexOfBox(GridPoint point)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] == point)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryMoveBox(int boxIndex, GridPoint target)
        {
            if (boxIndex < 0 || boxIndex >= boxes.Count || IsBlocked(target, boxIndex))
            {
                return false;
            }

            boxes[boxIndex] = target;
            return true;
        }

        public bool IsComplete()
        {
            if (boxes.Count == 0 || targets.Count == 0)
            {
                return false;
            }

            foreach (GridPoint box in boxes)
            {
                if (!targets.Contains(box))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
