using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlackoutGrid
{
    public sealed class BeamRenderer : MonoBehaviour
    {
        private readonly List<BeamVisual> pool = new List<BeamVisual>();
        private static Sprite flowSprite;
        private RectTransform rectTransform;
        private Coroutine pulseRoutine;

        public void Render(LevelData level, LightSimulationResult result, float tileSize, Vector2 boardOffset)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            List<BeamSegmentData> channels = BuildChannelSegments(result);
            while (pool.Count < channels.Count)
            {
                pool.Add(CreateBeamVisual(pool.Count));
            }

            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < channels.Count;
                pool[i].Root.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                BeamSegmentData beam = channels[i];
                Vector2 start = GridToLocal(level, beam.Start, tileSize, boardOffset);
                Vector2 end = GridToLocal(level, beam.End, tileSize, boardOffset);
                Place(pool[i].Glow, start, end, tileSize * 0.26f);
                Place(pool[i].Core, start, end, tileSize * 0.12f);

                Color beamColor = LightPuzzleModel.UnityColor(beam.ColorMask, 1f);
                pool[i].Glow.GetComponent<Image>().color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.28f);
                pool[i].Core.GetComponent<Image>().color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.9f);
                pool[i].Spark.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.82f);
                pool[i].Spark.gameObject.SetActive(false);
                pool[i].Start = start;
                pool[i].End = end;
            }
        }

        public static List<BeamSegmentData> BuildChannelSegments(LightSimulationResult result)
        {
            const float channelEnd = 0.44f;
            List<BeamSegmentData> channels = new List<BeamSegmentData>();
            foreach (BeamSegmentData beam in result.Beams)
            {
                bool startIsCell = IsCellCenter(beam.Start);
                bool endIsCell = IsCellCenter(beam.End);
                if (startIsCell && endIsCell)
                {
                    Vector2 direction = (beam.End - beam.Start).normalized;
                    Vector2 firstOpening = beam.Start + direction * channelEnd;
                    Vector2 secondOpening = beam.End - direction * channelEnd;
                    channels.Add(new BeamSegmentData(beam.Start, firstOpening, result.CellMask((int)beam.Start.x, (int)beam.Start.y)));
                    channels.Add(new BeamSegmentData(firstOpening, secondOpening, beam.ColorMask));
                    channels.Add(new BeamSegmentData(secondOpening, beam.End, result.CellMask((int)beam.End.x, (int)beam.End.y)));
                }
                else if (startIsCell != endIsCell)
                {
                    Vector2 cell = startIsCell ? beam.Start : beam.End;
                    Vector2 outside = startIsCell ? beam.End : beam.Start;
                    Vector2 opening = cell + (outside - cell).normalized * channelEnd;
                    channels.Add(new BeamSegmentData(cell, opening, result.CellMask((int)cell.x, (int)cell.y)));
                    channels.Add(new BeamSegmentData(opening, outside, beam.ColorMask));
                }
            }

            return channels;
        }

        private static bool IsCellCenter(Vector2 point)
        {
            return Mathf.Abs(point.x - Mathf.Round(point.x)) < 0.01f
                && Mathf.Abs(point.y - Mathf.Round(point.y)) < 0.01f;
        }

        public void Pulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private BeamVisual CreateBeamVisual(int index)
        {
            GameObject root = new GameObject("Beam_" + index, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;

            return new BeamVisual
            {
                Root = rootRect,
                Glow = CreateSegment(root.transform, "Glow"),
                Core = CreateSegment(root.transform, "Core"),
                Spark = CreateSegment(root.transform, "Spark")
            };
        }

        private static RectTransform CreateSegment(Transform parent, string name)
        {
            GameObject segment = new GameObject(name, typeof(RectTransform), typeof(Image));
            segment.transform.SetParent(parent, false);
            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = segment.GetComponent<Image>();
            image.sprite = FlowSprite();
            image.raycastTarget = false;
            return rect;
        }

        private static Sprite FlowSprite()
        {
            if (flowSprite != null)
            {
                return flowSprite;
            }

            const int width = 16;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float distance = Mathf.Abs((y + 0.5f) * 2f / height - 1f);
                float alpha = Mathf.Pow(Mathf.Max(0f, 1f - distance * distance), 2f);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            flowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            return flowSprite;
        }

        private IEnumerator PulseRoutine()
        {
            float elapsed = 0f;
            while (elapsed < 0.55f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.55f);
                foreach (BeamVisual beam in pool)
                {
                    if (!beam.Root.gameObject.activeSelf)
                    {
                        continue;
                    }

                    Vector2 delta = beam.End - beam.Start;
                    float length = delta.magnitude;
                    if (length < 1f)
                    {
                        continue;
                    }

                    Vector2 direction = delta / length;
                    float sparkLength = Mathf.Min(130f, length * 0.42f);
                    Vector2 center = Vector2.Lerp(beam.Start, beam.End, t);
                    Place(beam.Spark, center - direction * sparkLength * 0.5f, center + direction * sparkLength * 0.5f, 20f);
                }

                yield return null;
            }

            foreach (BeamVisual beam in pool)
            {
                beam.Spark.gameObject.SetActive(false);
            }

            pulseRoutine = null;
        }

        private static void Place(RectTransform segment, Vector2 start, Vector2 end, float width)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 1f)
            {
                segment.gameObject.SetActive(false);
                return;
            }

            segment.gameObject.SetActive(true);
            segment.anchoredPosition = (start + end) * 0.5f;
            segment.sizeDelta = new Vector2(length, width);
            segment.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Vector2 GridToLocal(LevelData level, Vector2 gridPoint, float tileSize, Vector2 boardOffset)
        {
            float x = (gridPoint.x - (level.width - 1) * 0.5f) * tileSize + boardOffset.x;
            float y = ((level.height - 1) * 0.5f - gridPoint.y) * tileSize + boardOffset.y;
            return new Vector2(x, y);
        }

        private sealed class BeamVisual
        {
            public RectTransform Root;
            public RectTransform Glow;
            public RectTransform Core;
            public RectTransform Spark;
            public Vector2 Start;
            public Vector2 End;
        }
    }
}
