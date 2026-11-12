using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

/// <summary>Effets de la table IFF constructeur et séquences EI/DI/IM/RETN.
/// Les phases d'appel et accès pile seront vérifiés dans le CPU intégré.</summary>
public sealed class InterruptControlTests
{
    [Fact]
    public void EiInhibitsItsOwnBoundaryAndOnlyTheFollowingInstruction()
    {
        var control = Create();
        control.SetInt(true);
        control.EnableInterrupts();
        Assert.True(control.Iff1); Assert.True(control.Iff2);
        control.InstructionCompleted();
        // Une consultation ou un préfixe ne doit pas consommer le délai d'une instruction.
        for (int prefix = 0; prefix < 12; prefix++)
            Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
        control.InstructionCompleted();
        Assert.Equal(Z80InterruptKind.Maskable, control.AcceptAtBoundary(false));
        Assert.False(control.Iff1); Assert.False(control.Iff2);
        Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
    }

    [Fact]
    public void ConsecutiveEiRestartsTheInhibitionAndDiDisablesImmediately()
    {
        var control = Create(); control.SetInt(true);
        for (int instruction = 0; instruction < 8; instruction++)
        {
            control.EnableInterrupts(); control.InstructionCompleted();
            Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
        }
        control.DisableInterrupts();
        Assert.False(control.Iff1); Assert.False(control.Iff2);
        control.InstructionCompleted();
        Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
        control.SetNmi(true);
        Assert.Equal(Z80InterruptKind.NonMaskable, control.AcceptAtBoundary(false));
        control.ReturnFromNonMaskableInterrupt();
        Assert.False(control.Iff1);
    }

    [Fact]
    public void NmiUsesOneEdgeLatchAndPreservesIff2AcrossNestedEntries()
    {
        var control = Create(enabled: true);
        control.SetNmi(true);
        Assert.Equal(Z80InterruptKind.NonMaskable, control.AcceptAtBoundary(false));
        Assert.False(control.Iff1); Assert.True(control.Iff2);
        control.SetNmi(true);
        Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
        control.SetNmi(false); control.SetNmi(true);
        control.SetNmi(false); control.SetNmi(true); // deux fronts pendants : pas une file.
        Assert.Equal(Z80InterruptKind.NonMaskable, control.AcceptAtBoundary(false));
        Assert.False(control.Iff1); Assert.True(control.Iff2);
        Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
        control.ReturnFromNonMaskableInterrupt();
        Assert.True(control.Iff1); Assert.True(control.Iff2);
    }

    [Fact]
    public void NmiIsIndependentOfTheEiMaskAndHigherPriorityThanInt()
    {
        var control = Create();
        control.SetInt(true); control.EnableInterrupts(); control.InstructionCompleted();
        control.SetNmi(true);
        Assert.Equal(Z80InterruptKind.NonMaskable, control.AcceptAtBoundary(false));
        Assert.False(control.Iff1); Assert.True(control.Iff2);
        control.ReturnFromNonMaskableInterrupt(); control.InstructionCompleted();
        Assert.Equal(Z80InterruptKind.Maskable, control.AcceptAtBoundary(false));
        Assert.False(control.Iff2);
    }

    [Fact]
    public void BusRequestDefersAcceptanceWithoutDiscardingPendingNmi()
    {
        var control = Create(enabled: true);
        control.SetInt(true); control.SetNmi(true); control.SetNmi(false);
        var pending = control.CaptureState();
        for (int boundary = 0; boundary < 8; boundary++)
            Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(true));
        Assert.Equal(pending, control.CaptureState());
        Assert.Equal(Z80InterruptKind.NonMaskable, control.AcceptAtBoundary(false));
        control.ReturnFromNonMaskableInterrupt();
        Assert.Equal(Z80InterruptKind.Maskable, control.AcceptAtBoundary(false));
    }

    [Theory]
    [InlineData(Z80InterruptMode.Mode0)]
    [InlineData(Z80InterruptMode.Mode1)]
    [InlineData(Z80InterruptMode.Mode2)]
    public void ModeChangesDoNotEnableInterruptsOrChangeIff2(Z80InterruptMode mode)
    {
        var control = Create(); control.SetInt(true); control.SetMode(mode);
        Assert.Equal(Z80InterruptKind.None, control.AcceptAtBoundary(false));
        control.EnableInterrupts(); control.InstructionCompleted(); control.InstructionCompleted();
        Assert.Equal(Z80InterruptKind.Maskable, control.AcceptAtBoundary(false));
        Assert.Equal(mode, control.Mode);
        Assert.False(control.Iff1); Assert.False(control.Iff2);
    }

    [Fact]
    public void NormalResetEffectsDoNotInventChangesOnExternalInputWires()
    {
        var control = Create(enabled: true);
        control.SetMode(Z80InterruptMode.Mode2); control.EnableInterrupts();
        control.SetInt(true); control.SetNmi(true);
        control.ApplyNormalResetEffects();
        Assert.Equal(new Z80InterruptState(false, false, Z80InterruptMode.Mode0, 0, true, true, false),
            control.CaptureState());
        control.SetNmi(true);
        Assert.False(control.NmiPending);
        control.SetNmi(false); control.SetNmi(true);
        Assert.True(control.NmiPending);
    }

    [Fact]
    public void RestorePreservesEiAndPendingNmiAndRejectsInvalidStatesAtomically()
    {
        var original = Create(); original.SetInt(true); original.EnableInterrupts();
        for (int completed = 0; completed < 3; completed++)
        {
            var restored = new Z80InterruptControl(original.CaptureState());
            Assert.Equal(original.CaptureState(), restored.CaptureState());
            original.SetNmi(true); restored.SetNmi(true);
            var pendingCopy = new Z80InterruptControl(original.CaptureState());
            Assert.Equal(Z80InterruptKind.None, pendingCopy.AcceptAtBoundary(true));
            Assert.Equal(original.AcceptAtBoundary(false), restored.AcceptAtBoundary(false));
            Assert.Equal(Z80InterruptKind.None, restored.AcceptAtBoundary(false));
            Assert.Equal(Z80InterruptKind.NonMaskable, pendingCopy.AcceptAtBoundary(false));
            original.ReturnFromNonMaskableInterrupt(); restored.ReturnFromNonMaskableInterrupt();
            original.SetNmi(false); restored.SetNmi(false);
            Assert.Equal(original.CaptureState(), restored.CaptureState());
            var before = original.CaptureState();
            Assert.Throws<ArgumentException>(() => original.RestoreState(before with { EiInhibitRemaining = 3 }));
            Assert.Throws<ArgumentException>(() => original.RestoreState(before with { Mode = (Z80InterruptMode)3 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => original.SetMode((Z80InterruptMode)3));
            Assert.Equal(before, original.CaptureState());
            original.InstructionCompleted();
        }
    }

    private static Z80InterruptControl Create(bool enabled = false) => new(
        new Z80InterruptState(enabled, enabled, Z80InterruptMode.Mode0, 0, false, false, false));
}
