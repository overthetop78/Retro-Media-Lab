#if DEBUG
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Common.Runtime;

/// <summary>Reads the existing immutable RVA data through a small optimized accessor in Debug.</summary>
internal static class NativeLiteralAccessor<T> where T : unmanaged
{
    internal delegate ReadOnlySpan<T> Getter();

    internal static Getter? Create(MethodInfo? source)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported || source is null || !source.IsStatic
            || source.ReturnType != typeof(ReadOnlySpan<T>) || source.GetParameters().Length != 0)
            return null;
        try
        {
            byte[]? body = source.GetMethodBody()?.GetILAsByteArray();
            if (body is null) return null;
            int offset = 0;
            while (offset < body.Length && body[offset] == 0) offset++;
            // Only accept the compiler's pure literal getter: ldtoken, CreateSpan, ret.
            if (body.Length != offset + 11 || body[offset] != 0xd0 || body[offset + 5] != 0x28 || body[^1] != 0x2a)
                return null;
            var context = source.DeclaringType?.GetGenericArguments();
            FieldInfo? field = source.Module.ResolveField(BitConverter.ToInt32(body, offset + 1), context, null);
            const FieldAttributes required = FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRVA;
            if (field is null || (field.Attributes & required) != required) return null;
            MethodBase? call = source.Module.ResolveMethod(BitConverter.ToInt32(body, offset + 6), context, null);
            if (call is not MethodInfo createSpan || createSpan.DeclaringType != typeof(RuntimeHelpers)
                || createSpan.Name != nameof(RuntimeHelpers.CreateSpan) || createSpan.ReturnType != typeof(ReadOnlySpan<T>))
                return null;

            // Anonymous hosting avoids inheriting the calling Debug module's JIT flags.
            var method = new DynamicMethod("LiteralAccess_" + source.DeclaringType!.Name + "_" + source.Name,
                typeof(ReadOnlySpan<T>), Type.EmptyTypes, true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, field); il.Emit(OpCodes.Call, createSpan); il.Emit(OpCodes.Ret);
            var native = method.CreateDelegate<Getter>();
            var original = source.CreateDelegate<Getter>();
            return MemoryMarshal.AsBytes(native()).SequenceEqual(MemoryMarshal.AsBytes(original())) ? native : null;
        }
        catch (Exception error) when (error is NotSupportedException or MemberAccessException
            or ArgumentException or InvalidProgramException)
        {
            return null;
        }
    }
}
#endif
