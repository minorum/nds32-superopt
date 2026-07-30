using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Search;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Tests;

[TestFixture]
public sealed class SuperoptimizerTests
{
    [Test]
    public void FindCheapestReplacement_RewritesAddZeroToMove55()
    {
        var r1 = new Register(1);
        var r2 = new Register(2);
        var source = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r1, 0),
        ]);
        var optimizer = CreateOptimizer();

        var rewrite = optimizer.FindCheapestReplacement(source, r2);

        Assert.Multiple(() =>
        {
            Assert.That(rewrite, Is.Not.Null);
            Assert.That(rewrite!.Replacement.Instructions, Has.Length.EqualTo(1));
            Assert.That(rewrite.Replacement.Instructions[0].Opcode, Is.EqualTo(Opcode.Move55));
            Assert.That(rewrite.BytesSaved, Is.EqualTo(2));
            Assert.That(rewrite.IsProven, Is.False);
            Assert.That(rewrite.Verification, Is.TypeOf<VerificationResult.NoCounterexample>());
        });
    }

    [Test]
    public void FindCheapestReplacement_RemovesArchitecturalNoOp()
    {
        var r2 = new Register(2);
        var source = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, r2, r2, 0),
        ]);

        var rewrite = CreateOptimizer().FindCheapestReplacement(source, r2);

        Assert.Multiple(() =>
        {
            Assert.That(rewrite, Is.Not.Null);
            Assert.That(rewrite!.Replacement.Instructions, Is.Empty);
            Assert.That(rewrite.BytesSaved, Is.EqualTo(4));
        });
    }

    [Test]
    public void ConcreteVerifier_ReturnsCounterexampleForDifferentSequences()
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

        Assert.That(result, Is.TypeOf<VerificationResult.Counterexample>());
    }

    [Test]
    public void ConcreteVerifier_RejectsCandidateThatClobbersUnobservedRegister()
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

        var counterexample = result as VerificationResult.Counterexample;
        Assert.Multiple(() =>
        {
            Assert.That(counterexample, Is.Not.Null);
            Assert.That(counterexample!.DifferingRegister, Is.EqualTo(r3));
        });
    }

    private static Superoptimizer CreateOptimizer() => new(
        new CandidateEnumerator(),
        new CodeSizeCostModel(),
        new ConcreteEquivalenceVerifier(randomCases: 64));
}
