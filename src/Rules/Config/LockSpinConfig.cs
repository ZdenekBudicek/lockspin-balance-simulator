using System;
using System.Collections.Generic;
using UnityEngine;

namespace LockSpin
{
    public enum DailyBossKind { None, Crystal, Clockwork, Ice }
    public enum SegmentKind
    {
        Neutral,
        Gold,
        Blue,
        Avoid,
        Bonus,
        Frozen
    }

    public enum ObjectiveKind
    {
        Score,
        Hits,
        Perfects,
        Combo,
        Survival,
        Boss
    }

    public enum SpinRule
    {
        Constant,
        FlipAfterHit, // Retired; keep its serialized value. Successful hits now preserve direction.
        SpeedUpOnHit
    }

    [Serializable]
    public sealed class SegmentConfig
    {
        [Range(0f, 360f)] public float startAngle;
        [Range(4f, 180f)] public float arc;
        public SegmentKind kind;
        [Range(0f, 1f)] public float perfectFraction = 0.28f;

        public SegmentConfig(float startAngle, float arc, SegmentKind kind, float perfectFraction = 0.28f)
        {
            this.startAngle = startAngle;
            this.arc = arc;
            this.kind = kind;
            this.perfectFraction = perfectFraction;
        }
    }

    [Serializable]
    public sealed class BossPhaseConfig
    {
        public string title;
        public ObjectiveKind objective;
        public int target;
        public SpinRule spinRule;
        public float needleSpeed;
        public bool resizeTargets;
        public float minArc = 38, maxArc = 56;
    }

    [Serializable]
    public sealed class SpeedZoneConfig
    {
        public float startAngle;
        [Range(10,180)] public float arc=90;
        [Range(1,1.75f)] public float multiplier=1.25f;
        [Range(1,20)] public float featherDegrees=10;
    }

    [Serializable]
    public sealed class LevelConfig
    {
        public int id;
        public string title;
        public ObjectiveKind objective;
        public SpinRule spinRule;
        public float objectiveTarget;
        public float timeLimit;
        public float needleSpeed;
        public int missLimit;
        public bool moveSegmentsAfterHit;
        public bool shrinkGoldAfterHit;
        public bool relocateHitSegment;
        public bool resizeHitSegment;
        public bool reverseOnMiss;
        public bool missSpeedResponse;
        public float minSegmentArc = 26;
        public float maxSegmentArc = 58;
        public float segmentRespawnDelay = .22f;
        public float idleLapPenalty;
        public bool perfectCombo;
        // Chance that a gold target returns tough after it moves: its first hit cracks it and turns the needle.
        [Range(0f, 1f)] public float crackChance;
        // Chance that a plain hit opens a rush: the targets step aside for a packed run of rushSize small gold ones.
        [Range(0f, 1f)] public float rushChance;
        public int rushSize;
        public float maxNeedleSpeed = 128;
        public int threeStarPerfects = 3;
        public int threeStarMisses;
        public int twoStarMisses = 3;
        public BossPhaseConfig[] bossPhases;
        public bool bossEncounter;
        public DailyBossKind dailyBoss;
        public int bossHealth = 400;
        public int playerHealth = 5;
        public int bossHitDamage = 10;
        public int bossPerfectDamage = 25;
        public int phases = 1;
        // Balance system (README.md). Defaults keep the legacy behaviour.
        public LockSpinLevelRole role;
        // Needle speed multiplier from the start to the end of the objective: each level builds to a finale.
        public float rampStart = 1, rampEnd = 1;
        // Half Perfect window in milliseconds of needle travel; 0 keeps each segment's perfectFraction.
        public float perfectWindowMs;
        public int[] starScoreThresholds;
        public List<SegmentConfig> segments = new List<SegmentConfig>();
        public SpeedZoneConfig[] speedZones = Array.Empty<SpeedZoneConfig>();

        public bool UsesMissLimit => missLimit > 0;
    }

    public readonly struct TapResult
    {
        public readonly SegmentKind kind;
        public readonly bool perfect;
        public readonly bool hit;
        public readonly float angle;

        public TapResult(SegmentKind kind, bool perfect, bool hit, float angle)
        {
            this.kind = kind;
            this.perfect = perfect;
            this.hit = hit;
            this.angle = angle;
        }
    }
}
