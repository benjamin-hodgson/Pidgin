using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Pidgin.DllRewriter
{
    class Program
    {
        static void Main(string[] args)
        {
            var netcoreapp = args[0].StartsWith("netcoreapp");

            var module = ModuleDefinition.ReadModule(args[1]);



            var unsafeType = module.Types.Single(t => t.Name == "Unsafe");

            RewriteAsPointerMethod(unsafeType);
            RewriteAsRefMethod(netcoreapp, unsafeType, module.TypeSystem);

            module.Write(args[2]);
        }

        // https://github.com/dotnet/corefx/blob/7942e7c3ed03cf7f19dffe539e23b84b4a85ad5a/src/System.Runtime.CompilerServices.Unsafe/src/System.Runtime.CompilerServices.Unsafe.il#L145
        private static void RewriteAsPointerMethod(TypeDefinition unsafeType)
        {
            var asPointerMethod = unsafeType.Methods.Single(m => m.Name == "AsPointer");

            var ilProcessor = asPointerMethod.Body.GetILProcessor();
            foreach (var instruction in asPointerMethod.Body.Instructions.ToList())
            {
                ilProcessor.Remove(instruction);
            }

            asPointerMethod.Body.MaxStackSize = 1;
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Conv_U);
            ilProcessor.Emit(OpCodes.Ret);
        }

        // https://github.com/dotnet/corefx/blob/7942e7c3ed03cf7f19dffe539e23b84b4a85ad5a/src/System.Runtime.CompilerServices.Unsafe/src/System.Runtime.CompilerServices.Unsafe.il#L262
        private static void RewriteAsRefMethod(bool netcoreapp, TypeDefinition unsafeType, TypeSystem typeSystem)
        {
            var asPointerMethod = unsafeType.Methods.Single(m => m.Name == "AsRef");

            var ilProcessor = asPointerMethod.Body.GetILProcessor();
            foreach (var instruction in asPointerMethod.Body.Instructions.ToList())
            {
                ilProcessor.Remove(instruction);
            }

            asPointerMethod.Body.MaxStackSize = 1;

            if (netcoreapp)
            {
                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(OpCodes.Ret);
            }
            else
            {
                asPointerMethod.Body.Variables.Clear();
                asPointerMethod.Body.Variables.Add(new VariableDefinition(new ByReferenceType(typeSystem.Int32)));
                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(OpCodes.Stloc_0);
                ilProcessor.Emit(OpCodes.Ldloc_0);
                ilProcessor.Emit(OpCodes.Ret);
            }
        }
    }
}