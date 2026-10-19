using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections;

namespace Primary.Rendering.Resources
{
    public record struct ResourceEntry(int LocalIndex, bool IsImported, FGResourceType ResourceType, ResourceAlignmentInfo AlignmentInfo, ResourceVersion? Version)
    {
    }
}
