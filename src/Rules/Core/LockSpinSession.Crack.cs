namespace LockSpin
{
    // Tough gold targets: the first hit cracks one and turns the needle, the second shatters it.
    public sealed partial class LockSpinSession
    {
        const int CrackPity = 5, FirstCrackPity = 2;
        bool[] tough, cracked;
        System.Random crackRandom;
        int goldMovesSinceTough;
        bool toughSeen;
        public bool SegmentCracked { get; private set; }
        public bool SegmentShattered { get; private set; }
        public bool CracksEnabled => Level.crackChance > 0 && !IsTutorial && !Level.bossEncounter && Level.dailyBoss == DailyBossKind.None;
        public bool IsTough(int index) => index >= 0 && index < tough.Length && tough[index] && !cracked[index];
        public bool IsCracked(int index) => index >= 0 && index < cracked.Length && cracked[index];

        void InitCracks(int seed)
        {
            tough = new bool[Level.segments.Count];
            cracked = new bool[Level.segments.Count];
            // A separate stream keeps the relocation sequence of every existing level unchanged.
            crackRandom = new System.Random(seed ^ 0x2C1A5);
        }

        // After a gold target moves. At most one tough or cracked target is on the dial. The first one comes
        // by the second gold move, so even a short run gets the whole crack-and-shatter payoff; after that
        // a run of plain moves forces the next one.
        void RollTough(int index)
        {
            if (!CracksEnabled || Level.segments[index].kind != SegmentKind.Gold) return;
            for (int i = 0; i < tough.Length; i++) if (tough[i]) return;
            goldMovesSinceTough++;
            int pity = toughSeen ? CrackPity : FirstCrackPity;
            if (goldMovesSinceTough < pity && crackRandom.NextDouble() >= Level.crackChance) return;
            tough[index] = true; toughSeen = true;
            goldMovesSinceTough = 0;
        }
    }
}
