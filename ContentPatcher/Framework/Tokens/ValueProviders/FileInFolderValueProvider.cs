using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using StardewModdingAPI.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders;

/// <summary>A value provider which returns a list of files in a specified folder in a content pack.</summary>
internal class FileInFolderValueProvider : BaseValueProvider
{
    /*********
    ** Fields
    *********/
    /// <summary>The absolute path to the content pack's folder.</summary>
    private readonly string BaseDirPath;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="baseDirPath">The absolute path to the content pack's folder.</param>
    public FileInFolderValueProvider(string baseDirPath)
        : base(ConditionType.FileInFolder, mayReturnMultipleValuesForRoot: true, isDeterministicForInput: true) // limitation: isDeterministicForInput doesn't account for content pack authors using `patch reload` after adding/deleting files
    {
        this.BaseDirPath = baseDirPath;
        this.EnableInputArguments(required: false, mayReturnMultipleValues: true, maxPositionalArgs: null);
    }

    /// <inheritdoc />
    public override bool UpdateContext(IContext context)
    {
        bool changed = !this.IsReady;
        this.MarkReady(true);
        return changed;
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetValues(IInputArguments input)
    {
        this.AssertInput(input);

        string[] files = this.GetFileNamesFromDirectory(input.HasPositionalArgs
            ? this.GetAbsolutePath(input.GetPositionalSegment())
            : this.BaseDirPath).ToArray();

        return files.Any()
            ? InvariantSets.From(files)
            : InvariantSets.Empty;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the absolute path for a file in the content pack with validation.</summary>
    /// <param name="path">The relative file path.</param>
    /// <exception cref="InvalidOperationException">The path is not relative or contains directory climbing (../).</exception>
    private string? GetAbsolutePath(string? path)
    {
        // get normalized path
        path = PathUtilities.NormalizePath(path);
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // validate
        if (Path.IsPathRooted(path))
            throw new InvalidOperationException($"The {this.Name} token requires a relative path.");
        if (!PathUtilities.IsSafeRelativePath(path))
            throw new InvalidOperationException($"The {this.Name} token requires a relative path and cannot contain directory climbing (../).");

        // get path
        return Path.GetFullPath(
            Path.Combine(this.BaseDirPath, path)
        );
    }

    /// <summary>Get the file names with extensions in a given directory.</summary>
    /// <param name="path">The absolute path to the folder.</param>
    /// <exception cref="InvalidOperationException">The path does not exist.</exception>
    private IEnumerable<string> GetFileNamesFromDirectory(string? path)
    {
        // make sure the directory exists first
        if (!Directory.Exists(path))
            throw new InvalidOperationException($"The {this.Name} token cannot find the specified folder '{path}'.");

        // get file names sans directory
        foreach (string file in Directory.EnumerateFiles(path))
            yield return Path.GetFileName(file);
    }
}
