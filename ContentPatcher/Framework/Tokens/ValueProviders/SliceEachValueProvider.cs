using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders;

/// <summary>A value provider which slices a number of characters off of each input</summary>
internal class SliceEachValueProvider : BaseValueProvider
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SliceEachValueProvider()
        : base(ConditionType.SliceEach, mayReturnMultipleValuesForRoot: false, isDeterministicForInput: true)
    {
        this.EnableInputArguments(required: false, mayReturnMultipleValues: true, maxPositionalArgs: null);
        this.ValidNamedArguments = InvariantSets.FromValue("sliceCount");
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
            if (input.NamedArgs["sliceCount"].Parsed.Length > 1)
                error = $"The {this.Name} token only accepts a single value for the 'sliceCount' named argument.";

            if (!int.TryParse(input.NamedArgs["sliceCount"].Parsed[0], out _))
                error = $"Can't parse sliceCount '{input.NamedArgs["sliceCount"].Parsed[0]}' as an integer";
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

        if (!input.HasNamedArgs)
            return input.PositionalArgs;

        if (!int.TryParse(input.NamedArgs["sliceCount"].Parsed[0], out int sliceCount))
            throw new InvalidOperationException($"Can't parse sliceCount '{input.NamedArgs["sliceCount"].Parsed[0]}' as an integer"); // should never happen since we check the input in TryValidateInput

        string[] output = input.PositionalArgs.Select(p =>
            sliceCount >= 0
                ? p.Length <= sliceCount ? string.Empty : p[sliceCount..]
                : p.Length <= -sliceCount ? string.Empty : p[..^-sliceCount]
        ).Where(p => !string.IsNullOrEmpty(p)).ToArray();

        return output.Any() ? InvariantSets.From(output) : InvariantSets.Empty;
    }
}
