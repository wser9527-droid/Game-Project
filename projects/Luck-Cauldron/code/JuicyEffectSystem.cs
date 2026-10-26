using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuckyCauldron
{
    public sealed class JuicyEffectSystem : MonoBehaviour
    {
        private readonly List<Coroutine> active = new();

        public void KillAll()
        {
            foreach (var routine in active)
            {
                if (routine != null) StopCoroutine(routine);
            }
            active.Clear();
        }

        public Coroutine Play(IEnumerator routine)
        {
            var c = StartCoroutine(Wrap(routine));
            active.Add(c);
            return c;
        }

        public IEnumerator Pop(RectTransform target, float scale = 1.12f, float duration = 0.18f)
        {
            var start = target.localScale;
            yield return Scale(target, start, Vector3.one * scale, duration * 0.45f);
            yield return Scale(target, target.localScale, Vector3.one, duration * 0.55f);
        }

        public IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var p = EaseOutBack(t / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, p);
                yield return null;
            }
            target.anchoredPosition = to;
        }

        public IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            group.alpha = to;
        }

        private IEnumerator Scale(RectTransform target, Vector3 from, Vector3 to, float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var p = EaseOutBack(t / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, p);
                yield return null;
            }
            target.localScale = to;
        }

        private IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
