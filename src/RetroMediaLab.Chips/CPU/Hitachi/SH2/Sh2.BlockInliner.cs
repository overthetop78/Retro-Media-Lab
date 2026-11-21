using System.Reflection;
using System.Reflection.Emit;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private static readonly Dictionary<ushort, OpCode> IlOpcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!).ToDictionary(code => unchecked((ushort)code.Value));
    private static readonly Dictionary<string, OpCode> IlOpcodeNames = IlOpcodes.Values.ToDictionary(code => code.Name!);

    // Copy the existing instruction IL into the native block. This keeps one semantic implementation,
    // and avoids calls back into Debug-compiled instruction/getter helpers on the hot path.
    private void EmitInlinedCall(ILGenerator il, MethodInfo method, int depth = 0, OpCode? originalCall = null)
    {
        MethodInfo optimized = ResolveCompiledHelper(method);
        if (optimized != method) { il.Emit(OpCodes.Call, optimized); return; }
        MethodBody? body = method.GetMethodBody();
        if (body is null || body.ExceptionHandlingClauses.Count != 0 || depth > 5)
        {
            il.Emit(originalCall ?? OpCodes.Call, method); return;
        }
        byte[] bytes = body.GetILAsByteArray()!;
        Type[] typeContext = method.DeclaringType?.GetGenericArguments() ?? [], methodContext = method.GetGenericArguments();
        var arguments = new List<Type>();
        if (!method.IsStatic) arguments.Add(method.DeclaringType!.IsValueType ? method.DeclaringType.MakeByRefType() : method.DeclaringType);
        arguments.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
        LocalBuilder[] args = arguments.Select(type => il.DeclareLocal(type)).ToArray();
        for (int i = args.Length - 1; i >= 0; i--) il.Emit(OpCodes.Stloc, args[i]);
        LocalBuilder[] locals = body.LocalVariables.Select(local => il.DeclareLocal(local.LocalType, local.IsPinned)).ToArray();
        var labels = new Dictionary<int, Label>();
        Label At(int offset) { if (!labels.TryGetValue(offset, out Label label)) labels[offset] = label = il.DefineLabel(); return label; }
        Label end = At(bytes.Length);
        Type? constrained = null;
        int position = 0;
        int Read32() { int value = BitConverter.ToInt32(bytes, position); position += 4; return value; }
        while (position < bytes.Length)
        {
            il.MarkLabel(At(position));
            ushort value = bytes[position++]; if (value == 0xfe) value = (ushort)(0xfe00 | bytes[position++]);
            OpCode code = IlOpcodes[value];
            if (code == OpCodes.Ret) { il.Emit(OpCodes.Br, end); continue; }
            if (code == OpCodes.Ldarg_0) { il.Emit(OpCodes.Ldloc, args[0]); continue; }
            if (code == OpCodes.Ldarg_1) { il.Emit(OpCodes.Ldloc, args[1]); continue; }
            if (code == OpCodes.Ldarg_2) { il.Emit(OpCodes.Ldloc, args[2]); continue; }
            if (code == OpCodes.Ldarg_3) { il.Emit(OpCodes.Ldloc, args[3]); continue; }
            if (code == OpCodes.Ldloc_0) { il.Emit(OpCodes.Ldloc, locals[0]); continue; }
            if (code == OpCodes.Ldloc_1) { il.Emit(OpCodes.Ldloc, locals[1]); continue; }
            if (code == OpCodes.Ldloc_2) { il.Emit(OpCodes.Ldloc, locals[2]); continue; }
            if (code == OpCodes.Ldloc_3) { il.Emit(OpCodes.Ldloc, locals[3]); continue; }
            if (code == OpCodes.Stloc_0) { il.Emit(OpCodes.Stloc, locals[0]); continue; }
            if (code == OpCodes.Stloc_1) { il.Emit(OpCodes.Stloc, locals[1]); continue; }
            if (code == OpCodes.Stloc_2) { il.Emit(OpCodes.Stloc, locals[2]); continue; }
            if (code == OpCodes.Stloc_3) { il.Emit(OpCodes.Stloc, locals[3]); continue; }
            switch (code.OperandType)
            {
                case OperandType.InlineNone: il.Emit(code); break;
                case OperandType.ShortInlineI: il.Emit(code, unchecked((sbyte)bytes[position++])); break;
                case OperandType.InlineI: il.Emit(code, Read32()); break;
                case OperandType.InlineI8: il.Emit(code, BitConverter.ToInt64(bytes, position)); position += 8; break;
                case OperandType.ShortInlineR: il.Emit(code, BitConverter.ToSingle(bytes, position)); position += 4; break;
                case OperandType.InlineR: il.Emit(code, BitConverter.ToDouble(bytes, position)); position += 8; break;
                case OperandType.InlineVar:
                case OperandType.ShortInlineVar:
                {
                    int index = code.OperandType == OperandType.ShortInlineVar ? bytes[position++] : BitConverter.ToUInt16(bytes, position);
                    if (code.OperandType == OperandType.InlineVar) position += 2;
                    if (code == OpCodes.Ldarg || code == OpCodes.Ldarg_S) il.Emit(OpCodes.Ldloc, args[index]);
                    else if (code == OpCodes.Ldarga || code == OpCodes.Ldarga_S) il.Emit(OpCodes.Ldloca, args[index]);
                    else if (code == OpCodes.Starg || code == OpCodes.Starg_S) il.Emit(OpCodes.Stloc, args[index]);
                    else if (code == OpCodes.Ldloc || code == OpCodes.Ldloc_S) il.Emit(OpCodes.Ldloc, locals[index]);
                    else if (code == OpCodes.Ldloca || code == OpCodes.Ldloca_S) il.Emit(OpCodes.Ldloca, locals[index]);
                    else il.Emit(OpCodes.Stloc, locals[index]);
                    break;
                }
                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineBrTarget:
                {
                    int delta = code.OperandType == OperandType.ShortInlineBrTarget ? unchecked((sbyte)bytes[position++]) : Read32();
                    if (code.OperandType == OperandType.ShortInlineBrTarget) code = IlOpcodeNames[code.Name![..^2]];
                    il.Emit(code, At(position + delta)); break;
                }
                case OperandType.InlineSwitch:
                {
                    int count = Read32(); int origin = position + count * 4;
                    var targets = new Label[count]; for (int i = 0; i < count; i++) targets[i] = At(origin + Read32());
                    il.Emit(code, targets); break;
                }
                case OperandType.InlineString: il.Emit(code, method.Module.ResolveString(Read32())); break;
                case OperandType.InlineField: il.Emit(code, method.Module.ResolveField(Read32(), typeContext, methodContext)!); break;
                case OperandType.InlineType:
                {
                    Type type = method.Module.ResolveType(Read32(), typeContext, methodContext);
                    if (code == OpCodes.Constrained) constrained = type; else il.Emit(code, type);
                    break;
                }
                case OperandType.InlineMethod:
                {
                    MethodBase called = method.Module.ResolveMethod(Read32(), typeContext, methodContext)!;
                    if (called is ConstructorInfo constructor) { il.Emit(code, constructor); break; }
                    var target = (MethodInfo)called;
                    if (constrained is null && target.DeclaringType == typeof(Contracts.ISh2Bus) && SpecializedExternalBusType is { IsValueType: false } externalType)
                    {
                        var map = externalType.GetInterfaceMap(typeof(Contracts.ISh2Bus));
                        int index = Array.FindIndex(map.InterfaceMethods, candidate => candidate.MetadataToken == target.MetadataToken);
                        if (index >= 0 && map.TargetMethods[index].DeclaringType == externalType)
                        {
                            ParameterInfo[] parameters = target.GetParameters();
                            LocalBuilder[] values = parameters.Select(parameter => il.DeclareLocal(parameter.ParameterType)).ToArray();
                            for (int i = values.Length - 1; i >= 0; i--) il.Emit(OpCodes.Stloc, values[i]);
                            il.Emit(OpCodes.Castclass, externalType);
                            foreach (LocalBuilder local in values) il.Emit(OpCodes.Ldloc, local);
                            target = (MethodInfo)map.TargetMethods[index];
                            called = target;
                        }
                    }
                    if (constrained is not null && constrained.IsValueType && target.DeclaringType!.IsInterface)
                    {
                        var map = constrained.GetInterfaceMap(target.DeclaringType);
                        int index = Array.FindIndex(map.InterfaceMethods, candidate => candidate.MetadataToken == target.MetadataToken);
                        if (index >= 0) target = (MethodInfo)map.TargetMethods[index];
                    }
                    Type? owner = target.DeclaringType;
                    bool cpuHelper = owner == typeof(Sh2<TBus>) || owner?.DeclaringType == typeof(Sh2<TBus>);
                    bool busHelper = owner == typeof(TBus) || (owner is not null && typeof(Contracts.ISh2Bus).IsAssignableFrom(owner));
                    bool tinyChipHelper = owner?.Module == typeof(Sh2<TBus>).Module && (target.GetMethodBody()?.GetILAsByteArray()?.Length ?? int.MaxValue) <= 40;
                    bool exactDispatch = constrained is null
                        ? !target.IsVirtual || target.IsFinal || owner!.IsSealed
                        : constrained.IsValueType && owner == constrained;
                    bool compilerHelper = target.Name.StartsWith("Emit", StringComparison.Ordinal) || target.Name.StartsWith("Compile", StringComparison.Ordinal) || target.Name is nameof(ClearCodeCache) or nameof(ExecuteNativeLoop) or nameof(ExecuteOpcode);
                    bool inline = !compilerHelper && exactDispatch && depth < 5 && !target.ContainsGenericParameters && (cpuHelper || busHelper || tinyChipHelper) && target.GetMethodBody() is { ExceptionHandlingClauses.Count: 0 };
                    if (inline) EmitInlinedCall(il, target, depth + 1);
                    else if (constrained is not null && target.DeclaringType == constrained && constrained.IsValueType) il.Emit(OpCodes.Call, target);
                    else { if (constrained is not null) il.Emit(OpCodes.Constrained, constrained); il.Emit(code, (MethodInfo)called); }
                    constrained = null; break;
                }
                case OperandType.InlineTok:
                {
                    MemberInfo member = method.Module.ResolveMember(Read32(), typeContext, methodContext)!;
                    if (member is Type type) il.Emit(code, type);
                    else if (member is FieldInfo field) il.Emit(code, field);
                    else il.Emit(code, (MethodInfo)member);
                    break;
                }
                default: throw new InvalidOperationException($"Unsupported SH2 inline operand {code}");
            }
        }
        il.MarkLabel(end);
    }
}
