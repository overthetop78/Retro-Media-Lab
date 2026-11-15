using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

/// <summary>Equivalent instruction semantics with independently comparable host dispatch paths.</summary>
public enum Sh2NativeDispatch { Switch, DelegateTable, IndirectTable }

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private static readonly ConcurrentDictionary<(Type Bus, Sh2NativeDispatch Dispatch), Type> NativeCpuTypes = new();
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<MethodInfo, MethodInfo>> NativeHelperMaps = new();
    private MethodInfo ResolveCompiledHelper(MethodInfo source) => NativeHelperMaps.TryGetValue(GetType(), out var helpers)
        && helpers.TryGetValue(source, out MethodInfo? native) ? native : source;
    /// <summary>Native dispatch interpreter over the same state and bus, without translating guest code.</summary>
    public static Sh2<TBus> CreateNative(TBus bus, Type? externalBusType = null, Sh2NativeDispatch dispatch = Sh2NativeDispatch.Switch)
    {
        if (!Enum.IsDefined(dispatch)) throw new ArgumentOutOfRangeException(nameof(dispatch));
        if (!RuntimeFeature.IsDynamicCodeSupported) return new(bus);
        Type type = NativeCpuTypes.GetOrAdd((externalBusType ?? typeof(TBus), dispatch), _ => new NativeRuntimeBuilder(externalBusType, dispatch).Build());
        return (Sh2<TBus>)Activator.CreateInstance(type, [bus])!;
    }

    private sealed class NativeRuntimeBuilder
    {
        private readonly TypeBuilder _type;
        private readonly Type? _externalType;
        private readonly Dictionary<MethodInfo, MethodBuilder> _methods = [];
        private readonly HashSet<Assembly> _granted = [];
        private readonly AssemblyBuilder _assembly;
        private readonly ConstructorInfo _ignoreAccess;
        private readonly Type _cpu = typeof(Sh2<TBus>);
        private readonly FieldBuilder _opcodeTable;
        private readonly Sh2NativeDispatch _dispatch;
        private static readonly Type OpcodeHandlerType = typeof(Action<Sh2<TBus>, ushort>);
        public NativeRuntimeBuilder(Type? externalType, Sh2NativeDispatch dispatch)
        {
            _externalType = externalType; _dispatch = dispatch;
            _assembly = AssemblyBuilder.DefineDynamicAssembly(new("SH2.Native." + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = _assembly.DefineDynamicModule("SH2.Native");
            TypeBuilder attribute = module.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", TypeAttributes.Public | TypeAttributes.Sealed, typeof(Attribute));
            ConstructorBuilder ctor = attribute.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(string)]);
            ILGenerator code = ctor.GetILGenerator(); code.Emit(OpCodes.Ldarg_0); code.Emit(OpCodes.Call, typeof(Attribute).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!); code.Emit(OpCodes.Ret);
            _ignoreAccess = attribute.CreateType()!.GetConstructor([typeof(string)])!;
            Grant(_cpu.Assembly); Grant(typeof(TBus).Assembly); if (externalType is not null) Grant(externalType.Assembly);
            _type = module.DefineType("SH2.Native.Interpreter", TypeAttributes.Public | TypeAttributes.Sealed, _cpu);
            Type entryType = dispatch == Sh2NativeDispatch.IndirectTable ? typeof(IntPtr) : OpcodeHandlerType;
            _opcodeTable = _type.DefineField("OpcodeTable", entryType.MakeArrayType(), FieldAttributes.Private | FieldAttributes.Static);
        }
        private void Grant(Assembly assembly)
        {
            if (_granted.Add(assembly)) _assembly.SetCustomAttribute(new(_ignoreAccess, [assembly.GetName().Name!]));
        }
        private bool CanCopy(MethodInfo method) => !method.ContainsGenericParameters && !method.Name.StartsWith("Emit", StringComparison.Ordinal)
            && !method.Name.StartsWith("Compile", StringComparison.Ordinal) && method.Name is not (nameof(ExecuteNativeLoop) or nameof(ClearCodeCache))
            && (method.Module == _cpu.Module || method.DeclaringType == _externalType)
            && method.GetMethodBody() is { ExceptionHandlingClauses.Count: 0 };
        private MethodInfo Copy(MethodInfo method)
        {
            if (!CanCopy(method)) return method;
            if (_methods.TryGetValue(method, out MethodBuilder? existing)) return existing;
            Grant(method.DeclaringType!.Assembly);
            var parameters = new List<Type>();
            if (!method.IsStatic) parameters.Add(method.DeclaringType.IsValueType ? method.DeclaringType.MakeByRefType() : method.DeclaringType);
            parameters.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
            MethodBuilder target = _type.DefineMethod("N" + _methods.Count + "_" + method.Name, MethodAttributes.Private | MethodAttributes.Static, method.ReturnType, parameters.ToArray());
            _methods[method] = target;
            if ((method.GetMethodBody()!.GetILAsByteArray()?.Length ?? 0) <= 64
                || (method.GetMethodImplementationFlags() & MethodImplAttributes.AggressiveInlining) != 0)
                target.SetImplementationFlags(MethodImplAttributes.AggressiveInlining);
            CopyBody(target.GetILGenerator(), method);
            return target;
        }
        public Type Build()
        {
            // Gens OP_Table maps every complete opcode to its handler. Compile
            // these reusable instruction handlers once, never per guest ROM/block.
            var handlerNames = new string[_dispatch == Sh2NativeDispatch.Switch ? 0 : 143];
            for (int i = 0; i < handlerNames.Length; i++)
                handlerNames[i] = Copy(_cpu.GetMethod("DirectOpcode" + i, BindingFlags.Static | BindingFlags.NonPublic)!).Name;
            // Blocks call the same optimized helpers as the interpreter, avoiding
            // a return into Debug-compiled C# accessors for every guest instruction.
            foreach (string helper in new[] { nameof(BeginCompiledInstruction), nameof(CanRunCompiledPurePrefix), nameof(FinishCompiledPurePrefix), nameof(CanContinueCompiledAfterRead), nameof(ResolveCompiledSuccessor),
                nameof(CanRunCompiledFastPayload), nameof(ChargeCompiledFastFetch), nameof(FinishCompiledFastLinear), nameof(FinishCompiledFastRead) })
                Copy(_cpu.GetMethod(helper, BindingFlags.Instance | BindingFlags.NonPublic)!);
            ConstructorBuilder ctor = _type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(TBus)]);
            ILGenerator il = ctor.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, _cpu.GetConstructor([typeof(TBus)])!); il.Emit(OpCodes.Ret);
            MethodInfo synchronize = Copy(_cpu.GetMethod(nameof(SynchronizeBusClock))!);
            MethodInfo cycles = Copy(_cpu.GetProperty(nameof(Cycles))!.GetMethod!);
            MethodInfo loop = Copy(_cpu.GetMethod(nameof(ExecuteLoop), BindingFlags.Instance | BindingFlags.NonPublic)!);
            FieldInfo deadline = _cpu.GetField(nameof(_executionEnd), BindingFlags.Instance | BindingFlags.NonPublic)!;
            MethodBuilder execute = _type.DefineMethod(nameof(Execute), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(int), [typeof(int)]);
            _type.DefineMethodOverride(execute, _cpu.GetMethod(nameof(Execute))!);
            il = execute.GetILGenerator(); LocalBuilder start = il.DeclareLocal(typeof(ulong));
            Label valid = il.DefineLabel(); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Bgt, valid); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Ret); il.MarkLabel(valid);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, synchronize);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, cycles); il.Emit(OpCodes.Stloc, start);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldloc, start); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Conv_U8); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stfld, deadline);
            il.BeginExceptionBlock(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, loop);
            il.BeginFinallyBlock(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, synchronize); il.EndExceptionBlock();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, cycles); il.Emit(OpCodes.Ldloc, start); il.Emit(OpCodes.Sub); il.Emit(OpCodes.Conv_Ovf_I4_Un); il.Emit(OpCodes.Ret);
            Type nativeType = _type.CreateType()!;
            if (_dispatch == Sh2NativeDispatch.IndirectTable)
            {
                var handlers = new IntPtr[handlerNames.Length];
                for (int i = 0; i < handlers.Length; i++)
                {
                    MethodInfo handler = nativeType.GetMethod(handlerNames[i], BindingFlags.Static | BindingFlags.NonPublic)!;
                    RuntimeHelpers.PrepareMethod(handler.MethodHandle);
                    handlers[i] = handler.MethodHandle.GetFunctionPointer();
                }
                var table = new IntPtr[65536];
                for (int opcode = 0; opcode < table.Length; opcode++) table[opcode] = handlers[Sh2OpcodeTable.Types[opcode]];
                nativeType.GetField(_opcodeTable.Name, BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, table);
            }
            else if (_dispatch == Sh2NativeDispatch.DelegateTable)
            {
                var handlers = new Action<Sh2<TBus>, ushort>[handlerNames.Length];
                for (int i = 0; i < handlers.Length; i++)
                    handlers[i] = nativeType.GetMethod(handlerNames[i], BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Action<Sh2<TBus>, ushort>>();
                var table = new Action<Sh2<TBus>, ushort>[65536];
                for (int opcode = 0; opcode < table.Length; opcode++) table[opcode] = handlers[Sh2OpcodeTable.Types[opcode]];
                nativeType.GetField(_opcodeTable.Name, BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, table);
            }
            NativeHelperMaps[nativeType] = _methods.ToDictionary(entry => entry.Key,
                entry => nativeType.GetMethod(entry.Value.Name, BindingFlags.Static | BindingFlags.NonPublic)!);
            return nativeType;
        }
        private void EmitDirectDispatch(ILGenerator il)
        {
            LocalBuilder opcode = il.DeclareLocal(typeof(ushort)), cpu = il.DeclareLocal(_cpu);
            il.Emit(OpCodes.Stloc, opcode); il.Emit(OpCodes.Stloc, cpu);
            if (_dispatch == Sh2NativeDispatch.IndirectTable)
            {
                il.Emit(OpCodes.Ldloc, cpu); il.Emit(OpCodes.Ldloc, opcode);
                il.Emit(OpCodes.Ldsfld, _opcodeTable); il.Emit(OpCodes.Ldloc, opcode); il.Emit(OpCodes.Ldelem_I);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), [_cpu, typeof(ushort)], null);
            }
            else
            {
                il.Emit(OpCodes.Ldsfld, _opcodeTable); il.Emit(OpCodes.Ldloc, opcode); il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Ldloc, cpu); il.Emit(OpCodes.Ldloc, opcode);
                il.Emit(OpCodes.Callvirt, OpcodeHandlerType.GetMethod("Invoke")!);
            }
        }
        private void CopyBody(ILGenerator il, MethodInfo source)
        {
            MethodBody body = source.GetMethodBody()!; byte[] bytes = body.GetILAsByteArray()!;
            foreach (LocalVariableInfo local in body.LocalVariables) il.DeclareLocal(local.LocalType, local.IsPinned);
            Type[] types = source.DeclaringType!.GetGenericArguments(), methods = source.GetGenericArguments();
            var labels = new Dictionary<int, Label>();
            Label At(int offset) { if (!labels.TryGetValue(offset, out Label label)) labels[offset] = label = il.DefineLabel(); return label; }
            int position = 0; Type? constrained = null;
            int Read32() { int value = BitConverter.ToInt32(bytes, position); position += 4; return value; }
            while (position < bytes.Length)
            {
                il.MarkLabel(At(position)); ushort value = bytes[position++]; if (value == 0xfe) value = (ushort)(0xfe00 | bytes[position++]);
                OpCode code = IlOpcodes[value];
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: il.Emit(code); break;
                    case OperandType.ShortInlineI: il.Emit(code, unchecked((sbyte)bytes[position++])); break;
                    case OperandType.InlineI: il.Emit(code, Read32()); break;
                    case OperandType.InlineI8: il.Emit(code, BitConverter.ToInt64(bytes, position)); position += 8; break;
                    case OperandType.ShortInlineR: il.Emit(code, BitConverter.ToSingle(bytes, position)); position += 4; break;
                    case OperandType.InlineR: il.Emit(code, BitConverter.ToDouble(bytes, position)); position += 8; break;
                    case OperandType.ShortInlineVar: il.Emit(code, bytes[position++]); break;
                    case OperandType.InlineVar: il.Emit(code, unchecked((short)BitConverter.ToUInt16(bytes, position))); position += 2; break;
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                    {
                        int delta = code.OperandType == OperandType.ShortInlineBrTarget ? unchecked((sbyte)bytes[position++]) : Read32();
                        if (code.OperandType == OperandType.ShortInlineBrTarget) code = IlOpcodeNames[code.Name![..^2]];
                        il.Emit(code, At(position + delta)); break;
                    }
                    case OperandType.InlineSwitch:
                    {
                        int count = Read32(), origin = position + count * 4; var targets = new Label[count];
                        for (int i = 0; i < count; i++) targets[i] = At(origin + Read32()); il.Emit(code, targets); break;
                    }
                    case OperandType.InlineString: il.Emit(code, source.Module.ResolveString(Read32())); break;
                    case OperandType.InlineField: il.Emit(code, source.Module.ResolveField(Read32(), types, methods)!); break;
                    case OperandType.InlineType:
                    {
                        Type type = source.Module.ResolveType(Read32(), types, methods);
                        if (code == OpCodes.Constrained) constrained = type; else il.Emit(code, type); break;
                    }
                    case OperandType.InlineMethod:
                    {
                        MethodBase called = source.Module.ResolveMethod(Read32(), types, methods)!;
                        if (called is ConstructorInfo constructor) { il.Emit(code, constructor); break; }
                        var target = (MethodInfo)called;
                        if (_dispatch != Sh2NativeDispatch.Switch && target.DeclaringType == _cpu && target.Name == nameof(ExecuteOpcode))
                        {
                            EmitDirectDispatch(il); constrained = null; break;
                        }
                        if (constrained?.IsValueType == true && target.DeclaringType!.IsInterface)
                        {
                            var map = constrained.GetInterfaceMap(target.DeclaringType);
                            int index = Array.FindIndex(map.InterfaceMethods, candidate => candidate.MetadataToken == target.MetadataToken);
                            if (index >= 0 && map.TargetMethods[index].DeclaringType == constrained) target = (MethodInfo)map.TargetMethods[index];
                        }
                        if (constrained is null && target.DeclaringType == typeof(Contracts.ISh2Bus) && _externalType is { IsValueType: false })
                        {
                            var map = _externalType.GetInterfaceMap(typeof(Contracts.ISh2Bus)); int index = Array.FindIndex(map.InterfaceMethods, candidate => candidate.MetadataToken == target.MetadataToken);
                            if (index >= 0 && map.TargetMethods[index].DeclaringType == _externalType)
                            {
                                LocalBuilder[] args = target.GetParameters().Select(parameter => il.DeclareLocal(parameter.ParameterType)).ToArray();
                                for (int i = args.Length - 1; i >= 0; i--) il.Emit(OpCodes.Stloc, args[i]);
                                il.Emit(OpCodes.Castclass, _externalType); foreach (LocalBuilder arg in args) il.Emit(OpCodes.Ldloc, arg);
                                target = (MethodInfo)map.TargetMethods[index]; called = target;
                            }
                        }
                        bool exact = constrained is null ? !target.IsVirtual || target.IsFinal || target.DeclaringType!.IsSealed : constrained.IsValueType && target.DeclaringType == constrained;
                        if (exact && CanCopy(target)) il.Emit(OpCodes.Call, Copy(target));
                        else if (constrained?.IsValueType == true && target.DeclaringType == constrained) il.Emit(OpCodes.Call, target);
                        else { if (constrained is not null) il.Emit(OpCodes.Constrained, constrained); il.Emit(code, (MethodInfo)called); }
                        constrained = null; break;
                    }
                    case OperandType.InlineTok:
                    {
                        MemberInfo member = source.Module.ResolveMember(Read32(), types, methods)!;
                        if (member is Type type) il.Emit(code, type); else if (member is FieldInfo field) il.Emit(code, field); else il.Emit(code, (MethodInfo)member); break;
                    }
                    default: throw new InvalidOperationException($"Unsupported native SH2 operand {code}");
                }
            }
            if (labels.ContainsKey(bytes.Length)) il.MarkLabel(labels[bytes.Length]);
        }
    }
}
