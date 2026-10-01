using UnityEngine;

namespace LockSpin
{
    public sealed partial class LockSpinSession
    {
        public bool IsBossEncounter => Level.bossEncounter && HasBossPhases;
        public bool BossCombatActive => IsBossEncounter && Phase>0 && Phase<Level.bossPhases.Length;
        public static bool AwardsPoints(SegmentKind kind)=>kind==SegmentKind.Gold || kind==SegmentKind.Bonus || kind==SegmentKind.Frozen;
        public int BossHealth { get; private set; }
        public int PlayerHealth { get; private set; }
        float crystalWeakness;
        int frozenSegment=-1;
        public bool IceCracked { get; private set; }
        public int ClockCharges { get; private set; }
        public bool CrystalShielded=>BossCombatActive && Level.dailyBoss==DailyBossKind.Crystal && crystalWeakness<=0;
        public float CrystalWeakness=>crystalWeakness;
        public bool IsFrozenSegment(int index)=>index>=0 && index<Level.segments.Count &&
            ((Level.segments[index].kind==SegmentKind.Frozen && !thawed[index]) ||
            (BossCombatActive && index==frozenSegment && Level.dailyBoss==DailyBossKind.Ice));

        void AdvanceBossEncounter(SegmentKind kind,bool perfect)
        {
            int next=Phase;
            if(Phase==0)
            {
                if(Score>=Level.bossPhases[0].target) next=1;
            }
            else
            {
                int damage=0;
                if(AwardsPoints(kind))
                {
                    damage=perfect?Level.bossPerfectDamage:Level.bossHitDamage;
                    if(Level.dailyBoss==DailyBossKind.Crystal)
                    {
                        if(perfect) crystalWeakness=6;
                        damage=crystalWeakness>0?damage*2:3;
                    }
                }
                else if(kind==SegmentKind.Blue && Level.dailyBoss==DailyBossKind.Clockwork)
                {
                    ClockCharges++;
                    if(ClockCharges==3) { ClockCharges=0; damage=60; }
                }
                BossHealth=Mathf.Max(0,BossHealth-damage);
                int final=Level.bossPhases.Length;
                if(BossHealth==0) next=final;
                else next=Mathf.Max(Phase,Mathf.Min(final-1,1+Mathf.FloorToInt((1-BossHealth/(float)Level.bossHealth)*(final-1))));
            }
            if(next==Phase) return;
            Phase=next;
            if(Phase==1 && Level.dailyBoss==DailyBossKind.Ice)
            { frozenSegment=Level.segments.FindIndex(s=>s.kind==SegmentKind.Gold); Revision++; }
            phaseScore=Score; phaseHits=Hits; phasePerfects=Perfects;
            if(Phase>=Level.bossPhases.Length) return;
            ApplyBossPhase();
            TimeLeft=Mathf.Min(150-Elapsed,Mathf.Min(Level.timeLimit,TimeLeft+6));
        }
    }
}
