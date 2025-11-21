using CommandLine;
using Editor.Shaders;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Common;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace ShaderGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            result.WithParsed(ParseSuccess);
            result.WithNotParsed(ParseFailed);
        }

        private static void ParseSuccess(CommandLineOptions options)
        {
            string source = File.ReadAllText(options.Input);

            ILogger logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            ShaderProcessorArgs args = new ShaderProcessorArgs
            {
                InputSource = source,
                SourceFileName = Path.GetFileName(options.Input),
                IncludeDirectories = options.IncludeDirectories?.ToArray() ?? Array.Empty<string>(),
                Targets = ShaderCompileTarget.None
            };

            if (options.CompileForDirect3D12)
                args.Targets |= ShaderCompileTarget.Direct3D12;
            if (options.CompileForDirectVulkan)
                args.Targets |= ShaderCompileTarget.Vulkan;

            long startTS = Stopwatch.GetTimestamp();

            ShaderProcessor processor = new ShaderProcessor(logger, ShaderAttributeSettings.Default);
            ShaderProcesserResult? result = processor.Process(args);

            double elapsed = (Stopwatch.GetTimestamp() - startTS) / (double)Stopwatch.Frequency;

            if (!result.HasValue)
            {
                Console.WriteLine("An error occured under compilation.");
                Console.ReadLine();
                return;
            }

            //print debug
            StringBuilder sb = new StringBuilder();

            {
                sb.Append("Duration: \e[38;5;33m");
                sb.Append(elapsed.ToString("F4"));
                sb.AppendLine("s\e[0m");
                sb.AppendLine();
                sb.Append("Property source template: ");
                if (processor.PropertySourceTemplate == null)
                    sb.AppendLine("null");
                else
                {
                    sb.Append("\e[38;5;221m\"");
                    sb.Append(processor.PropertySourceTemplate);
                    sb.AppendLine("\"\e[0m");
                }
                sb.AppendLine();
            }

            {
                sb.AppendLine("\e[1mBytecode:\e[0m");

                ShaderCompileTarget target = ShaderCompileTarget.None;
                foreach (ref readonly ShaderBytecode bytecode in result.Value.Bytecodes.AsSpan())
                {
                    if (target != bytecode.Target)
                    {
                        sb.Append("    \e[38;5;180m");
                        sb.Append(bytecode.Target.ToString());
                        sb.AppendLine("\e[0m:");

                        target = bytecode.Target;
                    }

                    sb.Append("       ");
                    sb.Append(bytecode.Stage.ToString());
                    sb.Append(": \e[38;5;140m");
                    sb.Append(FileUtility.FormatSize(bytecode.Bytes.LongLength, "F2", CultureInfo.InvariantCulture));
                    sb.AppendLine("\e[0m");
                }
            }

            {
                sb.AppendLine("\e[1mFunctions:\e[0m");

                foreach (ref readonly FunctionData data in processor.Functions)
                {
                    sb.Append("    \e[38;5;180m");
                    sb.Append(data.Name);
                    sb.Append(" \e[0m(\e[38;5;140m");
                    sb.Append(data.BodyRange.Start);
                    sb.Append("\e[0m-\e[38;5;140m");
                    sb.Append(data.BodyRange.End);
                    sb.AppendLine(data.Attributes.Length > 0 ? "\e[0m):" : "\e[0m)");

                    foreach (ref readonly AttributeData attrib in data.Attributes.AsSpan())
                    {
                        AttributeSignature signature = attrib.Signature;

                        sb.Append("        ");
                        sb.Append(signature.Name);

                        if (signature.Signature.Length > 0 && attrib.Data != null)
                        {
                            foreach (ref readonly AttributeVarData var in attrib.Data.AsSpan())
                            {
                                if (var.SourceIndex == -1)
                                    continue;

                                ref readonly AttributeVariable sig = ref signature.Signature[var.SourceIndex];

                                sb.Append("            ");
                                sb.Append(sig.Name);
                                sb.Append(": ");
                                sb.Append((var.Value ?? sig.Default)?.ToString() ?? "null");
                                sb.Append(" (");
                                sb.Append(sig.Type.Name);
                                sb.AppendLine(")");
                            }
                        }
                        else
                            sb.AppendLine();
                    }

                    sb.AppendLine("        \e[1mArguments:\e[0m");

                    foreach (ref readonly VariableData varData in data.Arguments.AsSpan())
                    {
                        sb.Append("            \e[38;5;155m");
                        if (varData.Generic.Generic == ValueGeneric.Custom)
                        {
                            ref readonly StructData @struct = ref processor.GetRefSource(varData.Generic);
                            sb.Append(Unsafe.IsNullRef(in @struct) ? "null" : @struct.Name);
                        }
                        else
                            sb.Append(varData.Generic.ToString());
                        sb.Append(" \e[38;5;180m");
                        sb.Append(varData.Name);
                        if (varData.Semantic.HasValue)
                        {
                            VarSemantic semantic = varData.Semantic.Value;

                            sb.Append(" \e[0m: ");
                            sb.Append(semantic.Semantic.ToString());
                            if (semantic.Index > 0)
                                sb.Append(semantic.Index);
                        }
                        sb.AppendLine("\e[0m");
                    }
                }
            }

            {
                sb.AppendLine("\e[1mResources:\e[0m");

                foreach (ref readonly ResourceData data in processor.Resources)
                {
                    sb.Append("    \e[38;5;155m");
                    sb.Append(data.Type.ToString());
                    if (data.Value.IsSpecified)
                    {
                        sb.Append('<');
                        if (data.Value.Generic == ValueGeneric.Custom)
                        {
                            ref readonly StructData @struct = ref processor.GetRefSource(data.Value);
                            sb.Append(Unsafe.IsNullRef(in @struct) ? "null" : @struct.Name);
                        }
                        else
                            sb.Append(data.Value.ToString());
                        sb.Append('>');
                    }
                    sb.Append(" \e[38;5;180m");
                    sb.Append(data.Name);
                    sb.Append(" \e[0m(\e[38;5;140m");
                    sb.Append(data.DeclerationRange.Start);
                    sb.Append("\e[0m-\e[38;5;140m");
                    sb.Append(data.DeclerationRange.End);
                    sb.AppendLine(data.Attributes.Length > 0 ? "\e[0m):" : "\e[0m)");

                    foreach (ref readonly AttributeData attrib in data.Attributes.AsSpan())
                    {
                        AttributeSignature signature = attrib.Signature;

                        sb.Append("        ");
                        sb.Append(signature.Name);

                        if (signature.Signature.Length > 0 && attrib.Data != null)
                        {
                            foreach (ref readonly AttributeVarData var in attrib.Data.AsSpan())
                            {
                                ref readonly AttributeVariable sig = ref signature.Signature[var.SourceIndex];

                                sb.Append("            ");
                                sb.Append(sig.Name);
                                sb.Append(": ");
                                sb.Append((var.Value ?? sig.Default)?.ToString() ?? "null");
                                sb.Append(" (");
                                sb.Append(sig.Type.Name);
                                sb.AppendLine(")");
                            }
                        }
                        else
                            sb.AppendLine();
                    }
                }
            }

            {
                sb.AppendLine("\e[1mProperties:\e[0m");

                foreach (ref readonly PropertyData data in processor.Properties)
                {
                    sb.Append("    \e[38;5;155m");
                    if (data.Generic.Generic == ValueGeneric.Custom)
                    {
                        ref readonly StructData @struct = ref processor.GetRefSource(data.Generic);
                        sb.Append(Unsafe.IsNullRef(in @struct) ? "null" : @struct.Name);
                    }
                    else
                        sb.Append(data.ToString());
                    sb.Append(" \e[38;5;180m");
                    sb.Append(data.Name);
                    sb.Append(" \e[0m(\e[38;5;140m");
                    sb.Append(data.DeclerationRange.Start);
                    sb.Append("\e[0m-\e[38;5;140m");
                    sb.Append(data.DeclerationRange.End);
                    sb.AppendLine(data.Attributes.Length > 0 ? "\e[0m):" : "\e[0m)");

                    foreach (ref readonly AttributeData attrib in data.Attributes.AsSpan())
                    {
                        AttributeSignature signature = attrib.Signature;

                        sb.Append("        ");
                        sb.Append(signature.Name);

                        if (signature.Signature.Length > 0 && attrib.Data != null)
                        {
                            foreach (ref readonly AttributeVarData var in attrib.Data.AsSpan())
                            {
                                ref readonly AttributeVariable sig = ref signature.Signature[var.SourceIndex];

                                sb.Append("                    ");
                                sb.Append(sig.Name);
                                sb.Append(": ");
                                sb.Append((var.Value ?? sig.Default)?.ToString() ?? "null");
                                sb.Append(" (");
                                sb.Append(sig.Type.Name);
                                sb.AppendLine(")");
                            }
                        }
                        else
                            sb.AppendLine();
                    }
                }
            }

            {
                sb.AppendLine("\e[1mStructs:\e[0m");

                foreach (ref readonly StructData data in processor.Structs)
                {
                    sb.Append("    \e[38;5;180m");
                    sb.Append(data.Name);
                    sb.Append(" \e[0m(\e[38;5;140m");
                    sb.Append(data.DeclerationRange.Start);
                    sb.Append("\e[0m-\e[38;5;140m");
                    sb.Append(data.DeclerationRange.End);
                    sb.AppendLine(data.Attributes.Length > 0 ? "\e[0m):" : "\e[0m)");

                    foreach (ref readonly AttributeData attrib in data.Attributes.AsSpan())
                    {
                        AttributeSignature signature = attrib.Signature;

                        sb.Append("        ");
                        sb.Append(signature.Name);

                        if (signature.Signature.Length > 0 && attrib.Data != null)
                        {
                            foreach (ref readonly AttributeVarData var in attrib.Data.AsSpan())
                            {
                                ref readonly AttributeVariable sig = ref signature.Signature[var.SourceIndex];

                                sb.Append("            ");
                                sb.Append(sig.Name);
                                sb.Append(": ");
                                sb.Append((var.Value ?? sig.Default)?.ToString() ?? "null");
                                sb.Append(" (");
                                sb.Append(sig.Type.Name);
                                sb.AppendLine(")");
                            }
                        }
                        else
                            sb.AppendLine();
                    }

                    sb.AppendLine("        \e[1mVariables:\e[0m");

                    foreach (ref readonly VariableData varData in data.Variables.AsSpan())
                    {
                        sb.Append("            \e[38;5;155m");
                        if (varData.Generic.Generic == ValueGeneric.Custom)
                        {
                            ref readonly StructData @struct = ref processor.GetRefSource(varData.Generic);
                            sb.Append(Unsafe.IsNullRef(in @struct) ? "null" : @struct.Name);
                        }
                        else
                            sb.Append(varData.Generic.ToString());
                        sb.Append(" \e[38;5;180m");
                        sb.Append(varData.Name);
                        if (varData.Semantic.HasValue)
                        {
                            VarSemantic semantic = varData.Semantic.Value;

                            sb.Append(" \e[0m: ");
                            sb.Append(semantic.Semantic.ToString());
                            if (semantic.Index > 0)
                                sb.Append(semantic.Index);
                        }
                        sb.AppendLine(data.Attributes.Length > 0 ? "\e[0m:" : "\e[0m");

                        foreach (ref readonly AttributeData attrib in varData.Attributes.AsSpan())
                        {
                            AttributeSignature signature = attrib.Signature;

                            sb.Append("                ");
                            sb.Append(signature.Name);

                            if (signature.Signature.Length > 0 && attrib.Data != null)
                            {
                                foreach (ref readonly AttributeVarData var in attrib.Data.AsSpan())
                                {
                                    if (var.SourceIndex == -1)
                                        continue;
                                    ref readonly AttributeVariable sig = ref signature.Signature[var.SourceIndex];

                                    sb.Append("                    ");
                                    sb.Append(sig.Name);
                                    sb.Append(": ");
                                    sb.Append((var.Value ?? sig.Default)?.ToString() ?? "null");
                                    sb.Append(" (");
                                    sb.Append(sig.Type.Name);
                                    sb.AppendLine(")");
                                }
                            }
                            else
                                sb.AppendLine();
                        }
                    }
                }
            }

            {
                sb.AppendLine("\e[1mStatic samplers:\e[0m");

                foreach (ref readonly StaticSamplerData staticSampler in processor.StaticSamplers)
                {
                    sb.Append("    \e[38;5;180m");
                    sb.Append(staticSampler.Name);
                    sb.Append(" \e[0m(\e[38;5;140m");
                    sb.Append(staticSampler.DeclerationRange.Start);
                    sb.Append("\e[0m-\e[38;5;140m");
                    sb.Append(staticSampler.DeclerationRange.End);
                    sb.AppendLine(staticSampler.Attributes.Length > 0 ? "\e[0m):" : "\e[0m)");

                    foreach (ref readonly AttributeData attrib in staticSampler.Attributes.AsSpan())
                    {
                        AttributeSignature signature = attrib.Signature;

                        sb.Append("        ");
                        sb.Append(signature.Name);

                        if (signature.Signature.Length > 0 && attrib.Data != null)
                        {
                            foreach (ref readonly AttributeVarData var in attrib.Data.AsSpan())
                            {
                                ref readonly AttributeVariable sig = ref signature.Signature[var.SourceIndex];

                                sb.Append("            ");
                                sb.Append(sig.Name);
                                sb.Append(": ");
                                sb.Append((var.Value ?? sig.Default)?.ToString() ?? "null");
                                sb.Append(" (");
                                sb.Append(sig.Type.Name);
                                sb.AppendLine(")");
                            }
                        }
                        else
                            sb.AppendLine();
                    }

                    sb.Append("       \e[0mFilter: \e[38;5;180m");
                    sb.AppendLine(staticSampler.Filter.ToString());

                    sb.Append("       \e[0mAddress modes:");

                    sb.Append("           U: \e[38;5;180m");
                    sb.AppendLine(staticSampler.AddressModeU.ToString());

                    sb.Append("           \e[0mV: \e[38;5;180m");
                    sb.AppendLine(staticSampler.AddressModeV.ToString());

                    sb.Append("           \e[0mW: \e[38;5;180m");
                    sb.AppendLine(staticSampler.AddressModeW.ToString());

                    sb.Append("       \e[0mMax anisotropy: \e[38;5;180m");
                    sb.AppendLine(staticSampler.MaxAnisotropy.ToString());

                    sb.Append("       \e[0mMip LOD bias: \e[38;5;180m");
                    sb.AppendLine(staticSampler.MipLODBias.ToString());

                    sb.Append("           \e[0mMin LOD: \e[38;5;180m");
                    sb.AppendLine(staticSampler.MinLOD.ToString());

                    sb.Append("           \e[0mMax LOD: \e[38;5;180m");
                    sb.AppendLine(staticSampler.MaxLOD.ToString());

                    sb.Append("       \e[0mBorder: \e[38;5;180m");
                    sb.AppendLine(staticSampler.Border.ToString());
                }
            }

            Console.WriteLine(sb.ToString());
            Console.ReadLine();
        }

        private static void ParseFailed(IEnumerable<Error> errors)
        {
            foreach (Error error in errors)
            {
                Console.WriteLine(error.ToString());
            }
        }

        private class CommandLineOptions
        {
            [Option('i', "input", Required = true)]
            public string Input { get; init; }

            [Option('o', "output", Required = true)]
            public string Output { get; init; }

            [Option('I', "incdir")]
            public IEnumerable<string> IncludeDirectories { get; init; }

            [Option("d3d12")]
            public bool CompileForDirect3D12 { get; init; }

            [Option("vulkan")]
            public bool CompileForDirectVulkan { get; init; }
        }
    }
}
