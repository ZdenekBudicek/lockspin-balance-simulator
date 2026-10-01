using System.Collections.Generic;

namespace LockSpin
{
    // Island 1 (Royal Castle) tuning. One row per level; roles and target bands live in
    // LockSpinBalanceBands, the reasoning in README.md. Verify every change with
    // Lock Spin > Balance > Report: each level must stay inside its band.
    // Layout rule: keep the arcs of one level under ~175 degrees in total. Relocation needs free room
    // (gaps, needle clearance); a packed dial silently keeps targets still. The balance report checks it.
    public static class LockSpinIslandOne
    {
        sealed class Row
        {
            public string title; public LockSpinLevelRole role;
            public ObjectiveKind objective; public float target, time, speed;
            public float perfectMs = 50, rampStart = .94f, rampEnd = 1.08f, idle = 10;
            public float crack, rush; public int rushSize;
            public bool resize, recovery; public float minArc = 34, maxArc = 70;
            public int missLimit = 0, threeStarPerfects, threeStarMisses = 1, twoStarMisses = 3;
            public SegmentConfig[] segments;
        }

        static SegmentConfig G(float start, float arc) => new SegmentConfig(start, arc, SegmentKind.Gold);
        static SegmentConfig B(float start, float arc) => new SegmentConfig(start, arc, SegmentKind.Blue);

        static readonly Row[] Rows =
        {
            // 1 Tutorial: nearly impossible to fail, understood in ten seconds.
            new Row { title="First Click", role=LockSpinLevelRole.Tutorial, objective=ObjectiveKind.Score, target=290, time=45, speed=90,
                perfectMs=60, rampStart=1, rampEnd=1, idle=0, threeStarPerfects=8,
                segments=new[]{ G(15,60), B(130,50), G(235,60) } },
            // 2 Practice: targets move after each hit; the goal is a hit count, not score.
            new Row { title="Keep Moving", role=LockSpinLevelRole.Practice, objective=ObjectiveKind.Hits, target=18, time=45, speed=96,
                perfectMs=60, threeStarPerfects=11,
                segments=new[]{ G(20,60), B(140,48), G(250,60) } },
            // 3 Intro: the clock matters - blue buys time.
            new Row { title="Time Saver", role=LockSpinLevelRole.Intro, objective=ObjectiveKind.Score, target=600, time=26, speed=100,
                perfectMs=64, threeStarPerfects=17,
                segments=new[]{ G(10,46), B(95,42), G(195,46), B(295,36) } },
            // 4 Skill: aim for the centre.
            new Row { title="Perfect Practice", role=LockSpinLevelRole.Skill, objective=ObjectiveKind.Perfects, target=13, time=50, speed=96,
                perfectMs=70, threeStarPerfects=13, threeStarMisses=0,
                segments=new[]{ G(25,62), B(140,46), G(250,62) } },
            // 5 Peak: first real tension. Score against a tight clock - combos (x2/x3) are the way there.
            // Not a "10 in a row" goal: chained-success goals compound per-tap misses and wall off weaker players.
            new Row { title="Combo Lock", role=LockSpinLevelRole.Peak, objective=ObjectiveKind.Score, target=1250, time=32, speed=110,
                threeStarPerfects=8,
                segments=new[]{ G(20,36), B(108,30), G(205,36), G(302,33) } },
            // 6 Breather + new toy: cracked targets.
            new Row { title="Cracked Locks", role=LockSpinLevelRole.Breather, objective=ObjectiveKind.Score, target=650, time=50, speed=100,
                crack=.35f, threeStarPerfects=6,
                segments=new[]{ G(40,56), B(152,46), G(270,56) } },
            // 7 Intro: rush waves, cracks keep coming.
            new Row { title="Rush Hour", role=LockSpinLevelRole.Intro, objective=ObjectiveKind.Hits, target=34, time=55, speed=104,
                crack=.30f, rush=.30f, rushSize=3, threeStarPerfects=12,
                segments=new[]{ G(12,50), B(112,40), G(204,50), B(310,34) } },
            // 8 Twist: changing widths and the slow recovery after a miss.
            new Row { title="Shifting Windows", role=LockSpinLevelRole.Twist, objective=ObjectiveKind.Score, target=1050, time=50, speed=108,
                crack=.32f, rush=.25f, rushSize=4, resize=true, recovery=true, minArc=34, maxArc=58,
                threeStarPerfects=5, threeStarMisses=4, twoStarMisses=6,
                segments=new[]{ G(38,52), B(168,42), G(278,52) } },
            // 9 Victory lap: generous, loud, rewarding - the player feels strong before the boss.
            new Row { title="Treasure Run", role=LockSpinLevelRole.VictoryLap, objective=ObjectiveKind.Score, target=1100, time=60, speed=102,
                crack=.40f, rush=.40f, rushSize=4, threeStarPerfects=10, threeStarMisses=2,
                segments=new[]{ G(28,48), B(130,38), G(232,48), G(318,38) } },
            // 10 Boss: everything taught; shields are the miss limit.
            new Row { title="Castle Guardian", role=LockSpinLevelRole.Boss, objective=ObjectiveKind.Boss, target=3, time=80, speed=108,
                rush=.25f, rushSize=4, minArc=36, maxArc=58, threeStarPerfects=6, threeStarMisses=2, twoStarMisses=4,
                segments=new[]{ G(22,51), B(106,33), G(188,46), G(290,37) } },
        };

        public static void Apply(LevelConfig[] levels)
        {
            for (int i = 0; i < Rows.Length && i < levels.Length; i++)
            {
                var r = Rows[i]; var l = levels[i];
                l.title = r.title; l.role = r.role;
                l.objective = r.objective; l.objectiveTarget = r.target;
                l.timeLimit = r.time; l.needleSpeed = r.speed;
                l.perfectWindowMs = r.perfectMs; l.rampStart = r.rampStart; l.rampEnd = r.rampEnd;
                l.idleLapPenalty = r.idle;
                l.crackChance = r.crack; l.rushChance = r.rush; l.rushSize = r.rushSize;
                l.resizeHitSegment = r.resize; l.missSpeedResponse = r.recovery;
                l.minSegmentArc = r.minArc; l.maxSegmentArc = r.maxArc;
                l.missLimit = r.missLimit;
                l.threeStarMisses = r.threeStarMisses; l.twoStarMisses = r.twoStarMisses; l.threeStarPerfects = r.threeStarPerfects;
                if (r.segments != null)
                {
                    l.segments = new List<SegmentConfig>();
                    foreach (var s in r.segments) l.segments.Add(new SegmentConfig(s.startAngle, s.arc, s.kind, .30f));
                }
            }
            if (levels.Length >= 10)
            {
                var boss = levels[9];
                boss.bossPhases = new[] {
                    new BossPhaseConfig { title="SCORE 550", objective=ObjectiveKind.Score, target=550, needleSpeed=112, minArc=38, maxArc=58 },
                    new BossPhaseConfig { title="BREAK THE GUARDIAN", objective=ObjectiveKind.Score, target=200, needleSpeed=116, minArc=38, maxArc=58 },
                    new BossPhaseConfig { title="SEAL THE GUARDIAN", objective=ObjectiveKind.Score, target=200, needleSpeed=120, spinRule=SpinRule.Constant, minArc=36, maxArc=52 }
                };
                boss.playerHealth = 5;
            }
        }
    }
}
