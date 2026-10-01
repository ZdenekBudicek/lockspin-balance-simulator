using UnityEngine;

namespace LockSpin
{
    // Pure session state: UI, persistence and effects do not decide tap outcomes.
    public sealed partial class LockSpinSession
    {
        public LevelConfig Level { get; }
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int ScoreMultiplier => Combo >= 10 ? 3 : Combo >= 5 ? 2 : 1;
        public int Hits { get; private set; }
        public int Perfects { get; private set; }
        public int PerfectStreak { get; private set; }
        public int Misses { get; private set; }
        public int Phase { get; private set; }
        public float TimeLeft { get; private set; }
        public float Elapsed { get; private set; }
        public float Angle { get; private set; } = 180;
        public float Speed { get; private set; }
        public bool Finished { get; private set; }
        public bool Won { get; private set; }
        public bool Paused { get; set; }
        public string Failure { get; private set; }
        public int Revision { get; private set; }
        public int LastRelocatedSegment { get; private set; } = -1;
        public int IdlePenalties { get; private set; }
        public float Direction => direction;
        public float CurrentSpeed => Mathf.Min(Speed * RampFactor * RecoveryFactor(recoveryAge),SpeedLimit)*SpeedZoneMultiplier;
        float SpeedLimit => Level.maxNeedleSpeed>0?Level.maxNeedleSpeed:128;
        public bool IsRecovering => recoveryAge < 1.5f;
        // 0..1 over roughly the last second of nominal travel before an idle-lap penalty.
        public float IdleWarning => IsTutorial || Finished || Level.idleLapPenalty <= 0 ? 0 :
            Mathf.Clamp01((idleTravel - 360 + IdleWarningArc) / IdleWarningArc);
        float IdleWarningArc => Mathf.Clamp(Speed, 60, 180);
        readonly float[] respawnTimers;
        readonly bool[] thawed;
        readonly System.Random random;
        float recoveryAge = 2;
        float direction = -1, cooldown, idleTravel;
        int latchedSegment = -1, passingSegment = -1;
        int phaseScore, phaseHits, phasePerfects;
        public bool HasBossPhases => Level.objective==ObjectiveKind.Boss && Level.bossPhases!=null && Level.bossPhases.Length>0;
        public BossPhaseConfig ActiveBossPhase => HasBossPhases?Level.bossPhases[Mathf.Min(Phase,Level.bossPhases.Length-1)]:null;
        public int BossPhaseProgress => IsBossEncounter && Phase>0?Mathf.Max(0,(Phase==1?Level.bossHealth:Level.bossHealth/2)-BossHealth):ActiveBossPhase==null?Score%700:
            ActiveBossPhase.objective==ObjectiveKind.Hits?Hits-phaseHits:
            ActiveBossPhase.objective==ObjectiveKind.Perfects?Perfects-phasePerfects:Score-phaseScore;
        public string BossPhaseGoal => ActiveBossPhase==null?"SCORE 700":ActiveBossPhase.title;

        public LockSpinSession(LevelConfig config, int seed = 0)
        {
            Level = JsonUtility.FromJson<LevelConfig>(JsonUtility.ToJson(config));
            InitRush(seed == 0 ? config.id * 7919 : seed);
            respawnTimers = new float[Level.segments.Count];
            thawed = new bool[Level.segments.Count];
            random = new System.Random(seed == 0 ? config.id * 7919 : seed);
            TimeLeft = Level.timeLimit;
            Speed = Mathf.Min(Level.needleSpeed,SpeedLimit);
            BossHealth=Mathf.Max(1,Level.bossHealth);
            PlayerHealth=Mathf.Max(1,Level.playerHealth);
            ApplyBossPhase();
            InitCracks(seed == 0 ? config.id * 7919 : seed);
            ApplyPerfectWindows();
        }

        public void Tick(float dt)
        {
            if (Paused || Finished || WaitingForGuidedTap || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if(TickLesson(dt)) return;
            // Never grant survival time after its budget has already run out.
            dt=Mathf.Min(dt,Mathf.Max(0,Mathf.Min(TimeLeft,150-Elapsed)));
            if(Level.objective==ObjectiveKind.Survival) dt=Mathf.Min(dt,Mathf.Max(0,Level.objectiveTarget-Elapsed));
            float travel = ZoneTravel(Mathf.Min(Speed * RampFactor * RecoveryFactor(recoveryAge+dt*.5f),SpeedLimit),dt);
            bool guidedStop=CatchGuidedTap(travel,out float guidedFraction,out float guidedAngle);
            if(guidedStop) { dt*=guidedFraction; travel*=guidedFraction; }
            Elapsed += dt;
            crystalWeakness=Mathf.Max(0,crystalWeakness-dt);
            cooldown = Mathf.Max(0, cooldown - dt);
            if(!TutorialWarmup) TimeLeft = Mathf.Max(0, Mathf.Min(TimeLeft - dt, 150 - Elapsed));
            else Elapsed=0;
            if(!TapGuideEnabled) travel=ZoneTravel(Mathf.Min(Speed * RampFactor * RecoveryFactor(recoveryAge+dt*.5f),SpeedLimit),dt);
            Angle = guidedStop?guidedAngle:Mathf.Repeat(Angle + travel * direction, 360);
            idleTravel += travel;
            recoveryAge += dt;
            if(!guidedStop) ApplyPerfectWindows();
            for(int i=0;i<respawnTimers.Length;i++)
            {
                if(respawnTimers[i]<=0) continue;
                respawnTimers[i]=Mathf.Max(0,respawnTimers[i]-dt);
                var waiting=Level.segments[i];
                if(respawnTimers[i]<=0 && Mathf.Abs(Mathf.DeltaAngle(Angle,waiting.startAngle+waiting.arc*.5f))<waiting.arc*.5f+10)
                    respawnTimers[i]=.05f;
            }
            TickRush(travel);
            int current = SegmentAt(Angle);
            // A target the needle rode all the way across without a tap costs one combo step.
            if (!IsTutorial && current != passingSegment && passingSegment >= 0 && passingSegment != latchedSegment &&
                Level.segments[passingSegment].kind != SegmentKind.Neutral && Level.segments[passingSegment].kind != SegmentKind.Avoid && PassedOver(passingSegment))
                Combo = Mathf.Max(0, Combo - 1);
            passingSegment = current;
            if (current != latchedSegment) latchedSegment = -1;
            while (!Finished && !IsTutorial && Level.idleLapPenalty > 0 && idleTravel >= 360) PunishIdleLap();
            if(Finished) { EndRush(RushSlotState.Cancelled); return; }
            if(Level.objective==ObjectiveKind.Survival && Elapsed>=Level.objectiveTarget-.0001f)
            {
                Won=true; Finished=true; Failure=null;
            }
            else if (TimeLeft <= 0 || Elapsed >= 150)
            {
                Finished = true;
                Won = false;
                Failure = "OUT OF TIME";
            }
            if(Finished) EndRush(RushSlotState.Cancelled);
        }

        // Left through its trailing edge in the direction of travel, not vanished or relocated from under the needle.
        bool PassedOver(int index)
        {
            var s = Level.segments[index];
            float d = Mathf.Repeat(Angle - s.startAngle, 360);
            return direction > 0 ? d >= s.arc && d < s.arc + 45 : d >= 315;
        }

        // Optional custom-level penalty: only a successful hit resets it, never a cheap miss.
        // A whole lap without a tap also breaks the combo.
        void PunishIdleLap()
        {
            idleTravel -= 360;
            IdlePenalties++;
            Combo = 0;
            TimeLeft = Mathf.Max(0, TimeLeft - Level.idleLapPenalty);
            if (TimeLeft <= 0)
            {
                Finished = true;
                Failure = "TOO SLOW";
            }
        }

        public int SegmentAt(float angle)
        {
            for (int i = 0; i < Level.segments.Count; i++)
                if (IsSegmentActive(i) && Mathf.Repeat(angle - Level.segments[i].startAngle, 360) < Level.segments[i].arc) return i;
            return -1;
        }

        public TapResult? Tap()
        {
            if (Paused || Finished || cooldown > 0) return null;
            if(IsTutorial && lessonTransition>0) return null;
            if(TutorialGuided) return TapLesson();
            LastRelocatedSegment = -1;
            IceCracked=false; SegmentCracked=false; SegmentShattered=false;
            RushStarted=false; RushCleared=false;
            int index = SegmentAt(Angle);
            if (index >= 0 && index == latchedSegment) return null;
            cooldown = .16f;
            RecordTapTiming();
            var segment = index < 0 ? null : Level.segments[index];
            bool hit = segment != null && segment.kind != SegmentKind.Neutral && segment.kind != SegmentKind.Avoid;
            bool perfect = hit && Mathf.Abs(Mathf.DeltaAngle(Angle, segment.startAngle + segment.arc * .5f))
                <= segment.arc * segment.perfectFraction * .5f;
            ClearGuidedHold();
            if(perfect) firstPerfectCoach=false;
            var result = new TapResult(segment == null ? SegmentKind.Neutral : segment.kind, perfect, hit, Angle);
            if (!hit)
            {
                ClassifyMiss();
                Combo = 0; PerfectStreak=0; if(!TutorialWarmup) Misses++;
                if(BossCombatActive)
                {
                    PlayerHealth=Mathf.Max(0,PlayerHealth-1);
                    if(PlayerHealth==0) { Finished=true; Failure="GUARDIAN STRIKE"; }
                }
                else if(!TutorialWarmup) TimeLeft = Mathf.Max(0, TimeLeft - (segment!=null && segment.kind==SegmentKind.Avoid?3:2));
                if (TimeLeft <= 0 || (Level.UsesMissLimit && Misses >= Level.missLimit))
                {
                    Finished = true;
                    Failure = TimeLeft <= 0 ? "OUT OF TIME" : "TOO MANY MISSES";
                }
                if(!Finished)
                {
                    if(Level.reverseOnMiss) direction *= -1;
                    if(Level.missSpeedResponse && !IsRecovering) recoveryAge = 0;
                }
                // A tap into a gap ends the rush; the miss itself is the whole price.
                EndRush(RushSlotState.Cancelled);
                return result;
            }
            latchedSegment = index;
            idleTravel = 0;
            IceCracked=false;
            if(IsFrozenSegment(index))
            {
                frozenSegment=-1; thawed[index]=true; IceCracked=true; Revision++;
                return result;
            }
            bool crackHit=CracksEnabled && IsTough(index), shatterHit=IsCracked(index), rushHit=IsRushSlot(index);
            int phaseBefore=Phase;
            Hits++; Combo=Level.perfectCombo && !perfect?0:Combo+1;
            BestCombo=Mathf.Max(BestCombo,Combo);
            PerfectStreak=perfect?PerfectStreak+1:0;
            if (perfect) Perfects++;
            if (AwardsPoints(segment.kind))
                Score += (perfect ? 25 : 10) * ScoreMultiplier * (shatterHit ? 2 : 1) * (segment.kind==SegmentKind.Bonus?2:1);
            else if(segment.kind==SegmentKind.Blue) TimeLeft = Mathf.Min(150 - Elapsed, Mathf.Min(Level.timeLimit + 12, TimeLeft + 3));
            if (rushHit) RushTargetHit(index);

            // Successful hits preserve travel, including legacy FlipAfterHit configurations.
            if (Level.spinRule == SpinRule.SpeedUpOnHit) Speed = Mathf.Min(SpeedLimit, Speed + 3);
            if (!Level.relocateHitSegment && (Level.moveSegmentsAfterHit || Level.shrinkGoldAfterHit))
            {
                for (int i = 0; i < Level.segments.Count; i++)
                {
                    if (IsRushSlot(i)) continue;
                    var s = Level.segments[i];
                    if (Level.moveSegmentsAfterHit) s.startAngle = Mathf.Repeat(s.startAngle + 31, 360);
                    if (Level.shrinkGoldAfterHit && s.kind == SegmentKind.Gold) s.arc = Mathf.Max(22, s.arc - 1);
                }
                Revision++;
            }
            if (Level.objective == ObjectiveKind.Boss)
            {
                if(IsBossEncounter) AdvanceBossEncounter(segment.kind,perfect);
                else if(HasBossPhases)
                {
                    if(BossPhaseProgress>=ActiveBossPhase.target)
                    {
                        Phase++;
                        phaseScore=Score; phaseHits=Hits; phasePerfects=Perfects;
                        if(Phase<Level.bossPhases.Length) { ApplyBossPhase(); TimeLeft=Mathf.Min(150-Elapsed,Mathf.Min(Level.timeLimit,TimeLeft+6)); }
                    }
                }
                else
                {
                    int next = Mathf.Min(Level.phases, Score / 700);
                    if (next > Phase) { Phase = next; Speed = Mathf.Min(SpeedLimit, Speed + 6); TimeLeft = Mathf.Min(150-Elapsed,Mathf.Min(Level.timeLimit, TimeLeft + 6)); }
                }
            }
            Won = Progress >= Level.objectiveTarget;
            Finished = Won;
            if(Finished) EndRush(RushSlotState.Cancelled);
            // A tough target cracks in place and turns the needle back; a cracked one breaks and moves on.
            if(crackHit) { cracked[index]=true; SegmentCracked=true; Revision++; if(!Finished) direction*=-1; }
            if(shatterHit) { tough[index]=cracked[index]=false; SegmentShattered=true; }
            // A rush target is spent where it stands.
            if(!Finished && Level.relocateHitSegment && !crackHit && !rushHit)
            {
                Relocate(index);
                thawed[index]=false;
                if(LastRelocatedSegment==index) RollTough(index);
            }
            // Big moments never overlap: no rush on a crack, a shatter or a phase change.
            if(!rushHit && !crackHit && !shatterHit && Phase==phaseBefore) MaybeStartRush();
            if(BossCombatActive && Level.dailyBoss==DailyBossKind.Ice && frozenSegment<0 && segment.kind==SegmentKind.Gold)
            { frozenSegment=index; Revision++; }
            return result;
        }

        void ApplyBossPhase()
        {
            if(!HasBossPhases) return;
            // A new phase clears the dial of any rush still running.
            EndRush(RushSlotState.Cancelled);
            var phase=ActiveBossPhase;
            Level.spinRule=phase.spinRule;
            Level.resizeHitSegment=phase.resizeTargets;
            Level.minSegmentArc=phase.minArc; Level.maxSegmentArc=phase.maxArc;
            Speed=Mathf.Min(phase.needleSpeed,SpeedLimit);
            recoveryAge=2;
            ApplyPerfectWindows();
        }

        public bool IsSegmentActive(int index) => index>=0 && index<Level.segments.Count &&
            (IsTutorial || (index<respawnTimers.Length && respawnTimers[index]<=0 && RushAllows(index)));

        static float RecoveryFactor(float age)
        {
            if(age>=1.5f) return 1;
            if(age<.12f) return .28f;
            if(age<1.05f) return Mathf.Lerp(.28f,1.18f,Mathf.SmoothStep(0,1,(age-.12f)/.93f));
            return Mathf.Lerp(1.18f,1,Mathf.SmoothStep(0,1,(age-1.05f)/.45f));
        }

        void Relocate(int index)
        {
            var target=Level.segments[index];
            float minimum=Mathf.Clamp(Level.minSegmentArc,18,80);
            float maximum=Mathf.Clamp(Level.maxSegmentArc,minimum,100);
            float desired=Level.resizeHitSegment?Mathf.Lerp(minimum,maximum,(float)random.NextDouble()):target.arc;
            if(Level.resizeHitSegment && Mathf.Abs(desired-target.arc)<4)
                desired=target.arc>(minimum+maximum)*.5f?minimum:maximum;
            float oldCenter=target.startAngle+target.arc*.5f;
            int offset=random.Next(360);
            // Reserve all segments, including those still respawning. Search the whole circle
            // before relaxing; never overlap another target or spawn under the needle.
            // Passes: 0 desired width well clear of the old spot, 1 minimum width (resizing levels),
            // 2 desired width at least 50 degrees away, 3 a slightly narrower target 50 degrees away.
            // A move under 50 degrees reads as a twitch, not a new target.
            float delay=Mathf.Clamp(Level.segmentRespawnDelay,.15f,.45f);
            for(int pass=0;pass<4;pass++)
            {
                if(pass==1 && !Level.resizeHitSegment) continue;
                float width=pass==1?minimum:pass==3?Mathf.Max(18,desired*.85f):desired;
                float oldGap=pass<2?Mathf.Max(50,(target.arc+width)*.5f+8):50;
                for(int attempt=0;attempt<360;attempt++)
                {
                    float center=Mathf.Repeat(offset+attempt*137,360);
                    if(Mathf.Abs(Mathf.DeltaAngle(center,oldCenter))<oldGap) continue;
                    if(Mathf.Abs(Mathf.DeltaAngle(center,Angle))<width*.5f+22) continue;
                    float predicted=Angle+CurrentSpeed*direction*delay;
                    if(Mathf.Abs(Mathf.DeltaAngle(center,predicted))<width*.5f+22) continue;
                    bool clear=true;
                    for(int j=0;j<Level.segments.Count;j++)
                    {
                        // Rush slots only live while the normal targets are hidden.
                        if(j==index || IsRushSlot(j)) continue;
                        var other=Level.segments[j];
                        if(Mathf.Abs(Mathf.DeltaAngle(center,other.startAngle+other.arc*.5f))<(width+other.arc)*.5f+8)
                        { clear=false; break; }
                    }
                    if(!clear) continue;
                    target.startAngle=Mathf.Repeat(center-width*.5f,360); target.arc=width;
                    ApplyPerfectWindow(index);
                    respawnTimers[index]=delay;
                    LastRelocatedSegment=index; Revision++; return;
                }
            }
            // No room anywhere: the target still breaks and comes back where it was once the needle has
            // left it (the respawn guard in Tick holds it while the needle is close). A hit never
            // leaves a target standing, and a packed custom layout never loses its target.
            respawnTimers[index]=delay;
            LastRelocatedSegment=index; Revision++;
        }

        public float Progress => Level.objective == ObjectiveKind.Hits ? Hits :
            Level.objective == ObjectiveKind.Perfects ? Perfects :
            Level.objective == ObjectiveKind.Combo ? Combo :
            Level.objective == ObjectiveKind.Boss ? Phase :
            Level.objective == ObjectiveKind.Survival ? Elapsed : Score;
        public int Stars => !Won ? 0 : Misses <= Level.threeStarMisses && Perfects >= Level.threeStarPerfects ? 3 : Misses <= Level.twoStarMisses ? 2 : 1;
        public string StarRequirement => $"3 STARS: {Level.threeStarMisses} MISSES"+(Level.threeStarPerfects>0?$" + {Level.threeStarPerfects} PERFECTS":"");
        public string StarFeedback => "FOR 3 STARS: "+(Misses>Level.threeStarMisses?
            $"{Misses-Level.threeStarMisses} FEWER MISSES":"")+
            (Misses>Level.threeStarMisses && Perfects<Level.threeStarPerfects?" + ":"")+
            (Perfects<Level.threeStarPerfects?$"{Level.threeStarPerfects-Perfects} MORE PERFECTS":"");
        public string Objective => HasBossPhases && !Won ? $"{ActiveBossPhase.objective.ToString().ToUpperInvariant()} {BossPhaseProgress}/{ActiveBossPhase.target}  |  PHASE {Phase+1}/{Level.phases}" :
            Level.objective == ObjectiveKind.Boss ? $"PHASE {Phase} / {Level.phases}" :
            $"{Level.objective.ToString().ToUpperInvariant()}  {Progress:0} / {Level.objectiveTarget:0}";
    }
}
