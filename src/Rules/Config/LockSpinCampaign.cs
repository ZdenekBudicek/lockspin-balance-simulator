using UnityEngine;

namespace LockSpin
{
    [CreateAssetMenu(menuName = "Lock Spin/Campaign", fileName = "Campaign")]
    public sealed class LockSpinCampaign : ScriptableObject
    {
        public LevelConfig[] levels;

        // Island 1 as tuned by the balance system (README.md), layered on the frozen legacy values.
        public static LevelConfig[] Defaults()
        {
            var result = LegacyDefaults();
            LockSpinIslandOne.Apply(result);
            return LockSpinExpansionCampaign.Append(result);
        }

        // The 2026-09-27 campaign, frozen: the vault, daily challenge, weekly events and daily boss are
        // built from these, so retuning the campaign never changes them.
        public static LevelConfig[] LegacyDefaults()
        {
            var source = LockSpinLevelLibrary.CreateLevels();
            var result = new LevelConfig[source.Count];
            int[] targets = { 300, 550, 650, 12, 12, 900, 24, 1100, 32, 3 };
            float[] speeds = { 90, 95, 100, 98, 100, 104, 106, 108, 110, 112 };
            float[] times = { 45, 48, 32, 55, 55, 55, 60, 60, 65, 80 };
            int[] perfects = { 1, 2, 0, 6, 0, 3, 2, 3, 2, 5 };
            float[] cracks = { 0, 0, 0, 0, .30f, .30f, .32f, .34f, .36f, 0 };
            float[] rushes = { 0, 0, 0, 0, 0, .25f, .25f, .25f, .25f, .25f };
            int[] rushSizes = { 0, 0, 0, 0, 0, 3, 3, 4, 4, 4 };
            string[] titles = { "First Click", "Moving Targets", "Time Saver", "Perfect Practice", "Combo Lock",
                "Reverse Recovery", "Steady Rhythm", "Shifting Windows", "Steady Hands", "Castle Guardian" };
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = source[i];
                result[i].objectiveTarget = targets[i];
                result[i].needleSpeed = speeds[i];
                result[i].timeLimit = times[i];
                result[i].title = titles[i];
                result[i].relocateHitSegment = true;
                result[i].resizeHitSegment = i>=7;
                // Core rule: a miss always turns the needle around. Levels 6 and 8 add the slow recovery.
                result[i].reverseOnMiss = true;
                result[i].missSpeedResponse = i==5 || i==7;
                result[i].minSegmentArc = i<7?34:36;
                result[i].maxSegmentArc = i<7?70:58;
                result[i].idleLapPenalty = 10;
                result[i].maxNeedleSpeed = 128;
                result[i].threeStarPerfects = perfects[i];
                result[i].threeStarMisses = 0;
                result[i].twoStarMisses = 3;
                result[i].missLimit = i==8?5:0;
                result[i].segmentRespawnDelay = .30f;
                result[i].moveSegmentsAfterHit = false;
                result[i].shrinkGoldAfterHit = false;
                result[i].spinRule=SpinRule.Constant;
                // Levels 5-9: some gold targets crack on the first hit and shatter on the second.
                result[i].crackChance=cracks[i];
                // Levels 6-10: now and then a packed rush of small gold targets asks for quick taps.
                result[i].rushChance=rushes[i]; result[i].rushSize=rushSizes[i];
                if(i==6) result[i].objective=ObjectiveKind.Hits;
                foreach(var segment in result[i].segments)
                    segment.perfectFraction=i==3?.38f:.30f;
            }
            result[9].bossEncounter=true;
            result[9].bossPhases=new[] {
                new BossPhaseConfig { title="SCORE 550", objective=ObjectiveKind.Score, target=550, needleSpeed=108, minArc=38, maxArc=58 },
                new BossPhaseConfig { title="BREAK THE GUARDIAN", objective=ObjectiveKind.Score, target=200, needleSpeed=108, minArc=38, maxArc=58 },
                new BossPhaseConfig { title="SEAL THE GUARDIAN", objective=ObjectiveKind.Score, target=200, needleSpeed=112, spinRule=SpinRule.Constant, minArc=36, maxArc=52 }
            };
            return result;
        }
    }

    public static class LockSpinProgress
    {
        const string Prefix = "LockSpin.Campaign.v1.";
        public static int TutorialStep => Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"tutorial",0),0,4);
        public static void SaveTutorial(int step)
        {
            PlayerPrefs.SetInt(Prefix+"tutorial",Mathf.Max(TutorialStep,Mathf.Clamp(step,0,4)));
            PlayerPrefs.Save();
        }
        public static int LevelCount { get { var asset=Resources.Load<LockSpinCampaign>("LockSpinCampaign"); return asset!=null && asset.levels!=null && asset.levels.Length>0?asset.levels.Length:LockSpinCampaign.Defaults().Length; } }
        public static int Unlocked {
            get {
                int count=LevelCount, unlocked=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"unlocked",1),1,count);
                // Existing players who completed the old final level can enter newly released content.
                while(unlocked<count && Stars(unlocked)>0) unlocked++;
                return unlocked;
            }
        }
        public static int Stars(int level) => PlayerPrefs.GetInt(Prefix + "stars." + level, 0);
        public static int Best(int level) => PlayerPrefs.GetInt(Prefix + "best." + level, 0);
        public static int TotalStars { get { int total = 0; for (int i=1;i<=LevelCount;i++) total += Stars(i); return total; } }
        // Campaign losses in a row per level; drives LockSpinAssist. A win resets it.
        public static int Losses(int level) => PlayerPrefs.GetInt(Prefix + "losses." + level, 0);
        public static void RecordLoss(int level)
        {
            PlayerPrefs.SetInt(Prefix + "losses." + level, Losses(level) + 1);
            PlayerPrefs.Save();
        }
        public static void Complete(int level, int stars, int score)
        {
            PlayerPrefs.DeleteKey(Prefix + "losses." + level);
            PlayerPrefs.SetInt(Prefix + "unlocked", Mathf.Max(Unlocked, Mathf.Min(LevelCount, level + 1)));
            PlayerPrefs.SetInt(Prefix + "stars." + level, Mathf.Max(Stars(level), stars));
            PlayerPrefs.SetInt(Prefix + "best." + level, Mathf.Max(Best(level), score));
            PlayerPrefs.Save();
        }
        public static bool Option(int index) => PlayerPrefs.GetInt(Prefix + "option." + index, 1) != 0;
        public static void SaveOption(int index, bool value)
        {
            PlayerPrefs.SetInt(Prefix + "option." + index, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
