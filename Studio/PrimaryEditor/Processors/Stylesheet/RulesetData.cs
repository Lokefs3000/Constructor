using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Utility;

namespace PrimaryEditor.Processors.Stylesheet
{
    public sealed class RulesetData
    {
        private readonly RulesetData? _owningRuleset;

        private readonly string _rulesetName;
        private readonly RulesetType _rulesetType;
        private readonly string? _target;
        private readonly string[]? _triggerValues;

        private Dictionary<string, string> _values;
        private Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _altValueLookup;

        private List<RulesetData> _childRulesets;

        internal RulesetData(RulesetData? owningRuleset, string rulesetName, RulesetType rulesetType, string? target, string[]? triggerValues)
        {
            _owningRuleset = owningRuleset;

            _rulesetName = rulesetName;
            _rulesetType = rulesetType;
            _target = target;
            _triggerValues = triggerValues;

            _values = new Dictionary<string, string>();
            _altValueLookup = _values.GetAlternateLookup<ReadOnlySpan<char>>();

            _childRulesets = new List<RulesetData>();
        }

        internal bool TryAddUnique(ReadOnlySpan<char> name, string value)
        {
            return _altValueLookup.TryAdd(name, value);
        }

        internal bool TryAddRuleset(RulesetData ruleset)
        {
            return _childRulesets.AddUnique(ruleset);
        }

        public RulesetKey AsKey() => new RulesetKey(_rulesetName, _rulesetType, _target, new ArraySegment<string>(_triggerValues ?? []));

        public RulesetData? OwningRulset => _owningRuleset;

        public string RulesetName => _rulesetName;
        public RulesetType RulesetType => _rulesetType;
        public string? Target => _target;
        public string[]? TriggerValues => _triggerValues;

        public RODictionary<string, string> Values => _values;

        public ROList<RulesetData> ChildRulesets => _childRulesets;
    }

    public readonly record struct RulesetKey(string RulesetName, RulesetType Type, string? Target, ArraySegment<string> TriggerValues) : IEquatable<RulesetKey>
    {
        public bool Equals(RulesetKey rhs)
        {
            return RulesetName == rhs.RulesetName && Type == rhs.Type && Target == rhs.Target && TriggerValues.SequenceEqual(rhs.TriggerValues);
        }

        public override int GetHashCode()
        {
            int hashCode = HashCode.Combine(RulesetName, Type, Target);
            foreach (string triggerValue in TriggerValues)
            {
                hashCode = HashCode.Combine(triggerValue, RulesetName);
            }

            return hashCode;
        }
    }

    public enum RulesetType : byte
    {
        Default = 0,

        FirstChild,
        LastChild,
        OnlyChild,

        FirstOfType,
        LastOfType,
        OnlyOfType,

        AllChildren,
    }
}
