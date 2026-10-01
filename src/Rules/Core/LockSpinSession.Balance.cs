using UnityEngine;

namespace LockSpin
{
    // Balance-system rules: in-level tension ramp, millisecond Perfect window, progress and tap-timing telemetry.
    public sealed partial class LockSpinSession
    {
        public const float FinalStretchAt = .8f;
        public int BestCombo { get; private set; }
        public int TapSamples { get; private set; }
        public bool LastMissNear { get; private set; }
        public bool LastMissEarly { get; private set; }
        public int EarlyMisses { get; private set; }
        public int LateMisses { get; private set; }
        public string TimingAdvice => EarlyMisses > LateMisses ? "LET THE NEEDLE REACH THE COLOURED TARGET" : LateMisses > 0 ? "TAP AS THE NEEDLE ENTERS THE COLOURED TARGET" : "WATCH THE NEEDLE, THEN TAP THE COLOURED TARGET";
        void ClassifyMiss()
        {
            float best = float.MaxValue, signed = 0;
            for (int i = 0; i < Level.segments.Count; i++)
            {
                if (!IsSegmentActive(i) || Level.segments[i].kind == SegmentKind.Neutral) continue;
                var s = Level.segments[i]; float delta = Mathf.DeltaAngle(s.startAngle + s.arc * .5f, Angle);
                float distance = Mathf.Max(0, Mathf.Abs(delta) - s.arc * .5f);
                if (distance < best) { best = distance; signed = delta * direction; }
            }
            LastMissNear = best <= CurrentSpeed * .12f; LastMissEarly = signed < 0;
            if (LastMissNear) { if (LastMissEarly) EarlyMisses++; else LateMisses++; }
        }
        double tapOffsetSum, tapOffsetSquares;

        public bool RampEnabled => !IsTutorial && (Level.rampStart != 1 || Level.rampEnd != 1);
        public float RampFactor => RampEnabled ? Mathf.Lerp(Level.rampStart, Level.rampEnd, ProgressFraction) : 1;
        public bool FinalStretch => RampEnabled && !Finished && ProgressFraction >= FinalStretchAt;

        // 0..1 toward the win. The guardian: the gate is the first 30%, its health the rest.
        public float ProgressFraction
        {
            get
            {
                if (Won) return 1;
                if (IsBossEncounter)
                {
                    if (Phase == 0) return .3f * Mathf.Clamp01(Score / (float)Mathf.Max(1, Level.bossPhases[0].target));
                    return .3f + .7f * Mathf.Clamp01(1 - BossHealth / (float)Mathf.Max(1, Level.bossHealth));
                }
                if (Level.objective == ObjectiveKind.Boss) return Mathf.Clamp01(Phase / (float)Mathf.Max(1, Level.phases));
                return Level.objectiveTarget > 0 ? Mathf.Clamp01(Progress / Level.objectiveTarget) : 0;
            }
        }

        // What the fail screen shows: a combo run is judged by its best streak, not the one a miss just broke.
        public float ResultProgress => Level.objective == ObjectiveKind.Combo && !IsBossEncounter && Level.objectiveTarget > 0
            ? Mathf.Clamp01(Mathf.Max(Combo, BestCombo) / Level.objectiveTarget) : ProgressFraction;

        // Signed tap timing against the nearest target centre, + = late. Calibrates the simulator's personas.
        public float TapOffsetMeanMs => TapSamples > 0 ? (float)(tapOffsetSum / TapSamples) : 0;
        public float TapOffsetSdMs => TapSamples > 1 ? (float)System.Math.Sqrt(System.Math.Max(0, tapOffsetSquares / TapSamples - System.Math.Pow(tapOffsetSum / TapSamples, 2))) : 0;

        void ApplyPerfectWindow(int index)
        {
            if (Level.perfectWindowMs <= 0 || index < 0 || index >= Level.segments.Count) return;
            var s = Level.segments[index];
            // The painted window and hit test share the real capped/recovering needle speed.
            float degrees = 2 * CurrentSpeed * Level.perfectWindowMs / 1000f;
            s.perfectFraction = Mathf.Clamp(degrees / Mathf.Max(1, s.arc), 0, .45f);
        }

        void ApplyPerfectWindows()
        {
            for (int i = 0; i < Level.segments.Count; i++) ApplyPerfectWindow(i);
        }

        void RecordTapTiming()
        {
            if (IsTutorial) return;
            float best = float.MaxValue, offset = 0;
            for (int i = 0; i < Level.segments.Count; i++)
            {
                if (!IsSegmentActive(i)) continue;
                var s = Level.segments[i];
                float delta = Mathf.DeltaAngle(s.startAngle + s.arc * .5f, Angle);
                if (Mathf.Abs(delta) > s.arc * .5f + 30 || Mathf.Abs(delta) >= best) continue;
                best = Mathf.Abs(delta); offset = delta * direction;
            }
            if (best == float.MaxValue) return;
            double ms = offset / Mathf.Max(1, CurrentSpeed) * 1000;
            TapSamples++; tapOffsetSum += ms; tapOffsetSquares += ms * ms;
        }
    }
}
