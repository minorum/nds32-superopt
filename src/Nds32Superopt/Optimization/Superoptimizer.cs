using Nds32Superopt.Execution;
using Nds32Superopt.Instructions;

namespace Nds32Superopt.Optimization;

/// <summary>
/// Offline NDS32 superoptimizer.
/// <para>
/// Uses Massalin-style exhaustive enumeration: programs are generated in order
/// of increasing length and tested against a set of random <see cref="TestVector"/>s.
/// The first (shortest) program whose input/output behavior matches the oracle is
/// returned as the optimized sequence.
/// </para>
/// <para>
/// Register convention used by the optimizer:
/// <list type="bullet">
///   <item>r0, r1, … r(numInputs-1)  — live-in inputs</item>
///   <item>r0                         — live-out output (after execution)</item>
///   <item>remaining registers        — scratch</item>
/// </list>
/// </para>
/// </summary>
public sealed class Superoptimizer(SearchSpace? space = null)
{
    private readonly SearchSpace _space = space ?? new SearchSpace();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the shortest NDS32 program equivalent to <paramref name="oracle"/>.
    /// </summary>
    /// <param name="oracle">
    /// A delegate that computes the expected r0 output for given input registers.
    /// </param>
    /// <param name="numInputs">
    /// Number of live-in registers (r0 … r<paramref name="numInputs"/>-1).
    /// </param>
    /// <param name="numTestVectors">
    /// Number of random test vectors to generate for quick filtering.
    /// More vectors reduce false positives at the cost of speed.
    /// </param>
    /// <param name="seed">Random seed (for reproducibility).</param>
    /// <param name="cancellationToken">Token to cancel a long-running search.</param>
    /// <returns>
    /// The shortest equivalent program, or <see langword="null"/> if none was found
    /// within <see cref="SearchSpace.MaxLength"/>.
    /// </returns>
    public Instruction[]? Find(
        Func<uint[], uint> oracle,
        int numInputs = 2,
        int numTestVectors = 16,
        int seed = 42,
        CancellationToken cancellationToken = default)
    {
        var vectors = GenerateTestVectors(oracle, numInputs, numTestVectors, seed);
        return FindWithVectors(vectors, numInputs, cancellationToken);
    }

    /// <summary>
    /// Finds the shortest NDS32 program that satisfies the provided
    /// <paramref name="testVectors"/> (r0 live-out must match
    /// <see cref="TestVector.ExpectedOutput"/> for every vector).
    /// </summary>
    /// <param name="numInputs">
    /// Number of live-in registers.  Defaults to the length of the first test
    /// vector's input array when set to 0.
    /// </param>
    public Instruction[]? FindWithVectors(
        IReadOnlyList<TestVector> testVectors,
        int numInputs = 0,
        CancellationToken cancellationToken = default)
    {
        int liveIn = numInputs > 0
            ? numInputs
            : testVectors.Count > 0 ? testVectors[0].Inputs.Length : 2;

        for (int len = 1; len <= _space.MaxLength; len++)
        {
            var result = SearchLength(len, liveIn, testVectors, cancellationToken);
            if (result is not null) return result;
            if (cancellationToken.IsCancellationRequested) return null;
        }
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Instruction[]? SearchLength(
        int length,
        int numInputs,
        IReadOnlyList<TestVector> vectors,
        CancellationToken ct)
    {
        var enumerator = new ProgramEnumerator(_space, length, numInputs);

        foreach (var candidate in enumerator.Enumerate())
        {
            if (ct.IsCancellationRequested) return null;
            if (IsEquivalent(candidate, vectors))
                return [.. candidate]; // return a fresh copy
        }
        return null;
    }

    private static bool IsEquivalent(
        Instruction[] candidate,
        IReadOnlyList<TestVector> vectors)
    {
        foreach (var vec in vectors)
        {
            var state = CpuState.FromInputs(vec.Inputs);
            state = Interpreter.Execute(state, candidate);
            if (state.Exception || state.OutputAt(0) != vec.ExpectedOutput)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Generates random test vectors by running <paramref name="oracle"/>
    /// over random input pairs.
    /// </summary>
    public static List<TestVector> GenerateTestVectors(
        Func<uint[], uint> oracle,
        int numInputs,
        int count,
        int seed = 42)
    {
        var rng = new Random(seed);
        var vectors = new List<TestVector>(count);

        // Always include edge-case inputs.
        var edgeCases = new uint[][]
        {
            [0, 0],
            [1, 0],
            [0, 1],
            [1, 1],
            [unchecked((uint)-1), 0],
            [0, unchecked((uint)-1)],
            [unchecked((uint)-1), unchecked((uint)-1)],
            [0x8000_0000u, 0],
            [0x7FFF_FFFFu, 1],
        };

        foreach (var ec in edgeCases)
        {
            if (vectors.Count >= count) break;
            var inputs = BuildInputs(ec, numInputs);
            vectors.Add(new TestVector(inputs, oracle(inputs)));
        }

        while (vectors.Count < count)
        {
            var inputs = new uint[numInputs];
            for (int i = 0; i < numInputs; i++)
                inputs[i] = (uint)rng.Next();
            vectors.Add(new TestVector(inputs, oracle(inputs)));
        }

        return vectors;
    }

    private static uint[] BuildInputs(uint[] src, int numInputs)
    {
        var arr = new uint[numInputs];
        for (int i = 0; i < numInputs; i++)
            arr[i] = i < src.Length ? src[i] : 0;
        return arr;
    }
}
