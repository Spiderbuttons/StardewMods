using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders;

/// <summary>A value provider which slices a number of characters determined by the first input off of each subsequent input</summary>
internal class SliceEachValueProvider : BaseValueProvider
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SliceEachValueProvider()
        : base(ConditionType.SliceEach, mayReturnMultipleValuesForRoot: false, isDeterministicForInput: true)
    {
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
            error = $"The {this.Name} token requires a slice count argument.";
        else if (!int.TryParse(input.GetFirstPositionalArg(), out _))
            error = $"Can't parse slice count '{input.GetFirstPositionalArg()}' as an integer";

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

        string? rawSliceCount = input.GetFirstPositionalArg();
        if (string.IsNullOrWhiteSpace(rawSliceCount))
            return input.PositionalArgs[1..];

        if (!int.TryParse(rawSliceCount, out int sliceCount))
            throw new InvalidOperationException($"Can't parse slice count '{rawSliceCount}' as an integer"); // should never happen since we check the input in TryValidateInput

        string[] output = input.PositionalArgs[1..].Select(p =>
            sliceCount >= 0
                ? p.Length <= sliceCount ? string.Empty : p[sliceCount..]
                : p.Length <= -sliceCount ? string.Empty : p[..^-sliceCount]
        ).Where(p => !string.IsNullOrEmpty(p)).ToArray();

        return output.Any() ? InvariantSets.From(output) : InvariantSets.Empty;
    }
}
