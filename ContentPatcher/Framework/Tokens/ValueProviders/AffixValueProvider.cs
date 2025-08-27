using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders;

/// <summary>A value provider which affixes a string to the start or end of every input</summary>
internal class AffixValueProvider : BaseValueProvider
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public AffixValueProvider()
        : base(ConditionType.AffixEach, mayReturnMultipleValuesForRoot: false, isDeterministicForInput: true)
    {
        this.EnableInputArguments(required: false, mayReturnMultipleValues: true, maxPositionalArgs: null);
        this.ValidNamedArguments = InvariantSets.From(["prefix", "suffix"]);
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

        if (input.HasNamedArgs)
        {
            if (input.NamedArgs.TryGetValue("prefix", out IInputArgumentValue? prefix) && prefix.Parsed.Length > 1)
                error = $"The {this.Name} token only accepts a single value for the 'prefix' argument.";
            else if (input.NamedArgs.TryGetValue("suffix", out IInputArgumentValue? suffix) && suffix.Parsed.Length > 1)
                error = $"The {this.Name} token only accepts a single value for the 'suffix' argument.";
        }

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

        if (!input.HasPositionalArgs)
            return InvariantSets.Empty;

        if (!input.HasNamedArgs)
            return input.PositionalArgs;

        string prefix = input.NamedArgs.TryGetValue("prefix", out IInputArgumentValue? prefixArg)
            ? prefixArg.Raw // we already validated that there is only one value in this argument, so we can grab the raw
            : string.Empty;
        string suffix = input.NamedArgs.TryGetValue("suffix", out IInputArgumentValue? suffixArg)
            ? suffixArg.Raw
            : string.Empty;

        string[] output = input.PositionalArgs.Select(value => $"{prefix}{value}{suffix}").ToArray();

        return output.Any() ? InvariantSets.From(output) : InvariantSets.Empty;
    }
}
