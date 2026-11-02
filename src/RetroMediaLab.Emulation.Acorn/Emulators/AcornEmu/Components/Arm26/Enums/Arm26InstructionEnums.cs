namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Enums;

internal enum Arm26Model
{
    Arm2,
    Arm250,
    Arm3
}

internal enum ArmMultiplyDigit : uint
{
    Zero,
    One,
    Two,
    Three
}

internal enum ArmCondition : uint
{
    Equal,
    NotEqual,
    CarrySet,
    CarryClear,
    Negative,
    Positive,
    Overflow,
    NoOverflow,
    Higher,
    LowerOrSame,
    GreaterOrEqual,
    LessThan,
    GreaterThan,
    LessOrEqual,
    Always,
    Never
}

internal enum ArmDataOperation : uint
{
    And,
    ExclusiveOr,
    Subtract,
    ReverseSubtract,
    Add,
    AddCarry,
    SubtractCarry,
    ReverseSubtractCarry,
    Test,
    TestEqual,
    Compare,
    CompareNegative,
    Or,
    Move,
    BitClear,
    MoveNot
}

internal enum ArmShiftType : uint
{
    LogicalLeft,
    LogicalRight,
    ArithmeticRight,
    RotateRight
}
