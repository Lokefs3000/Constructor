using System.Collections.Immutable;
using System.Text;
using PhysXBindGen.Data;
using PhysXBindGen.Emitters;
using PhysXBindGen.Parsing;

namespace PhysXBindGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string TypesFile = @"D:\source\repos\Constructor\Native\PhysXInterop\bridge_types.hpp";
            ImmutableArray<string> BridgeFiles = [.. Directory.GetFiles(@"D:\source\repos\Constructor\Native\PhysXInterop\public", "*.hpp")];
            const string OutputDir = @"D:\source\repos\Constructor\Interop.PhysX";

            GeneratorState state = new GeneratorState();
            {
                TypeParser parser = new TypeParser(state);
                if (!parser.Scan(File.ReadAllText(TypesFile)))
                    throw new Exception("Type parser scan failed");

                Console.WriteLine($"Scan resulted in #{state.Datatypes.Count} datatypes");
            }

            {
                HeaderParser parser = new HeaderParser(state);
                foreach (string bridgeFile in BridgeFiles)
                {
                    if (!parser.Scan(File.ReadAllText(bridgeFile)))
                        throw new Exception("Header parser scan failed");
                }
            }

            {
                StringBuilder sb = new StringBuilder();
                List<MethodState> methods = new List<MethodState>();
                
                foreach (var (name, type) in state.Datatypes)
                {
                    if (!name.StartsWith('C'))
                        continue;

                    Console.WriteLine($"Emitting bindings for: {name} (type: {type.GetType().Name})");

                    if (type is EnumState enumState)
                    {
                        sb.Clear();
                        EnumEmitter.Emit(enumState, sb);

                        File.WriteAllText($"{OutputDir}\\Enums\\{enumState.Name[1..]}.cs", sb.ToString());
                    }
                    else if (type is TypedefState typedefState)
                    {
                        if (!typedefState.IsGenericAlias)
                        {
                            sb.Clear();
                            TypedefEmitter.Emit(name, typedefState, sb);

                            File.WriteAllText($"{OutputDir}\\Typedefs\\{name[1..]}.cs", sb.ToString());
                        }
                    }
                    else if (type is StructState structState)
                    {
                        sb.Clear();
                        StructEmitter.Emit(structState, sb);

                        File.WriteAllText($"{OutputDir}\\Structs\\{structState.Name[1..]}.cs", sb.ToString());
                    }
                    else if (type is ClassState classState)
                    {
                        sb.Clear();
                        ClassEmitter.Emit(classState, sb);

                        File.WriteAllText($"{OutputDir}\\Classes\\{classState.Name}.cs", sb.ToString());
                        methods.AddRange(classState.Methods);
                    }
                    else if (type is MethodState methodState)
                    {
                        methods.Add(methodState);
                    }
                }

                sb.Clear();
                MethodEmitter.Emit(methods, sb);

                if (sb.Length > 0)
                {
                    File.WriteAllText($"{OutputDir}\\PhysXInterop.cs", sb.ToString());
                }
            }

            Console.ReadKey();
        }
    }
}
