using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Rules;
using Nds32.Superopt.Core.Search;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Tests;

public sealed class SuperoptimizerTests
{
    [Test]
    public async Task FindCheapestReplacement_RewritesAddZeroToMove55()
    {
        var r1 = new Register(1);
        var r2 = new Register(2);
        var source = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r1, 0),
        ]);

        RewriteCandidate? result = CreateOptimizer().FindCheapestReplacement(source, r2);

        await Assert.That(result).IsNotNull();
        RewriteCandidate rewrite = result!;

        using (Assert.Multiple())
        {
            await Assert.That(rewrite.Replacement.Instructions.Length).IsEqualTo(1);
            await Assert.That(rewrite.Replacement.Instructions[0].Opcode).IsEqualTo(Opcode.Move55);
            await Assert.That(rewrite.BytesSaved).IsEqualTo(2);
            await Assert.That(rewrite.IsProven).IsFalse();
            await Assert.That(rewrite.Verification).IsTypeOf<VerificationResult.NoCounterexample>();
        }
    }

    [Test]
    public async Task FindCheapestReplacement_RemovesArchitecturalNoOp()
    {
        var r2 = new Register(2);
        var source = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r2, 0),
        ]);

        RewriteCandidate? result = CreateOptimizer().FindCheapestReplacement(source, r2);

        await Assert.That(result).IsNotNull();
        RewriteCandidate rewrite = result!;

        using (Assert.Multiple())
        {
            await Assert.That(rewrite.Replacement.Instructions).IsEmpty();
            await Assert.That(rewrite.BytesSaved).IsEqualTo(4);
        }
    }

    [Test]
    public async Task ConcreteVerifier_ReturnsCounterexampleForDifferentSequences()
    {
        var r1 = new Register(1);
        var r2 = new Register(2);
        var source = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r1, 1),
        ]);
        var candidate = new InstructionSequence(
        [
            Instruction.Unary(Opcode.Move55, r2, r1),
        ]);

        VerificationResult result = new ConcreteEquivalenceVerifier(randomCases: 0)
            .Verify(source, candidate);

        await Assert.That(result).IsTypeOf<VerificationResult.Counterexample>();
    }

    [Test]
    public async Task ConcreteVerifier_RejectsCandidateThatClobbersUnobservedRegister()
    {
        var r2 = new Register(2);
        var r3 = new Register(3);
        var source = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.MoveImmediate, r2, default, 1),
        ]);
        var candidate = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.MoveImmediate, r2, default, 1),
            Instruction.WithImmediate(Opcode.MoveImmediate, r3, default, 0),
        ]);

        VerificationResult result = new ConcreteEquivalenceVerifier(randomCases: 0)
            .Verify(source, candidate);

        await Assert.That(result).IsTypeOf<VerificationResult.Counterexample>();
        var counterexample = (VerificationResult.Counterexample)result;
        await Assert.That(counterexample.DifferingRegister).IsEqualTo(r3);
    }

    private static Superoptimizer CreateOptimizer() => new(
        new CandidateEnumerator(),
        new CodeSizeCostModel(),
        new ConcreteEquivalenceVerifier(randomCases: 64));
}