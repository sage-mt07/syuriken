namespace Ksql.EntityFramework.Models;

/// <summary>
/// Specifies the type of change for a record in a stream or table.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// A new record was inserted.
    /// </summary>
    Insert,

    /// <summary>
    /// An existing record was updated.
    /// </summary>
    Update,

    /// <summary>
    /// An existing record was deleted.
    /// </summary>
    Delete
}
