using UnityEngine;

namespace LockSpin
{
    public enum RushSlotState { Idle, Live, Hit, Lapsed, Cancelled }

    // Rush wave: now and then the normal targets step aside and a packed run of small gold targets comes in
    // just ahead of the needle. Each one hit scores as gold; each one the needle passes costs time.
    public sealed partial class LockSpinSession
    {
        public const float RushArc = 28, RushGap = 6, RushStagger = .06f, RushTimeCost = 2, RushClearBonus = 2;
        const float RushPassMargin = 2;
        const int RushSpacing = 5, RushPity = 9, FirstRushSpacing = 2, FirstRushPity = 3;
        int rushFirst = -1, hitsSinceRush;
        RushSlotState[] rushSlots = new RushSlotState[0];
        float[] rushPassAt = new float[0];
        float rushTravel;
        bool rushSeen;
        System.Random rushRandom;
        public bool RushActive { get; private set; }
        public bool RushStarted { get; private set; }
        public bool RushCleared { get; private set; }
        public int RushHits { get; private set; }
        public int RushCount { get; private set; }
        public int RushLapses { get; private set; }
        public int RushSize => rushSlots.Length;
        public bool RushEnabled => rushFirst >= 0 && !IsTutorial;
        public bool IsRushSlot(int index) => RushEnabled && index >= rushFirst && index < rushFirst + rushSlots.Length;
        // 0 for the slot the needle reaches first, -1 for a normal target.
        public int RushOrder(int index) => IsRushSlot(index) ? index - rushFirst : -1;
        public RushSlotState RushState(int index) => IsRushSlot(index) ? rushSlots[index - rushFirst] : RushSlotState.Idle;

        // Runs before the per-target arrays are sized: the slots sit after the normal targets, so no index ever shifts.
        void InitRush(int seed)
        {
            // A separate stream keeps the relocation and crack sequences of every level unchanged.
            rushRandom = new System.Random(seed ^ 0x5A17E);
            if (Level.rushChance <= 0 || Level.rushSize <= 0 || Level.dailyBoss != DailyBossKind.None) return;
            rushFirst = Level.segments.Count;
            int size = Mathf.Clamp(Level.rushSize, 2, 6);
            rushSlots = new RushSlotState[size];
            rushPassAt = new float[size];
            float perfect = rushFirst > 0 ? Level.segments[0].perfectFraction : .3f;
            for (int i = 0; i < size; i++) Level.segments.Add(new SegmentConfig(0, RushArc, SegmentKind.Gold, perfect));
        }

        // Normal targets step aside while a rush runs; a rush slot shows only while it is live.
        bool RushAllows(int index) => rushFirst < 0 ? true :
            index < rushFirst ? !RushActive : rushSlots[index - rushFirst] == RushSlotState.Live;

        // After a plain hit on a normal target. The first rush comes early, so every run shows it.
        void MaybeStartRush()
        {
            if (!RushEnabled || RushActive || Finished) return;
            hitsSinceRush++;
            if (hitsSinceRush < (rushSeen ? RushSpacing : FirstRushSpacing)) return;
            bool roll = rushRandom.NextDouble() < Level.rushChance;
            if (!roll && hitsSinceRush < (rushSeen ? RushPity : FirstRushPity)) return;
            StartRush();
        }

        void StartRush()
        {
            RushActive = RushStarted = rushSeen = true;
            RushCount++; RushHits = 0; hitsSinceRush = 0; rushTravel = 0;
            // Far enough ahead to read (about 0.6 s of travel); the run extends in the direction of travel.
            float lead = Mathf.Clamp(CurrentSpeed * .6f, 55, 80);
            for (int i = 0; i < rushSlots.Length; i++)
            {
                float near = lead + i * (RushArc + RushGap);
                var slot = Level.segments[rushFirst + i];
                slot.startAngle = Mathf.Repeat(Angle + direction * (near + RushArc * .5f) - RushArc * .5f, 360);
                slot.arc = RushArc;
                ApplyPerfectWindow(rushFirst + i);
                rushSlots[i] = RushSlotState.Live;
                rushPassAt[i] = near + RushArc + RushPassMargin;
                // They pop in one after another, in the order the needle will reach them.
                respawnTimers[rushFirst + i] = RushStagger * (i + 1);
            }
            Revision++;
        }

        void TickRush(float travel)
        {
            if (!RushActive) return;
            rushTravel += travel;
            bool live = false;
            for (int i = 0; i < rushSlots.Length; i++)
            {
                if (rushSlots[i] != RushSlotState.Live) continue;
                if (rushTravel < rushPassAt[i]) { live = true; continue; }
                // Passed without a tap: it costs the same time as a miss, but it is not a miss.
                rushSlots[i] = RushSlotState.Lapsed; RushLapses++;
                TimeLeft = Mathf.Max(0, TimeLeft - RushTimeCost);
            }
            if (!live) EndRush(RushSlotState.Cancelled);
        }

        void RushTargetHit(int index)
        {
            rushSlots[index - rushFirst] = RushSlotState.Hit;
            RushHits++;
            if (RushHits == rushSlots.Length)
            {
                RushCleared = true;
                TimeLeft = Mathf.Min(150 - Elapsed, Mathf.Min(Level.timeLimit + 12, TimeLeft + RushClearBonus));
            }
            foreach (var state in rushSlots) if (state == RushSlotState.Live) return;
            EndRush(RushSlotState.Cancelled);
        }

        // Live slots leave in the given state and the normal targets come back one after another.
        // No Revision bump: the screen animates each slot's exit itself.
        void EndRush(RushSlotState remaining)
        {
            if (!RushActive) return;
            RushActive = false;
            for (int i = 0; i < rushSlots.Length; i++)
            {
                if (rushSlots[i] == RushSlotState.Live) rushSlots[i] = remaining;
                respawnTimers[rushFirst + i] = 0;
            }
            for (int i = 0, order = 1; i < rushFirst; i++) respawnTimers[i] = Mathf.Max(respawnTimers[i], RushStagger * order++);
            // A wave the player let pass never feeds the idle-lap penalty.
            idleTravel = 0;
        }
    }
}
