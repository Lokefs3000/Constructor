using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Styling
{
    public static class TriggerValues
    {
        public const ushort IsHovered = 0;
        public const ushort IsHeld = 1;
        public const ushort IsActive = 2;

        public static readonly FrozenDictionary<string, ushort> Triggers = new Dictionary<string, ushort>
        {
            { "is-hovered", IsHovered },
            { "is-held", IsHeld },
            { "is-active", IsActive },
            { "is-checked", IsActive },
        }.ToFrozenDictionary();

        public static readonly FrozenDictionary<string, ushort>.AlternateLookup<ReadOnlySpan<char>> TriggersSpanAlt = Triggers.GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
