using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders;

/// <summary>A value provider which affixes the first input to the value of every subsequent input</summary>
internal class AffixValueProvider : BaseValueProvider
{
    /*********
    ** Fields
    *********/
    /// <summary>The token type.</summary>
    private readonly ConditionType Type;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="type">The condition type. This must be one of <see cref="ConditionType.PrefixEach"/> or <see cref="ConditionType.SuffixEach"/>.</param>
    public AffixValueProvider(ConditionType type)
        : base(type, mayReturnMultipleValuesForRoot: false, isDeterministicForInput: true)
    {
        if (type != ConditionType.PrefixEach && type != ConditionType.SuffixEach)
            throw new ArgumentException($"The {nameof(type)} must be one of {ConditionType.PrefixEach} or {ConditionType.SuffixEach}.", nameof(type));

        this.Type = type;
        this.EnableInputArguments(required: true, mayReturnMultipleValues: true, maxPositionalArgs: null);
    }

    /// <inheritdoc />
    public override bool UpdateContext(IContext context)
    {
        bool changed = !this.IsReady;
        this.MarkReady(true);
        return changed;
    }

    /// <inheritdoc />
    public override bool TryValidateInput(IInputArguments input, [NotNullWhen(false)] out string? error)
    {
        if (!base.TryValidateInput(input, out error))
            return false;

        if (input.PositionalArgs.Length == 0)
            error = $"The {this.Name} token requires a {(this.Type == ConditionType.PrefixEach ? "prefix" : "suffix")} argument.";

        return error == null;
    }

    /// <inheritdoc />
    public override bool HasBoundedValues(IInputArguments input, out IInvariantSet allowedValues)
    {
        allowedValues = InvariantSets.From(this.GetValues(input));
        return true;
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetValues(IInputArguments input)
    {
        this.AssertInput(input);

        string? affix = input.GetFirstPositionalArg();
        if (string.IsNullOrWhiteSpace(affix))
            return input.PositionalArgs[1..];

        string[] output = this.Type switch
        {
            ConditionType.PrefixEach => input.PositionalArgs[1..].Select(p => affix + p).ToArray(),
            ConditionType.SuffixEach => input.PositionalArgs[1..].Select(p => p + affix).ToArray(),
            _ => throw new NotSupportedException($"Unimplemented affix type '{this.Type}'.") // should never happen
        };

        return output.Any() ? InvariantSets.From(output) : InvariantSets.Empty;
    }
}
