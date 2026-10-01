using System.Collections.Generic;

namespace LockSpin
{
    public static class LockSpinLevelLibrary
    {
        public static IReadOnlyList<LevelConfig> CreateLevels()
        {
            return new[]
            {
                Level(1, "First Click", ObjectiveKind.Score, 240, 45, 115, 0, SpinRule.Constant, false, false,
                    Seg(15, 64, SegmentKind.Gold), Seg(120, 48, SegmentKind.Blue), Seg(220, 70, SegmentKind.Gold)),
                Level(2, "Faster Dial", ObjectiveKind.Score, 320, 45, 155, 0, SpinRule.Constant, false, false,
                    Seg(30, 44, SegmentKind.Gold), Seg(135, 34, SegmentKind.Blue), Seg(250, 48, SegmentKind.Gold)),
                Level(3, "Time Saver", ObjectiveKind.Score, 360, 28, 145, 0, SpinRule.Constant, false, false,
                    Seg(10, 38, SegmentKind.Gold), Seg(86, 42, SegmentKind.Blue), Seg(198, 40, SegmentKind.Gold), Seg(300, 34, SegmentKind.Blue)),
                Level(4, "Perfect Intro", ObjectiveKind.Perfects, 4, 50, 135, 0, SpinRule.Constant, false, false,
                    Seg(25, 52, SegmentKind.Gold), Seg(140, 44, SegmentKind.Blue), Seg(250, 54, SegmentKind.Gold)),
                Level(5, "Combo Lock", ObjectiveKind.Combo, 5, 55, 150, 0, SpinRule.Constant, false, false,
                    Seg(20, 42, SegmentKind.Gold), Seg(108, 34, SegmentKind.Blue), Seg(205, 42, SegmentKind.Gold), Seg(302, 38, SegmentKind.Gold)),
                Level(6, "Moving Gold", ObjectiveKind.Score, 500, 60, 150, 0, SpinRule.Constant, true, false,
                    Seg(40, 42, SegmentKind.Gold), Seg(152, 36, SegmentKind.Blue), Seg(270, 42, SegmentKind.Gold)),
                Level(7, "Steady Rhythm", ObjectiveKind.Score, 520, 60, 155, 0, SpinRule.Constant, false, false,
                    Seg(12, 40, SegmentKind.Gold), Seg(112, 34, SegmentKind.Blue), Seg(204, 42, SegmentKind.Gold), Seg(310, 28, SegmentKind.Blue)),
                Level(8, "Shrinking Window", ObjectiveKind.Score, 560, 60, 145, 0, SpinRule.SpeedUpOnHit, false, true,
                    Seg(38, 58, SegmentKind.Gold), Seg(168, 42, SegmentKind.Blue), Seg(278, 58, SegmentKind.Gold)),
                Level(9, "No Miss Run", ObjectiveKind.Hits, 12, 60, 165, 3, SpinRule.Constant, false, false,
                    Seg(28, 38, SegmentKind.Gold), Seg(130, 32, SegmentKind.Blue), Seg(232, 38, SegmentKind.Gold), Seg(318, 30, SegmentKind.Gold)),
                Level(10, "First Boss", ObjectiveKind.Boss, 3, 80, 150, 5, SpinRule.Constant, true, true,
                    Seg(22, 46, SegmentKind.Gold), Seg(106, 30, SegmentKind.Blue), Seg(188, 42, SegmentKind.Gold), Seg(290, 34, SegmentKind.Gold)),
            };
        }

        static LevelConfig Level(int id, string title, ObjectiveKind objective, float target, float timeLimit, float speed, int missLimit, SpinRule spinRule, bool moving, bool shrinking, params SegmentConfig[] segments)
        {
            return new LevelConfig
            {
                id = id,
                title = title,
                objective = objective,
                objectiveTarget = target,
                timeLimit = timeLimit,
                needleSpeed = speed,
                missLimit = missLimit,
                spinRule = spinRule,
                moveSegmentsAfterHit = moving,
                shrinkGoldAfterHit = shrinking,
                phases = objective == ObjectiveKind.Boss ? 3 : 1,
                starScoreThresholds = new[] { 180 + id * 35, 300 + id * 45, 440 + id * 60 },
                segments = new List<SegmentConfig>(segments)
            };
        }

        static SegmentConfig Seg(float start, float arc, SegmentKind kind)
        {
            return new SegmentConfig(start, arc, kind);
        }
    }
}
