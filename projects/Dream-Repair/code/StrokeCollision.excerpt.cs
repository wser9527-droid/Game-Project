// READING EXCERPT: members of the original class; not a standalone C# compilation unit.
// Source: Assets/DreamRepair/Scripts/DreamRepairGame.cs
// Dependencies: DreamRepairGame fields, EnemyRuntime, SkillDef, SkillDamage, DamageEnemy, HitSpark, System.Linq and UnityEngine.

// Source lines 5216-5226
        private static List<Vector2> SmoothPoints(IReadOnlyList<Vector2> points)
        {
            if (points.Count <= 2) return points.ToList();
            var smoothed = new List<Vector2> { points[0] };
            for (var i = 1; i < points.Count - 1; i++)
            {
                smoothed.Add((points[i - 1] + points[i] * 2f + points[i + 1]) * 0.25f);
            }
            smoothed.Add(points[^1]);
            return smoothed;
        }

// Source lines 5352-5402
        private bool CastLineCut(IReadOnlyList<Vector2> rawPoints, SkillDef skill)
        {
            lastCastHitCount = 0;
            lastCastUsedFallbackTarget = false;
            lastCastFailure = "";
            lastBossDamageText = "";
            if (rawPoints.Count < 2)
            {
                lastCastFailure = "Line too short";
                return false;
            }

            var points = SmoothPoints(rawPoints);
            var width = 0.14f + stats.lineWidthBonus + stats.skillLevels[SkillId.PaperBird] * 0.012f;
            var hit = new List<EnemyRuntime>();
            foreach (var enemy in enemies.ToArray())
            {
                var enemyPos = (Vector2)enemy.go.transform.position;
                for (var i = 1; i < points.Count; i++)
                {
                    if (DistancePointToSegment(enemyPos, points[i - 1], points[i]) <= enemy.def.radius + width)
                    {
                        hit.Add(enemy);
                        break;
                    }
                }
            }

            if (hit.Count == 0)
            {
                var fallbackOrigin = points[points.Count - 1];
                var target = BossTargetForFallback() ?? NearestEnemy(fallbackOrigin, float.MaxValue);
                if (target == null)
                {
                    lastCastFailure = "No target";
                    return false;
                }
                hit.Add(target);
                lastCastUsedFallbackTarget = true;
            }

            foreach (var enemy in hit)
            {
                var hitPos = (Vector2)enemy.go.transform.position;
                DamageEnemy(enemy, SkillDamage(skill));
                HitSpark(hitPos, skill.color, Mathf.Clamp(enemy.def.radius * 0.9f, 0.11f, 0.2f));
            }
            lastCastHitCount = hit.Count;
            lastProjectileSpriteName = "line-cut";
            return true;
        }

// Source lines 5431-5460
        private EnemyRuntime? BossTargetForFallback()
        {
            if (mode != Mode.Playing || !CurrentLevel().isBoss) return null;
            return enemies.FirstOrDefault(enemy => enemy.isBoss);
        }

        private EnemyRuntime? NearestEnemy(Vector2 origin, float maxDistance)
        {
            EnemyRuntime? best = null;
            var bestDist = maxDistance;
            foreach (var enemy in enemies)
            {
                var dist = Vector2.Distance(origin, enemy.go.transform.position);
                if (dist <= bestDist)
                {
                    best = enemy;
                    bestDist = dist;
                }
            }
            return best;
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var lengthSq = segment.sqrMagnitude;
            if (lengthSq < 0.0001f) return Vector2.Distance(point, a);
            var t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSq);
            return Vector2.Distance(point, a + segment * t);
        }
