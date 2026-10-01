using UnityEngine;

namespace LockSpin
{
    // Hidden help after repeated campaign losses (README.md): wider targets, a calmer needle and,
    // for the guardian, extra shields. Never on a first attempt, never harder, reset by a win.
    public static class LockSpinAssist
    {
        public static readonly float[] ArcScale = { 1, 1.12f, 1.20f };
        public static readonly float[] SpeedScale = { 1, .92f, .88f };

        public static int TierForLosses(int losses) => losses >= 3 ? 2 : losses >= 2 ? 1 : 0;

        public static LevelConfig Apply(LevelConfig source, int tier)
        {
            tier = Mathf.Clamp(tier, 0, ArcScale.Length - 1);
            if (source == null || tier == 0) return source;
            var level = JsonUtility.FromJson<LevelConfig>(JsonUtility.ToJson(source));
            float arc = ArcScale[tier], speed = SpeedScale[tier];
            foreach (var s in level.segments)
            {
                float center = s.startAngle + s.arc * .5f;
                s.arc = Mathf.Min(100, s.arc * arc);
                s.startAngle = Mathf.Repeat(center - s.arc * .5f, 360);
            }
            level.minSegmentArc *= arc; level.maxSegmentArc *= arc;
            level.needleSpeed *= speed;
            if (level.bossPhases != null)
                foreach (var phase in level.bossPhases)
                {
                    phase.needleSpeed *= speed;
                    phase.minArc *= arc; phase.maxArc *= arc;
                }
            if (level.bossEncounter) level.playerHealth += tier + 1;
            return level;
        }
    }
}
