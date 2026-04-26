using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace MapNodeChanger.Utils.AncientOptions;

public sealed class AncientOptionRerollService
{
    private readonly ConditionalWeakTable<AncientEventModel, RerollRequest> _requests = new();
    private readonly Action<string> _log;

    public AncientOptionRerollService(Action<string>? log = null)
    {
        _log = log ?? (message => Log.Warn(message));
    }

    public void RequestReroll(AncientEventModel ancientEvent, string seedMaterial)
    {
        _requests.Remove(ancientEvent);
        _requests.Add(ancientEvent, new RerollRequest(seedMaterial));
        _log($"AncientOptionReroll: requested reroll for {ancientEvent.Id} material={seedMaterial}");
    }

    public bool TryConsume(AncientEventModel ancientEvent, out Rng rerollRng)
    {
        rerollRng = null!;
        if (!_requests.TryGetValue(ancientEvent, out var request))
        {
            return false;
        }

        _requests.Remove(ancientEvent);

        var runSeed = ancientEvent.Owner?.RunState.Rng.Seed ?? 0;
        var playerId = ancientEvent.Owner?.NetId ?? 0;
        var seed = DeterministicSeed($"{ancientEvent.Id}|run={runSeed}|player={playerId}|{request.SeedMaterial}");
        rerollRng = new Rng(seed);
        _log($"AncientOptionReroll: applying reroll for {ancientEvent.Id} seed={seed} material={request.SeedMaterial}");
        return true;
    }

    private static uint DeterministicSeed(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash;
        }
    }

    private sealed record RerollRequest(string SeedMaterial);
}
