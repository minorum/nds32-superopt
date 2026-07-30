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
    var optimizer = new Superoptimizer(
        new CandidateEnumerator(),
        new CodeSizeCostModel(),
        new ConcreteEquivalenceVerifier());

    Console.WriteLine("NDS32 Superoptimizer — concrete-equivalence demo");
    Console.WriteLine(new string('─', 60));
    Console.WriteLine();

    var r1 = new Register(1);
    var r2 = new Register(2);

    // ── Case 1: addi rd, rs, 0  →  mov55 rd, rs  (4 bytes → 2 bytes) ─────────
    ShowOptimization(
        optimizer,
        description: "copy via add-zero",
        destination: r2,
        source: new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r1, 0),
        ]));

    // ── Case 2: or rd, rs, rs  →  mov55 rd, rs  (4 bytes → 2 bytes) ──────────
    ShowOptimization(
        optimizer,
        description: "copy via self-OR",
        destination: r2,
        source: new InstructionSequence(
        [
            Instruction.Binary(Opcode.Or, r2, r1, r1),
        ]));

    // ── Case 3: addi rd, rd, 0  →  <empty>  (4 bytes → 0 bytes, nop) ─────────
    ShowOptimization(
        optimizer,
        description: "add-zero-to-self nop",
        destination: r2,
        source: new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r2, 0),
        ]));

    // ── Case 4: addi rd, rs, 1; addi rd, rd, 1  →  addi rd, rs, 2  (8→4) ─────
    ShowOptimization(
        optimizer,
        description: "add-immediate chain folding",
        destination: r2,
        source: new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r1, 1),
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r2, 1),
        ]));

    // ── Case 5: xor rd, rs, rs; add rd, rd, rs  →  mov55 rd, rs  (8→2) ────────
    ShowOptimization(
        optimizer,
        description: "copy via xor-zero then add",
        destination: r2,
        source: new InstructionSequence(
        [
            Instruction.Binary(Opcode.Xor, r2, r1, r1),  // r2 = r1 ^ r1 = 0
            Instruction.Binary(Opcode.Add, r2, r2, r1),  // r2 = 0 + r1 = r1
        ]));

    return 0;
}

static void ShowOptimization(
    Superoptimizer optimizer,
    string description,
    Register destination,
    InstructionSequence source)
{
    Console.WriteLine($"  [{description}]");
    Console.WriteLine($"  Source ({source.EncodedSize} byte(s)):");
    foreach (var line in source.ToString().Split(Environment.NewLine))
        Console.WriteLine($"    {line}");

    var rewrite = optimizer.FindCheapestReplacement(source, destination);

    if (rewrite is null)
    {
        Console.WriteLine("  No cheaper candidate survived concrete testing.");
    }
    else
    {
        Console.WriteLine($"  Replacement ({rewrite.ReplacementCost}, saves {rewrite.BytesSaved} byte(s)):");
        foreach (var line in rewrite.Replacement.ToString().Split(Environment.NewLine))
            Console.WriteLine($"    {line}");
        Console.WriteLine($"  Evidence: {Describe(rewrite.Verification)}");

        if (!rewrite.IsProven)
        {
            Console.WriteLine("  NOTE: not formally proven; do not emit as a trusted LLVM rewrite.");
        }
    }

    Console.WriteLine();
}

static string Describe(VerificationResult result) => result switch
{
    VerificationResult.NoCounterexample tested =>
        $"no counterexample in {tested.CasesChecked} concrete cases",
    VerificationResult.ProvenEquivalent proof =>
        $"formally proven by {proof.ProofMethod}",
    VerificationResult.Counterexample counterexample =>
        $"counterexample at case {counterexample.CaseIndex}, register {counterexample.DifferingRegister}",
    _ => throw new ArgumentOutOfRangeException(nameof(result)),
};

static int PrintHelp()
{
    Console.WriteLine("NDS32 Superoptimizer");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  nds32-superopt demo   Run built-in optimization demonstrations");
    Console.WriteLine("  nds32-superopt help   Show this message");
    return 0;
}

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Run 'nds32-superopt help' for usage.");
    return 2;
}
