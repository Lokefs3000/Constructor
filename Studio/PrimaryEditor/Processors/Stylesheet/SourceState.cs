using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Processors.Stylesheet
{
    public record struct SourceState(string SourceText, int Start, int Current, int Line, Stack<RulesetData> Context);
}
