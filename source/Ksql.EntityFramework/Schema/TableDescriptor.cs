using Ksql.EntityFramework.Configuration;

namespace Ksql.EntityFramework.Schema;

/// <summary>
/// Describes a KSQL table and its associated topic.
/// </summary>
internal class TableDescriptor
{
    /// <summary>
    /// Gets or sets the name of the table.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic descriptor for the table.
    /// </summary>
    public TopicDescriptor TopicDescriptor { get; set; } = new TopicDescriptor();

    /// <summary>
    /// Gets or sets the options for the table.
    /// </summary>
    public TableOptions Options { get; set; } = new TableOptions();
}
