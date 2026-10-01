# Lock Spin balance simulator

A standalone C# simulator extracted from the rules of my in-development Unity timing game, Lock Spin. It runs the real session logic against four assumed player timing profiles and reports win rates, completion times, retry success and difficulty-band violations. No Unity installation, game assets, account or network connection is required.

## Run

Install the .NET 9 SDK, then from this directory:

```sh
dotnet run --project src/Simulator -c Release -- --runs 1000 --level 5
dotnet run --project src/Simulator -c Release -- --runs 1000 --level 5 --time-limit 35 --json
dotnet run --project tests -c Release
```

Use `--help` for options. Omit `--level` to evaluate the entire included campaign. `--strict` returns exit code 1 when a level is outside its target band; normal report mode returns 0, and invalid input returns 2. Decimal options use a dot in every locale. There are no runtime NuGet dependencies.

## What is interesting

- The UI-independent session handles hit windows, misses, target relocation, speed zones, boss phases and retries. The simulator uses this code directly instead of maintaining an approximate second game implementation.
- Profiles vary tap jitter, persistent early/late bias, reaction time, missed opportunities and accidental taps. Retry simulations preserve the same player's bias.
- Fixed trial seeds make comparisons reproducible on the same runtime. JSON output supports before/after analysis of a level configuration.
- A relocation check detects packed layouts that silently prevent targets from moving.
- Tests exercise configuration isolation, deterministic runs, bounded reports, invalid inputs, strict failure and termination across every campaign level.

## Interpretation and limitations

These are **assumed design profiles**, not validated predictions of human performance. Population weights and acceptance bands are designer choices, not measured player distributions. A passing band is a tuning signal, not proof that a level is enjoyable or suitable for release. A few trials are noisy; use at least 1,000 for comparisons and validate against playtests. Some included campaign levels can legitimately report outside their bands.

The simulation uses 60 Hz steps and caps a run at 200 simulated seconds; unfinished runs are reported as `SIMULATION LIMIT`. Empty winner sets report median winning time as 0. The Unity compatibility layer implements only the APIs used by the extracted rules; it is not a general Unity replacement. The original Unity report remains the authoritative in-game check. Cross-runtime bit-for-bit reproducibility is not promised. This repository does not include the playable game, art, audio, SDKs or live telemetry.

## Provenance

The session rules, campaign data, player model and original command-line harness come from my Lock Spin project. This public edition was prepared with Codex assistance in October 2026: fixed the missing expansion source, isolated the rules, replaced the stateful JSON clone shim, constrained command-line overrides, corrected even-sample medians, added JSON reports and regression checks, and reviewed the publication files. It is an extracted and improved code sample, not a claim that all development was performed without AI assistance. No third-party game assets are included.

Start with `src/Rules/Balance/LockSpinPlayerModel.cs`, then `src/Rules/Core/LockSpinSession.cs` and `src/Simulator/Program.cs`.
