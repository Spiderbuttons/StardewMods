﻿using CommunityToolkit.Diagnostics;
using Microsoft.Xna.Framework;

namespace AtraShared.Content.RawTexData;

public static class IRawTextureDataExtensions
{
    public static void PatchImage(
        this IRawTextureData raw,
        IRawTextureData source,
        Rectangle? sourceArea = null,
        Rectangle? targetArea = null,
        PatchMode patchMode = PatchMode.Replace)
    {
        Guard.IsNotNull(raw);
        Guard.IsNotNull(source);

        // Calculate bounds.
        sourceArea ??= new(0, 0, source.Width, source.Height);
        targetArea ??= new(0, 0, Math.Min(source.Width, raw.Width), Math.Min(source.Height, raw.Height));

        raw.ApplyPatch(source, sourceArea.Value, targetArea.Value, patchMode);

    }

    private static void ApplyPatch(
        this IRawTextureData raw,
        IRawTextureData source,
        Rectangle sourceArea,
        Rectangle targetArea,
        PatchMode patchMode
        )
    {
        // validate
        if (sourceArea.X < 0 || sourceArea.Y < 0 || sourceArea.Right > source.Width || sourceArea.Left > source.Height)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException($"Source rectangle appears to be out of range.");
        }

        if (targetArea.X < 0 || targetArea.Y < 0 || targetArea.Right > raw.Width || targetArea.Right > raw.Height)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException($"Target rectangle appears to be out of range.");
        }

        if (targetArea.Width != sourceArea.Width || targetArea.Height != sourceArea.Height)
        {
            ThrowHelper.ThrowArgumentException($"Size of target and source rectangles appear to be different.");
        }
    }
}
