namespace Nds32Superopt.Optimization;

/// <summary>
/// A single test case used to distinguish program behaviors.
/// <para>
/// <see cref="Inputs"/> sets the initial register file (r0, r1, …) and
/// <see cref="ExpectedOutput"/> holds the value r0 should contain after executing
/// a correct program.
/// </para>
/// </summary>
/// <param name="Inputs">Initial register values for the test.</param>
/// <param name="ExpectedOutput">Expected value in r0 after execution.</param>
public sealed record TestVector(uint[] Inputs, uint ExpectedOutput);
