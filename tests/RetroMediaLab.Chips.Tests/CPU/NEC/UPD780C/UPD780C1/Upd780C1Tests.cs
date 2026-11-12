using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.Tests.CPU.Common.Z80;
using Xunit;

namespace RetroMediaLab.Chips.Tests.CPU.NEC.UPD780C.UPD780C1;

public sealed class Upd780C1Tests
{
    [Fact]
    public void CarryInstructionsUseNecAccumulatorBitsForEveryFlagHistoryAndIgnoredIndexPrefix()
    {
        // Rapport matériel Banks : NEC NMOS prend F3/F5 dans A, indépendamment
        // de l'historique Q du Zilog. UM0080 : autres flags et temps SCF/CCF.
        foreach (byte accumulator in new byte[] { 0, 8, 32, 40 })
        for (int flags = 0; flags < 256; flags++)
        foreach (byte q in new byte[] { 0, 0xff })
        foreach (byte opcode in new byte[] { 0x37, 0x3f })
        foreach (byte prefix in new byte[] { 0, 0xdd, 0xfd })
        {
            var initial = Z80TestCircuit.InitialRegisters();
            initial.A = accumulator;
            initial.F = (byte)flags;
            initial.Q = q;
            byte[] program = prefix == 0 ? [opcode] : [prefix, opcode];
            using var circuit = new Z80TestCircuit(program, initial, silicon: Z80SiliconVariant.NecNmos);
            circuit.RunInstructions(1);
            int expected = (flags & 0xc4) | accumulator;
            if (opcode == 0x37) expected |= 1;
            else expected |= (flags & 1) != 0 ? 0x10 : 1;
            Assert.Equal((byte)expected, circuit.Cpu.Registers.F);
            Assert.Equal(accumulator, circuit.Cpu.Registers.A);
            Assert.Equal((byte)expected, circuit.Cpu.Registers.Q);
            Assert.Equal((prefix == 0 ? 4 : 8) + 1, circuit.Cpu.InputClockCount);
        }
    }

    // Catalogue NEC 1982 p.393, source retenue explicitement par l'utilisateur.
    // Les impulsions RESET courtes et RD/WR pendant HALT restent non qualifiés.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NormalResetFromWaitOrHaltMatchesTheSelectedNec1982Specification(bool fromHalt)
        => Z80TestCircuit.VerifyNormalReset(Z80SiliconVariant.NecNmos, fromHalt);
}
