using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Search;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

return args.Length switch
{
    0 => PrintHelp(),
    1 when args[0] is "help" or "--help" or "-h" => PrintHelp(),
    1 when args[0] == "demo" => RunDemo(),
    _ => PrintUnknownCommand(args[0]),
};

static int RunDemo()
{
    var r1 = new Register(1);
    var r2 = new Register(2);

    var source = new InstructionSequence(
    [
        Instruction.WithImmediate(Opcode.AddImmediate, r2, r1, 0),
    ]);

    var optimizer = new Superoptimizer(
        new CandidateEnumerator(),
        new CodeSizeCostModel(),
        new ConcreteEquivalenceVerifier());

    var rewrite = optimizer.FindCheapestReplacement(source, r2);

    Console.WriteLine("Source:");
    Console.WriteLine(source);
    Console.WriteLine($"Cost: {source.EncodedSize} byte(s)");
    Console.WriteLine();

    if (rewrite is null)
    {
        Console.WriteLine("No cheaper candidate survived concrete testing.");
        return 1;
    }

    Console.WriteLine("Replacement candidate:");
    Console.WriteLine(rewrite.Replacement);
    Console.WriteLine($"Cost: {rewrite.ReplacementCost}");
    Console.WriteLine($"Saving: {rewrite.BytesSaved} byte(s)");
    Console.WriteLine($"Evidence: {Describe(rewrite.Verification)}");
    Console.WriteLine();

    if (!rewrite.IsProven)
    {
        Console.WriteLine("WARNING: this candidate is not formally proven and must not be emitted as a trusted LLVM rewrite.");
    }

    return 0;
}

static string Describe(VerificationResult result) => result switch
{
    VerificationResult.NoCounterexample tested =>
        $"no counterexample in {tested.CasesChecked} deterministic concrete cases",
    VerificationResult.ProvenEquivalent proof => $"formally proven by {proof.ProofMethod}",
    VerificationResult.Counterexample counterexample =>
        $"counterexample at case {counterexample.CaseIndex}, register {counterexample.DifferingRegister}",
    _ => throw new ArgumentOutOfRangeException(nameof(result)),
};

static int PrintHelp()
{
    Console.WriteLine("NDS32 Superoptimizer");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  nds32-superopt demo");
    Console.WriteLine("  nds32-superopt help");
    return 0;
}

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Run 'nds32-superopt help' for usage.");
    return 2;
}
