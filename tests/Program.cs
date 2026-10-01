using LockSpin;
using System.Text.Json;
using UnityEngine;

int count = 0;
void Test(string name, Action action) { action(); count++; Console.WriteLine("PASS " + name); }
void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }

Test("Median handles odd, even and empty winner sets", () => {
    Check(LockSpinPlayerModel.MedianSorted(new float[]{1, 3, 9}) == 3);
    Check(LockSpinPlayerModel.MedianSorted(new float[]{1, 3, 9, 11}) == 6);
    Check(LockSpinPlayerModel.MedianSorted(Array.Empty<float>()) == 0);
});

Test("Seeded simulation repeats exactly", () => {
    var l = LockSpinCampaign.Defaults()[4];
    var a = LockSpinPlayerModel.Play(l, LockSpinPlayerModel.Casual, 1017);
    var b = LockSpinPlayerModel.Play(l, LockSpinPlayerModel.Casual, 1017);
    Check(a.won == b.won && a.elapsed == b.elapsed && a.hits == b.hits && a.progress == b.progress);
});
Test("Session owns an independent deep clone", () => {
    var l = LockSpinCampaign.Defaults()[4]; var before = JsonUtility.ToJson(l);
    var s = new LockSpinSession(l, 7); s.Level.segments[0].arc = 7; s.Tick(1); s.Tap();
    Check(before == JsonUtility.ToJson(l));
});
Test("JSON clone does not depend on the most recent serialization", () => {
    var levels = LockSpinCampaign.Defaults(); var first = JsonUtility.ToJson(levels[0]);
    _ = JsonUtility.ToJson(levels[4]); Check(JsonUtility.FromJson<LevelConfig>(first).id == levels[0].id);
});
Test("Paused and invalid time steps do not advance state", () => {
    var s = new LockSpinSession(LockSpinCampaign.Defaults()[4], 7);
    s.Tick(float.NaN); s.Tick(float.PositiveInfinity); s.Tick(-1); Check(s.Elapsed == 0);
    s.Paused = true; s.Tick(1); Check(s.Elapsed == 0);
});
Test("Evaluate rejects zero trials", () => {
    try { LockSpinPlayerModel.Evaluate(LockSpinCampaign.Defaults()[4], LockSpinPlayerModel.Casual, 0); throw new Exception("Expected rejection"); }
    catch (ArgumentOutOfRangeException) { }
});
foreach (string[] bad in new[] { new[]{"--runs","0"}, new[]{"--runs","10001"}, new[]{"--level","9999"}, new[]{"--runs"}, new[]{"--wat"}, new[]{"--target","NaN"}, new[]{"--time-limit","Infinity"}, new[]{"--json","--json"} })
    Test("Invalid CLI: " + string.Join(' ', bad), () => Check(Simulator.Run(bad, new StringWriter(), new StringWriter()) == 2));
Test("JSON report is parseable and rates are bounded", () => {
    var output = new StringWriter(); Check(Simulator.Run(new[]{"--runs","20","--level","5","--json"}, output, new StringWriter()) == 0);
    using var doc = JsonDocument.Parse(output.ToString());
    foreach (var p in doc.RootElement.GetProperty("levels")[0].GetProperty("personas").EnumerateArray())
    { float rate = p.GetProperty("winRate").GetSingle(); Check(rate >= 0 && rate <= 1); Check(p.GetProperty("winWithin3").GetSingle() >= rate); }
});
Test("Strict mode fails an impossible target", () => Check(Simulator.Run(new[]{"--runs","3","--level","5","--target","100000","--strict"}, new StringWriter(), new StringWriter()) == 1));
Test("Expansion reports do not claim an unreviewed acceptance band", () => {
    var output = new StringWriter();
    Check(Simulator.Run(new[]{"--runs","1","--level","11","--strict"}, output, new StringWriter()) == 1);
    Check(output.ToString().Contains("UNRATED"));
    try { LockSpinBalanceBands.For(11); throw new Exception("Expected rejection"); }
    catch (ArgumentOutOfRangeException) { }
});
Test("All campaign levels terminate without mutating configuration", () => {
    foreach (var level in LockSpinCampaign.Defaults())
    {
        string before = JsonUtility.ToJson(level);
        foreach (var p in LockSpinPlayerModel.All)
        { var r = LockSpinPlayerModel.Play(level, p, 1234); Check(float.IsFinite(r.elapsed) && r.elapsed >= 0 && r.elapsed <= 151); }
        Check(before == JsonUtility.ToJson(level));
    }
});
Console.WriteLine($"{count} checks passed.");
