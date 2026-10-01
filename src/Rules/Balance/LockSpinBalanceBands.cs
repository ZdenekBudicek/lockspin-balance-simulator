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

        public static bool HasReviewedBand(int levelId) => levelId >= 1 && levelId <= IslandOne.Length;

        public static LockSpinBalanceBand For(int levelId)
        {
            if (!HasReviewedBand(levelId))
                throw new System.ArgumentOutOfRangeException(nameof(levelId), "Only the original ten levels have reviewed bands.");
            return IslandOne[levelId - 1];
        }
    }
}
