using UnityEngine;

namespace LockSpin
{
    public sealed partial class LockSpinSession
    {
        public bool IsTutorial { get; private set; }
        public int TutorialStep { get; private set; }
        public bool TutorialGuided => IsTutorial && TutorialStep<3;
        public int LessonMistakes { get; private set; }
        float lessonTransition;
        int practiceStartHits;
        public int TutorialSafeHitsLeft=>IsTutorial && TutorialStep==3?Mathf.Max(0,2-(Hits-practiceStartHits)):0;
        public bool TutorialWarmup => TutorialSafeHitsLeft>0;

        public void BeginTutorial(int step)
        {
            IsTutorial=true; TutorialStep=Mathf.Clamp(step,0,3);
            firstPerfectCoach=false; ClearGuidedHold();
            lessonTransition=0;
            ConfigureLesson();
        }

        void ConfigureLesson()
        {
            practiceStartHits=Hits;
            ClearGuidedHold();
            Level.relocateHitSegment=false; Level.resizeHitSegment=false;
            Level.moveSegmentsAfterHit=false; Level.shrinkGoldAfterHit=false;
            Level.reverseOnMiss=false; Level.missSpeedResponse=false; Level.missLimit=0;
            Level.spinRule=SpinRule.Constant;
            Level.title=new[]{"GOLD","TIME","PERFECT","YOUR TURN"}[TutorialStep];
            Level.timeLimit=45; TimeLeft=45; Elapsed=0;
            Speed=Mathf.Min(Level.needleSpeed,SpeedLimit); Angle=180; direction=-1;
            latchedSegment=-1; cooldown=0; LessonMistakes=0;
            Level.segments.Clear();
            if(TutorialGuided)
                Level.segments.Add(new SegmentConfig(35,90,TutorialStep==1?SegmentKind.Blue:SegmentKind.Gold,.38f));
            else
            {
                Level.segments.Add(new SegmentConfig(30,70,SegmentKind.Gold));
                Level.segments.Add(new SegmentConfig(155,65,SegmentKind.Blue));
                Level.segments.Add(new SegmentConfig(265,65,SegmentKind.Gold));
            }
            Level.objective=ObjectiveKind.Hits; Level.objectiveTarget=Hits+6;
            Revision++;
        }

        bool TickLesson(float dt)
        {
            if(!IsTutorial) return false;
            if(lessonTransition>0)
            {
                lessonTransition=Mathf.Max(0,lessonTransition-dt);
                if(lessonTransition==0) ConfigureLesson();
                return true;
            }
            if(!TutorialGuided) return false;
            cooldown=Mathf.Max(0,cooldown-dt);
            // Teach the level's real rhythm; the guided hold supplies assistance, not slow motion.
            float travel=Speed*dt;
            Angle=CatchGuidedTap(travel,out _,out float stop)?stop:Mathf.Repeat(Angle+travel*direction,360);
            if(SegmentAt(Angle)!=latchedSegment) latchedSegment=-1;
            return true;
        }

        TapResult? TapLesson()
        {
            if(lessonTransition>0) return null;
            cooldown=.16f;
            int index=SegmentAt(Angle);
            var target=Level.segments[0];
            bool hit=index==0;
            bool perfect=hit && Mathf.Abs(Mathf.DeltaAngle(Angle,target.startAngle+target.arc*.5f))<=target.arc*target.perfectFraction*.5f;
            if(!hit || (TutorialStep==2 && !perfect))
            {
                LessonMistakes++;
                if(LessonMistakes>=2)
                {
                    float center=target.startAngle+target.arc*.5f;
                    target.arc=Mathf.Min(125,target.arc+10); target.startAngle=center-target.arc*.5f;
                    Revision++;
                }
                return new TapResult(hit?target.kind:SegmentKind.Neutral,perfect,hit,Angle);
            }
            ClearGuidedHold();
            Hits++; Combo++; if(perfect) Perfects++;
            if(target.kind==SegmentKind.Gold) Score+=perfect?25:10;
            else TimeLeft+=3;
            var result=new TapResult(target.kind,perfect,true,Angle);
            TutorialStep++; lessonTransition=.8f;
            return result;
        }
    }
}
