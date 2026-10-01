using UnityEngine;

namespace LockSpin
{
    public sealed partial class LockSpinSession
    {
        public bool WaitingForGuidedTap { get; private set; }
        public int GuidedSegment { get; private set; }=-1;
        public bool TapGuideEnabled => TutorialGuided || firstPerfectCoach;
        bool firstPerfectCoach;

        public void EnableFirstPerfectCoach()
        {
            if(!IsTutorial && Level.id==1 && !IsBossEncounter && Perfects==0)
                firstPerfectCoach=true;
        }

        void ClearGuidedHold()
        {
            WaitingForGuidedTap=false;
            GuidedSegment=-1;
        }

        // Sweep the actual travel, including wraparound and long frames. Stop 75% through the
        // painted Perfect window in the current direction, without synthesizing a tap or a reward.
        bool CatchGuidedTap(float travel,out float fraction,out float stopAngle)
        {
            fraction=1; stopAngle=Angle;
            if(!TapGuideEnabled || travel<=0) return false;
            float nearest=float.MaxValue;
            int chosen=-1;
            for(int i=0;i<Level.segments.Count;i++)
            {
                var target=Level.segments[i];
                if(!IsSegmentActive(i) || i==latchedSegment ||
                    (target.kind!=SegmentKind.Gold && target.kind!=SegmentKind.Blue) || target.perfectFraction<=0) continue;
                float stop=Mathf.Repeat(target.startAngle+target.arc*.5f+direction*target.arc*target.perfectFraction*.25f,360);
                float distance=Mathf.Repeat((stop-Angle)*direction,360);
                if(distance>travel || distance>=nearest) continue;
                nearest=distance; chosen=i; stopAngle=stop;
            }
            if(chosen<0) return false;
            fraction=Mathf.Clamp01(nearest/travel);
            WaitingForGuidedTap=true; GuidedSegment=chosen;
            cooldown=0;
            return true;
        }
    }
}
