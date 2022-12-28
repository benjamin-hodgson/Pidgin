namespace Pidgin.Expression;

/// <summary>
/// The associativity of the binary operator: left-associative, right-associative, or non-associative.
/// </summary>
public enum BinaryOperatorType
{
    /// <summary>
    /// Denotes a non-associative binary operator
    /// </summary>
    NonAssociative,

    /// <summary>
    /// Denotes a left-associative binary operator
    /// </summary>
    LeftAssociative,

    /// <summary>
    /// Denotes a right-associative binary operator
    /// </summary>
    RightAssociative
}
