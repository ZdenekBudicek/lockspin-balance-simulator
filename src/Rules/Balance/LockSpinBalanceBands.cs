namespace LockSpin
{
    // What a level is for. Drives its target band; see README.md.
    public enum LockSpinLevelRole { None, Tutorial, Practice, Intro, Skill, Peak, Breather, Twist, VictoryLap, Boss }

    // Target band for one level, measured by LockSpinPlayerModel (first attempt, no assist).
    public readonly struct LockSpinBalanceBand
    {
        public readonly LockSpinLevelRole role;
        public readonly float casualWin, tolerance;   // population first-try win rate (LockSpinPlayerModel.Mix), +/- tolerance
        public readonly float weakMin;                // weak (95 ms) first-try minimum
        public readonly float weakWithin3Min;         // weak within three attempts incl. hidden assist (0 = not checked)
        public readonly float durationMin, durationMax; // casual median winning run, seconds of play
        public readonly float threeStarMin, threeStarMax; // share of casual wins with three stars
        public readonly float perfectMin, perfectMax;     // casual Perfects per hit

        public bool TwoSided => role == LockSpinLevelRole.Peak || role == LockSpinLevelRole.Twist || role == LockSpinLevelRole.Boss;

        public LockSpinBalanceBand(LockSpinLevelRole role, float casualWin, float tolerance, float weakMin, float weakWithin3Min,
            float durationMin, float durationMax, float threeStarMin, float threeStarMax, float perfectMin, float perfectMax)
        {
            this.role = role; this.casualWin = casualWin; this.tolerance = tolerance; this.weakMin = weakMin;
            this.weakWithin3Min = weakWithin3Min; this.durationMin = durationMin; this.durationMax = durationMax;
            this.threeStarMin = threeStarMin; this.threeStarMax = threeStarMax; this.perfectMin = perfectMin; this.perfectMax = perfectMax;
        }
    }

    public static class LockSpinBalanceBands
    {
        // Island 1 (Royal Castle): onboarding saw-tooth. Sources and reasoning: README.md.
        static readonly LockSpinBalanceBand[] IslandOne =
        {
            //                     role                         population  tol  weak  weak<=3  seconds  3-star     perfect/hit
            new LockSpinBalanceBand(LockSpinLevelRole.Tutorial,   .99f, .02f, .95f, 0,    14, 30, .25f, .60f, .55f, .70f),
            new LockSpinBalanceBand(LockSpinLevelRole.Practice,   .97f, .03f, .90f, 0,    18, 32, .25f, .50f, .55f, .70f),
            new LockSpinBalanceBand(LockSpinLevelRole.Intro,      .95f, .04f, .85f, 0,    20, 35, .25f, .50f, .55f, .70f),
            new LockSpinBalanceBand(LockSpinLevelRole.Skill,      .92f, .04f, .80f, 0,    20, 40, .25f, .50f, .55f, .75f),
            new LockSpinBalanceBand(LockSpinLevelRole.Peak,       .85f, .04f, .35f, .80f, 25, 45, .20f, .40f, .40f, .55f),
            new LockSpinBalanceBand(LockSpinLevelRole.Breather,   .95f, .04f, .85f, 0,    25, 45, .25f, .45f, .40f, .55f),
            new LockSpinBalanceBand(LockSpinLevelRole.Intro,      .90f, .04f, .75f, 0,    25, 45, .20f, .45f, .40f, .55f),
            new LockSpinBalanceBand(LockSpinLevelRole.Twist,      .85f, .05f, .50f, .85f, 30, 45, .20f, .40f, .40f, .55f),
            new LockSpinBalanceBand(LockSpinLevelRole.VictoryLap, .93f, .04f, .80f, 0,    25, 45, .25f, .45f, .40f, .55f),
            new LockSpinBalanceBand(LockSpinLevelRole.Boss,       .70f, .05f, .20f, .70f, 30, 60, .10f, .25f, .40f, .55f),
        };

        // Islands 2+: one ten-slot template, each island a touch harder, with floors.
        static readonly (LockSpinLevelRole role, float win, float step, float floor)[] Template =
        {
            (LockSpinLevelRole.Breather,   .95f, .005f, .90f), // 1 warm-up with known rules
            (LockSpinLevelRole.Intro,      .95f, 0,     .95f), // 2 mechanic A, safe
            (LockSpinLevelRole.Practice,   .90f, .005f, .85f), // 3 practise A
            (LockSpinLevelRole.Practice,   .86f, .005f, .80f), // 4 A + older rules
            (LockSpinLevelRole.Peak,       .75f, .01f,  .62f), // 5 mini-peak (HARD tag from island 3)
            (LockSpinLevelRole.Intro,      .93f, 0,     .93f), // 6 breather / mechanic B
            (LockSpinLevelRole.Practice,   .88f, .005f, .82f), // 7 practise B
            (LockSpinLevelRole.Twist,      .82f, .01f,  .70f), // 8 twist A + B
            (LockSpinLevelRole.VictoryLap, .93f, 0,     .93f), // 9 victory lap
            (LockSpinLevelRole.Boss,       .65f, .01f,  .55f), // 10 boss
        };

        // levelId is 1-based across the whole campaign (11 = island 2, slot 1).
        public static LockSpinBalanceBand For(int levelId)
        {
            int index = System.Math.Max(0, levelId - 1);
            if (index < IslandOne.Length) return IslandOne[index];
            int island = index / 10 + 1, slot = index % 10;
            var t = Template[slot];
            float win = System.Math.Max(t.floor, t.win - t.step * (island - 2));
            bool peak = t.role == LockSpinLevelRole.Peak || t.role == LockSpinLevelRole.Boss || t.role == LockSpinLevelRole.Twist;
            return new LockSpinBalanceBand(t.role, win, peak ? .05f : .04f, System.Math.Max(.25f, win - (peak ? .40f : .15f)), peak ? .70f : 0,
                t.role == LockSpinLevelRole.Boss ? 35 : 25, t.role == LockSpinLevelRole.Boss ? 60 : 45,
                t.role == LockSpinLevelRole.Boss ? .10f : .20f, t.role == LockSpinLevelRole.Boss ? .25f : .40f, .35f, .55f);
        }
    }
}
