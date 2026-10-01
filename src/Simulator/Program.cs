using System.Globalization;
using System.Text.Json;
using LockSpin;

return Simulator.Run(args, Console.Out, Console.Error);

public static class Simulator
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        try
        {
            if (args.Contains("--help"))
            {
                output.WriteLine("Usage: --runs 1000 --level 5 [--time-limit 45] [--target 1300] [--json] [--strict]\nOmit --level to evaluate the campaign. Runs: 1..10000. Strict exits 1 for out-of-band results; invalid arguments exit 2.");
                return 0;
            }
            int runs = 1000, levelId = 0;
            float? time = null, target = null;
            bool json = false, strict = false;
            var seen = new HashSet<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (!seen.Add(key)) throw new ArgumentException("Duplicate option: " + key);
                if (key == "--json") { json = true; continue; }
                if (key == "--strict") { strict = true; continue; }
                if (key is not ("--runs" or "--level" or "--time-limit" or "--target"))
                    throw new ArgumentException("Unknown option: " + key);
                if (++i >= args.Length) throw new ArgumentException("Missing value for " + key);
                string value = args[i];
                if (key == "--runs") runs = ParseInt(value, 1, 10000);
                if (key == "--level") levelId = ParseInt(value, 1, LockSpinCampaign.Defaults().Length);
                if (key == "--time-limit") time = ParseFloat(value, 1, 150);
                if (key == "--target") target = ParseFloat(value, 1, 100000);
            }
            var levels = LockSpinCampaign.Defaults().Where(l => levelId == 0 || l.id == levelId).ToArray();
            var reports = new List<object>();
            bool allPassed = true;
            foreach (var level in levels)
            {
                if (time.HasValue) level.timeLimit = time.Value;
                if (target.HasValue) level.objectiveTarget = target.Value;
                var stats = LockSpinPlayerModel.All.Select(p => LockSpinPlayerModel.Evaluate(level, p, runs)).ToArray();
                string verdict = LockSpinPlayerModel.Verdict(level, stats, out bool passed);
                float relocation = LockSpinPlayerModel.RelocationRate(level);
                if (relocation < .9f) { passed = false; verdict += "; relocation below 90%"; }
                allPassed &= passed;
                reports.Add(new
                {
                    level = level.id,
                    level.title,
                    populationWinRate = LockSpinPlayerModel.PopulationWin(stats),
                    relocation,
                    passed,
                    verdict,
                    personas = stats.Select(s => new { name = s.persona.name, s.runs, s.wins, s.winRate, s.medianWinSeconds, s.perfectRate, s.threeStarShare, s.lossProgress, s.winWithin3, s.failures })
                });
                if (!json)
                {
                    output.WriteLine(FormattableString.Invariant($"Level {level.id}: {level.title} | population win {LockSpinPlayerModel.PopulationWin(stats):P1} | {verdict}"));
                    foreach (var s in stats)
                        output.WriteLine(FormattableString.Invariant($"  {s.persona.name}: {s.wins}/{s.runs} wins, median winning time {s.medianWinSeconds:F1}s, win within 3 attempts {s.winWithin3:P1}"));
                }
            }
            if (json) output.WriteLine(JsonSerializer.Serialize(new { model = "assumed timing personas", runs, seedPolicy = "1000 + trial * 17; retry offsets 1000000 and 2000000", allPassed, levels = reports }, new JsonSerializerOptions { WriteIndented = true }));
            return strict && !allPassed ? 1 : 0;
        }
        catch (ArgumentException ex) { error.WriteLine(ex.Message); return 2; }
    }

    static int ParseInt(string value, int min, int max) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= min && n <= max
        ? n : throw new ArgumentException($"Expected an integer in {min}..{max}.");
    static float ParseFloat(string value, float min, float max) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float n) && float.IsFinite(n) && n >= min && n <= max
        ? n : throw new ArgumentException($"Expected a finite number in {min}..{max}.");
}
