using System;
using System.Collections.Generic;
using UnityEngine;

namespace LockSpin
{
    // Human-like player model that plays the real LockSpinSession rules. Used by the balance report
    // (Lock Spin > Balance > Report) and the standalone tuning harness. See README.md.
    public static class LockSpinPlayerModel
    {
        public sealed class Persona
        {
            public readonly string name;
            public readonly float sigma;   // tap timing SD, seconds
            public readonly float bias;    // SD of a player's personal early/late offset, seconds
            public readonly float react;   // min time from a target appearing to a planned tap, seconds
            public readonly float lapse;   // chance to let a reachable target pass
            public readonly float stray;   // accidental taps per second
            public Persona(string name, float sigma, float bias, float react, float lapse, float stray)
            { this.name = name; this.sigma = sigma; this.bias = bias; this.react = react; this.lapse = lapse; this.stray = stray; }
        }

        // Assumed timing profiles for sensitivity analysis, not a validated human-performance model.
        // Replace these parameters with measured playtest telemetry before making player forecasts.
        public static readonly Persona Weak = new Persona("weak 95ms", .095f, .040f, .50f, .12f, .03f);
        public static readonly Persona Casual = new Persona("casual 70ms", .070f, .025f, .40f, .06f, .02f);
        public static readonly Persona Engaged = new Persona("engaged 50ms", .050f, .015f, .32f, .03f, .01f);
        public static readonly Persona Skilled = new Persona("skilled 35ms", .035f, .008f, .25f, .015f, .005f);
        public static readonly Persona[] All = { Weak, Casual, Engaged, Skilled };
        // Assumed audience mix for population targets (same order as All). Re-weight from telemetry.
        public static readonly float[] Mix = { .20f, .50f, .20f, .10f };

        public static float PopulationWin(Stats[] all)
        {
            float win = 0;
            for (int i = 0; i < all.Length && i < Mix.Length; i++) win += Mix[i] * all[i].winRate;
            return win;
        }

        public struct Run
        {
            public bool won; public float elapsed, progress; public string failure;
            public int stars, misses, perfects, hits;
        }

        public sealed class Stats
        {
            public Persona persona; public int runs, wins;
            public float winRate, medianWinSeconds, threeStarShare, perfectRate, lossProgress, winWithin3;
            public readonly Dictionary<string, int> failures = new Dictionary<string, int>();
        }

        const float Dt = 1f / 60f;

        static double Gauss(System.Random r)
        {
            double u1 = 1 - r.NextDouble(), u2 = r.NextDouble();
            return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }

        public static Run Play(LevelConfig level, Persona p, int seed, float? persistentBias = null)
        {
            var s = new LockSpinSession(level, seed);
            var rng = new System.Random(seed * 31 + 7);
            int n = s.Level.segments.Count;
            var seenAt = new float[n]; var start = new float[n]; var width = new float[n]; var skip = new bool[n];
            for (int i = 0; i < n; i++) seenAt[i] = float.NaN;
            int plan = -1; float err = 0, lastDir = s.Direction, t = 0;
            float runBias = (float)(Gauss(rng) * p.bias);
            if (persistentBias.HasValue) runBias = persistentBias.Value;
            while (!s.Finished && t < 200)
            {
                s.Tick(Dt); t += Dt;
                if (s.Finished) break;
                float dir = s.Direction, angle = s.Angle, speed = Math.Max(1, s.CurrentSpeed);
                if (dir != lastDir) { plan = -1; Array.Clear(skip, 0, n); lastDir = dir; }
                for (int i = 0; i < n; i++)
                {
                    var g = s.Level.segments[i];
                    if (!s.IsSegmentActive(i)) { seenAt[i] = float.NaN; skip[i] = false; if (plan == i) plan = -1; continue; }
                    if (float.IsNaN(seenAt[i]) || start[i] != g.startAngle || width[i] != g.arc)
                    { seenAt[i] = t; start[i] = g.startAngle; width[i] = g.arc; skip[i] = false; if (plan == i) plan = -1; }
                    // Once the needle is well past a target it is a fresh opportunity next lap.
                    if (skip[i] && Mathf.DeltaAngle(g.startAngle + g.arc * .5f, angle) * dir > g.arc * .5f + 20) skip[i] = false;
                }
                if (plan < 0)
                {
                    float best = float.MaxValue;
                    for (int i = 0; i < n; i++)
                    {
                        if (float.IsNaN(seenAt[i]) || skip[i]) continue;
                        var g = s.Level.segments[i];
                        // Degrees until the centre along travel; negative while inside past the centre.
                        float d = Mathf.Repeat((g.startAngle + g.arc * .5f - angle) * dir + g.arc * .5f, 360) - g.arc * .5f;
                        if (d < best) { best = d; plan = i; }
                    }
                    if (plan >= 0)
                    {
                        float e = (float)(runBias + Gauss(rng) * p.sigma);
                        if (t + best / speed + e - seenAt[plan] < p.react || rng.NextDouble() < p.lapse) { skip[plan] = true; plan = -1; }
                        else err = e;
                    }
                }
                if (plan >= 0)
                {
                    var g = s.Level.segments[plan];
                    if (Mathf.DeltaAngle(g.startAngle + g.arc * .5f, angle) * dir >= err * speed)
                    {
                        skip[plan] = true; plan = -1;
                        s.Tap();
                        continue;
                    }
                }
                if (rng.NextDouble() < p.stray * Dt) s.Tap();
            }
            return new Run
            {
                won = s.Won,
                elapsed = s.Elapsed,
                progress = s.ResultProgress,
                failure = s.Finished ? s.Failure : "SIMULATION LIMIT",
                stars = s.Stars,
                misses = s.Misses,
                perfects = s.Perfects,
                hits = s.Hits
            };
        }

        public static Stats Evaluate(LevelConfig level, Persona p, int runs, bool withAssist = true)
        {
            if (runs <= 0) throw new ArgumentOutOfRangeException(nameof(runs));
            var stats = new Stats { persona = p, runs = runs };
            var winTimes = new List<float>();
            int winsWithin3 = 0;
            var thirdAttempt = withAssist ? LockSpinAssist.Apply(level, 1) : level;
            int threeStars = 0, hits = 0, perfects = 0; float lossProgress = 0;
            for (int i = 1; i <= runs; i++)
            {
                int seed = 1000 + i * 17;
                var r = Play(level, p, seed);
                // Follow the same player's early/late tendency across retries, not independent population averages.
                float bias = (float)(Gauss(new System.Random(seed * 31 + 7)) * p.bias);
                if (r.won || Play(level, p, seed + 1000000, bias).won || Play(thirdAttempt, p, seed + 2000000, bias).won)
                    winsWithin3++;
                hits += r.hits; perfects += r.perfects;
                if (r.won) { stats.wins++; winTimes.Add(r.elapsed); if (r.stars == 3) threeStars++; }
                else
                {
                    lossProgress += r.progress;
                    string key = r.failure ?? "?";
                    stats.failures[key] = stats.failures.TryGetValue(key, out int c) ? c + 1 : 1;
                }
            }
            winTimes.Sort();
            stats.winRate = stats.wins / (float)runs;
            stats.medianWinSeconds = MedianSorted(winTimes);
            stats.threeStarShare = stats.wins > 0 ? threeStars / (float)stats.wins : 0;
            stats.perfectRate = hits > 0 ? perfects / (float)hits : 0;
            stats.lossProgress = runs > stats.wins ? lossProgress / (runs - stats.wins) : 0;
            stats.winWithin3 = winsWithin3 / (float)runs;
            return stats;
        }

        public static float MedianSorted(IReadOnlyList<float> sorted)
        {
            if (sorted.Count == 0) return 0;
            int middle = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
        }

        // all = Evaluate results in the order of All.
        // Share of plain hits whose target really moved (perfect centre taps). A packed dial silently keeps
        // targets still, which changes the level completely, so the report fails below 90%.
        public static float RelocationRate(LevelConfig level, int seeds = 12)
        {
            if (!level.relocateHitSegment) return 1;
            int plain = 0, moved = 0;
            for (int seed = 1; seed <= seeds; seed++)
            {
                var run = new LockSpinSession(level, seed);
                for (int frame = 0; frame < 60000 && !run.Finished; frame++)
                {
                    run.Tick(.005f);
                    int i = run.SegmentAt(run.Angle);
                    if (i < 0) continue;
                    var s = run.Level.segments[i];
                    if (Mathf.Abs(Mathf.DeltaAngle(run.Angle, s.startAngle + s.arc * .5f)) >= .6f) continue;
                    bool plainTarget = !run.IsRushSlot(i) && !run.IsTough(i) && !run.IsFrozenSegment(i);
                    float before = s.startAngle;
                    // Moved = a new place; a respawn in place (no room anywhere) does not count.
                    if (run.Tap().HasValue && plainTarget && !run.Finished) { plain++; if (run.LastRelocatedSegment == i && s.startAngle != before) moved++; }
                }
            }
            return plain > 0 ? moved / (float)plain : 1;
        }

        public static string Verdict(LevelConfig level, Stats[] all, out bool ok)
        {
            if (!LockSpinBalanceBands.HasReviewedBand(level.id))
            {
                ok = false;
                return "UNRATED: no reviewed band for this expansion level";
            }
            var band = LockSpinBalanceBands.For(level.id);
            Stats weak = all[0], casual = all[1];
            float population = PopulationWin(all);
            var problems = new List<string>();
            // Peaks are two-sided (they must bite); every other role only has a floor - easier never hurts
            // by design; these bands express design goals rather than empirically validated player outcomes.
            if (band.TwoSided ? Math.Abs(population - band.casualWin) > band.tolerance + 1e-4f : population < band.casualWin - band.tolerance - 1e-4f)
                problems.Add($"population win {population:P0} vs {band.casualWin:P0}{(band.TwoSided ? "Â±" : "-")}{band.tolerance:P0}");
            if (weak.winRate < band.weakMin) problems.Add($"weak win {weak.winRate:P0} < {band.weakMin:P0}");
            if (band.weakWithin3Min > 0 && weak.winWithin3 < band.weakWithin3Min) problems.Add($"weak within 3 {weak.winWithin3:P0} < {band.weakWithin3Min:P0}");
            if (casual.medianWinSeconds < band.durationMin || casual.medianWinSeconds > band.durationMax) problems.Add($"duration {casual.medianWinSeconds:0}s vs {band.durationMin:0}-{band.durationMax:0}s");
            if (casual.threeStarShare < band.threeStarMin || casual.threeStarShare > band.threeStarMax) problems.Add($"3-star {casual.threeStarShare:P0} vs {band.threeStarMin:P0}-{band.threeStarMax:P0}");
            if (casual.perfectRate < band.perfectMin || casual.perfectRate > band.perfectMax) problems.Add($"perfect {casual.perfectRate:P0} vs {band.perfectMin:P0}-{band.perfectMax:P0}");
            ok = problems.Count == 0;
            return ok ? "OK" : "OUT: " + string.Join("; ", problems);
        }
    }
}
