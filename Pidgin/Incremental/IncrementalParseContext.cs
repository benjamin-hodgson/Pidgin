using System.Collections.Immutable;

namespace Pidgin.Incremental;

/// <summary>
/// Represents the context for an incremental parse operation,
/// tracking the list of edits applied to the input and the cache of previous parse results.
/// </summary>
public class IncrementalParseContext
{
    /// <summary>
    /// Gets the list of edits that have been applied to the input.
    /// </summary>
    public ImmutableList<EditInfo> Edits { get; }

    internal CachedParseResultTable ResultCache { get; }

    internal IncrementalParseContext(ImmutableList<EditInfo> edits, CachedParseResultTable resultCache)
    {
        Edits = edits;
        ResultCache = resultCache;
    }

    /// <summary>
    /// Returns a new <see cref="IncrementalParseContext"/> representing
    /// this instance with an edit applied.
    /// </summary>
    /// <remarks>
    /// It is your responsibility to keep the input stream in sync with the <see cref="EditInfo"/>s
    /// provided to this method.
    /// </remarks>
    /// <param name="edit">The edit to add.</param>
    /// <returns>A new <see cref="IncrementalParseContext"/> with the edit applied.</returns>
    public IncrementalParseContext AddEdit(EditInfo edit)
        => new(Edits.Add(edit), ResultCache);

    internal bool IsValid(LocationRange oldRange)
    {
        // chronological order
        foreach (var edit in Edits)
        {
            if (edit.InputRange.OverlapsOrExtends(oldRange))
            {
                return false;
            }

            var newStart = edit.Shift(oldRange.Start);
            if (!newStart.HasValue)
            {
                // probably unreachable?
                return false;
            }

            if (newStart != oldRange.Start)
            {
                oldRange = oldRange with { Start = newStart.Value };
            }
        }

        return true;
    }

    /// <summary>
    /// Turn a location in the new (edited) input stream into
    /// a corresponding location in the original one one.
    /// </summary>
    /// <param name="newLocation">The location in the new (edited) input stream.</param>
    /// <returns>The corresponding location in the original input stream.</returns>
    internal long? Unshift(long newLocation)
    {
        // reverse chronological order
        foreach (var edit in Edits.Reverse())
        {
            var unshifted = edit.Unshift(newLocation);
            if (!unshifted.HasValue)
            {
                return null;
            }

            newLocation = unshifted.Value;
        }

        return newLocation;
    }
}
