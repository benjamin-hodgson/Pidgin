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
            var module = ModuleDefinition.ReadModule(args[0]);



            var unsafeType = module.Types.Single(t => t.Name == "Unsafe");

            RewriteAsPointerMethod(unsafeType);
            RewriteAsRefMethod(unsafeType, module.TypeSystem);

            module.Write(args[1]);
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
        private static void RewriteAsRefMethod(TypeDefinition unsafeType, TypeSystem typeSystem)
        {
            var asPointerMethod = unsafeType.Methods.Single(m => m.Name == "AsRef");

            var ilProcessor = asPointerMethod.Body.GetILProcessor();
            foreach (var instruction in asPointerMethod.Body.Instructions.ToList())
            {
                ilProcessor.Remove(instruction);
            }

            asPointerMethod.Body.MaxStackSize = 1;
            asPointerMethod.Body.Variables.Clear();
            asPointerMethod.Body.Variables.Add(new VariableDefinition(new ByReferenceType(typeSystem.Int32)));
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Stloc_0);
            ilProcessor.Emit(OpCodes.Ldloc_0);
            ilProcessor.Emit(OpCodes.Ret);
        }
    }
}