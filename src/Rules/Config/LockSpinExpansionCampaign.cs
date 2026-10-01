using System;
using UnityEngine;

namespace LockSpin
{
    // Authored encounters, not a speed ladder. Each chapter includes breathing room after its challenges.
    public static class LockSpinExpansionCampaign
    {
        public static readonly string[] WorldKeys = { "castle", "forest", "mountains", "coast", "island", "city" };
        public static readonly string[] WorldTitles = { "ROYAL CASTLE", "THORNWOOD", "FROZEN HEIGHTS", "TIDAL COAST", "SUN TEMPLE", "ECLIPSE CITY" };
        public static readonly string[] BossNames = { "THE USURPER", "THORN QUEEN", "ICE SCORPION", "THE KRAKEN", "SUN IDOL", "ECLIPSE EMPRESS" };
        public static readonly string[][] Locations ={
            new[]{"Castle Gate","Courtyard","Fountain","Guard Hall","Great Hall","Dining Hall","Library","Armory","Royal Corridor","Treasury Entrance","Treasury","Tower Stairs","Tower","Kings Chamber","Royal Vault"},
            new[]{"Outskirts","Forest Entrance","Woodland Path","River","Bridge","Deep Forest","Ancient Tree","Clearing","Ruins","Broken Shrine","Grove","Standing Stones","Forgotten Path","Shrine Entrance","Thorn Shrine"},
            new[]{"Foothills","Rocky Path","Trail","Cliff","Rope Bridge","Mining Camp","Mine Entrance","Old Mine","Crystal Cave","Deep Cave","Mountain Pass","Snow Line","Frozen Pass","Summit Path","Summit"},
            new[]{"Descent","Coastal Road","Cliffs","Lighthouse Path","Lighthouse","Village","Harbor Road","Harbor","Dockyard","Warehouse","Shipyard","Pier","Ship Deck","Captains Lock","Departure"},
            new[]{"Beach","Palm Trail","Jungle Entrance","Jungle Path","River Crossing","Waterfall","Ruins","Temple Steps","Temple Exterior","Temple Gate","Temple Hall","Underground Passage","Hidden Chamber","Treasure Room","Sun Vault"},
            new[]{"Ancient Road","City Gate","Outer District","Market","Canal","Rooftops","Observatory","Palace Road","Gardens","Palace Entrance","Eclipse Hall","Temple","Vault Passage","Grand Vault Door","Grand Vault"}
        };
        public static int WorldIndex(int level) => Mathf.Clamp((level - 11) / 15, 0, 5);
        public static int First(int world) => 11 + 15 * Mathf.Clamp(world, 0, 5);
        public static int Last(int world) => First(world) + 14;
        [Flags] enum F { None = 0, Resize = 1, Crack = 2, Zone = 4, Bonus = 8, Red = 16, Ice = 32, Rush = 64, Perfect = 128 }
        readonly struct Encounter
        {
            public readonly ObjectiveKind goal; public readonly int target; public readonly F rules;
            public Encounter(ObjectiveKind g, int t, F f = F.None) { goal = g; target = t; rules = f; }
        }
        static Encounter S(int n, F f = F.None) => new Encounter(ObjectiveKind.Score, n, f);
        static Encounter H(int n, F f = F.None) => new Encounter(ObjectiveKind.Hits, n, f);
        static Encounter P(int n, F f = F.None) => new Encounter(ObjectiveKind.Perfects, n, f);
        static Encounter C(int n, F f = F.None) => new Encounter(ObjectiveKind.Combo, n, f);
        static Encounter T(int n, F f = F.None) => new Encounter(ObjectiveKind.Survival, n, f);
        static Encounter B(F f = F.None) => new Encounter(ObjectiveKind.Boss, 3, f);
        static readonly Encounter[][] Chapters ={
            new[]{S(650),H(22),S(750,F.Resize),P(8,F.Resize),H(24,F.Crack),S(850,F.Crack),H(22,F.Resize),S(750,F.Zone),C(10),C(12,F.Resize),S(1000,F.Bonus),H(26,F.Zone),S(1100,F.Bonus|F.Crack),P(12,F.Resize),B(F.Bonus)},
            new[]{S(750),H(24,F.Resize),S(1050,F.Bonus),C(10),P(10),H(20,F.Red),S(800,F.Red),H(24,F.Red|F.Resize),C(10,F.Red),S(1000,F.Red),H(26,F.Red|F.Crack),P(12,F.Red),S(1050,F.Red|F.Resize),C(12,F.Red),B(F.Red)},
            new[]{S(800,F.Zone),H(25,F.Zone),P(10,F.Resize),S(1000,F.Zone),C(10),H(18,F.Ice),S(850,F.Ice),H(22,F.Ice),S(1000,F.Ice|F.Bonus),P(10,F.Ice),S(950,F.Zone),C(10,F.Perfect),H(26,F.Ice|F.Resize),P(14,F.Zone),B(F.Ice)},
            new[]{T(40),T(45,F.Resize),H(26),T(50,F.Red),S(950,F.Bonus),H(28,F.Resize),P(12,F.Resize),S(1100,F.Resize),C(12),T(55,F.Resize),H(30,F.Red),S(1150,F.Zone),C(12,F.Resize),T(60,F.Red),B(F.Zone)},
            new[]{S(1000,F.Bonus),H(26,F.Red),P(12,F.Ice),C(12,F.Resize),T(50,F.Zone),S(1200,F.Bonus|F.Red),H(28,F.Ice|F.Resize),S(1100,F.Crack|F.Zone),C(12,F.Red),P(14,F.Resize),S(1300,F.Bonus|F.Ice),H(30,F.Red|F.Resize),T(60,F.Red),C(12,F.Perfect),B(F.Bonus|F.Red)},
            new[]{S(1050,F.Resize),H(28,F.Ice),C(12,F.Red),S(1250,F.Bonus|F.Zone),P(14,F.Resize),H(30,F.Crack|F.Red),T(60,F.Zone),S(1300,F.Ice|F.Bonus),C(14,F.Resize),P(14,F.Red),S(1400,F.Bonus|F.Red),H(32,F.Zone|F.Resize),C(14,F.Perfect),S(1450,F.Ice|F.Red),B(F.Zone|F.Bonus)}
        };
        public static LevelConfig Create(int id)
        {
            if (id < 11 || id > 100) throw new ArgumentOutOfRangeException(nameof(id));
            int w = WorldIndex(id), i = id - First(w); var e = Chapters[w][i];
            bool Has(F flag) => (e.rules & flag) != 0;
            float speed = 94 + w * 3 + (i % 5) * 2;
            bool sustained = e.goal == ObjectiveKind.Score || e.goal == ObjectiveKind.Hits || e.goal == ObjectiveKind.Perfects;
            var c = new LevelConfig
            {
                id = id,
                title = Locations[w][i],
                objective = e.goal,
                objectiveTarget = sustained ? Mathf.RoundToInt(e.target * 1.6f) : e.target,
                timeLimit = e.goal == ObjectiveKind.Survival ? 18 : 75 + w * 2,
                needleSpeed = speed,
                maxNeedleSpeed = 128,
                relocateHitSegment = true,
                resizeHitSegment = Has(F.Resize),
                minSegmentArc = 34,
                maxSegmentArc = 52,
                segmentRespawnDelay = .28f,
                reverseOnMiss = true,
                idleLapPenalty = 10,
                perfectWindowMs = Has(F.Perfect) ? 100 : 120,
                perfectCombo = Has(F.Perfect),
                threeStarPerfects = e.goal == ObjectiveKind.Perfects ? e.target : 5 + w,
                threeStarMisses = 1,
                twoStarMisses = 4,
                crackChance = Has(F.Crack) ? .28f : 0,
                spinRule = SpinRule.Constant,
                rampStart = .96f,
                rampEnd = 1.04f
            };
            float offset = (i * 29 + w * 17) % 360;
            c.segments.Add(new SegmentConfig(offset, 46, Has(F.Ice) ? SegmentKind.Frozen : SegmentKind.Gold));
            c.segments.Add(new SegmentConfig(Mathf.Repeat(offset + 105, 360), 44, SegmentKind.Blue));
            c.segments.Add(new SegmentConfig(Mathf.Repeat(offset + 210, 360), 46, Has(F.Bonus) ? SegmentKind.Bonus : SegmentKind.Gold));
            if (Has(F.Red)) c.segments.Add(new SegmentConfig(Mathf.Repeat(offset + 285, 360), 26, SegmentKind.Avoid, 0));
            if (e.goal == ObjectiveKind.Survival)
            {
                c.segments[0].kind = SegmentKind.Blue;
                c.threeStarPerfects = 6; c.rampStart = c.rampEnd = 1;
            }
            if (Has(F.Zone)) c.speedZones = new[] { new SpeedZoneConfig { startAngle = 35 + (i % 3) * 85, arc = 70, multiplier = w < 3 ? 1.22f : 1.3f, featherDegrees = 12 } };
            if (e.goal == ObjectiveKind.Boss)
            {
                c.title = BossNames[w]; c.bossEncounter = true; c.phases = w == 5 ? 5 : 3; c.objectiveTarget = c.phases;
                c.bossHealth = 610 + w * 75; c.playerHealth = 5; c.timeLimit = 110; c.rampStart = c.rampEnd = 1;
                c.bossPhases = new BossPhaseConfig[c.phases];
                for (int p = 0; p < c.phases; p++) c.bossPhases[p] = new BossPhaseConfig { title = p == 0 ? "OPEN THE GATE" : "SEAL THE GATE", objective = ObjectiveKind.Score, target = p == 0 ? 400 : 200, needleSpeed = 98 + w * 3 + p * 2, minArc = 36, maxArc = 52, resizeTargets = p > 1 };
            }
            return c;
        }
        public static LevelConfig[] Append(LevelConfig[] firstTen)
        {
            if (firstTen == null || firstTen.Length < 10) throw new ArgumentException("The original ten levels are required.");
            var result = new LevelConfig[100]; Array.Copy(firstTen, result, 10);
            for (int id = 11; id <= 100; id++) result[id - 1] = Create(id);
            return result;
        }
    }
}
