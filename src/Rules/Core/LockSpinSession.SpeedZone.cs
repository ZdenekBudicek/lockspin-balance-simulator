using UnityEngine;

namespace LockSpin
{
    public sealed partial class LockSpinSession
    {
        public float SpeedZoneMultiplier => ZoneMultiplierAt(Angle);

        public float ZoneMultiplierAt(float angle)
        {
            float result=1;
            if(Level.speedZones==null || IsTutorial) return result;
            foreach(var zone in Level.speedZones)
            {
                if(zone==null || !Finite(zone.startAngle) || !Finite(zone.arc) ||
                    !Finite(zone.multiplier) || !Finite(zone.featherDegrees) || zone.arc<=0) continue;
                float arc=Mathf.Clamp(zone.arc,10,180);
                float along=Mathf.Repeat(angle-zone.startAngle,360);
                if(along>=arc) continue;
                float feather=Mathf.Clamp(zone.featherDegrees,1,arc*.5f);
                float weight=Mathf.SmoothStep(0,1,Mathf.Min(along,arc-along)/feather);
                // Overlaps take the stronger zone, never compound multipliers.
                result=Mathf.Max(result,Mathf.Lerp(1,Mathf.Clamp(zone.multiplier,1,1.75f),weight));
            }
            return result;
        }

        static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);

        float ZoneTravel(float baseSpeed,float dt)
        {
            if(Level.speedZones==null || Level.speedZones.Length==0 || IsTutorial) return baseSpeed*dt;
            if(baseSpeed<=0 || dt<=0) return 0;
            // Midpoint integration below half a degree: stable at 30/60/120 Hz, including wrapped arcs.
            int steps=Mathf.Max(1,Mathf.CeilToInt(baseSpeed*1.75f*dt/.5f));
            float step=dt/steps, travel=0;
            for(int i=0;i<steps;i++)
            {
                float angle=Angle+direction*travel;
                float half=baseSpeed*ZoneMultiplierAt(angle)*step*.5f;
                travel+=baseSpeed*ZoneMultiplierAt(angle+direction*half)*step;
            }
            return travel;
        }
    }
}
