namespace Pidgin;

/// <summary>
/// An uninteresting type with only one value (<see cref="Value"/>) and no fields.
/// Like <c>void</c>, but valid as a type parameter.
/// </summary>
public sealed class Unit
{
    private Unit()
    {
    }

    /// <summary>
    /// The single unique <see cref="Unit"/> value.
    /// </summary>
    public static Unit Value { get; } = new Unit();
}
