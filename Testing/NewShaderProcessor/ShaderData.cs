using System;
using System.Collections.Generic;
using System.Text;
using Primary.Utility;

namespace NewShaderProcessor
{
    public sealed class ShaderData
    {
        private readonly List<string> _variantMasks;

        internal ShaderData()
        {
            _variantMasks = new List<string>();
        }

        internal bool AddVariant(string variantName)
        {
            if (_variantMasks.AddUnique(variantName))
            {
                if (_variantMasks.Count > 64)
                {
                    throw new CompilerException("Only 64 unique shader variants are supported");
                }

                return true;
            }

            return false;
        }
    }
}
