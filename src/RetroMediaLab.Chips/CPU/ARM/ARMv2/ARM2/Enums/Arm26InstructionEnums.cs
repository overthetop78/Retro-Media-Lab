namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;

public enum Arm26Model
{
    Arm2,
    Arm250,
    Arm3
}

public enum ArmMultiplyDigit : uint
{
    Zero,
    One,
    Two,
    Three
}

public enum ArmCondition : uint
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

public enum ArmDataOperation : uint
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

public enum ArmShiftType : uint
{
    LogicalLeft,
    LogicalRight,
    ArithmeticRight,
    RotateRight
}
