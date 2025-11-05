using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Primary.Rendering.PostProcessing
{
    [JsonDerivedType(typeof(EnviormentEffectData), "env")]
    public interface IPostProcessingData
    {
        [JsonIgnore]
        public string Name { get; }
    }
}
