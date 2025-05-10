using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Configuration;

/// <summary>
/// Represents options for configuring a KSQL table.
/// </summary>
public class TableOptions
{
    /// <summary>
    /// Gets or sets the name of the topic to use for the table.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets the format to use for values in the table.
    /// </summary>
    public ValueFormat ValueFormat { get; set; } = ValueFormat.Avro;

    /// <summary>
    /// Gets or sets the names of the key columns for the table.
    /// </summary>
    public List<string> KeyColumns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the partitioning column for the table.
    /// </summary>
    public string? PartitionBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp column for the table.
    /// </summary>
    public string? TimestampColumn { get; set; }

    /// <summary>
    /// Gets or sets the format for the timestamp column.
    /// </summary>
    public string? TimestampFormat { get; set; }

    /// <summary>
    /// Specifies the key columns to use for the table.
    /// </summary>
    /// <param name="columnNames">The names of the key columns.</param>
    /// <returns>The table options for method chaining.</returns>
    public TableOptions WithKeyColumns(params string[] columnNames)
    {
        KeyColumns.Clear();
        KeyColumns.AddRange(columnNames);
        return this;
    }

    /// <summary>
    /// Specifies the topic to use for the table.
    /// </summary>
    /// <param name="topicName">The name of the topic.</param>
    /// <returns>The table options for method chaining.</returns>
    public TableOptions WithTopic(string topicName)
    {
        TopicName = topicName;
        return this;
    }

    /// <summary>
    /// Specifies the format to use for values in the table.
    /// </summary>
    /// <param name="format">The value format.</param>
    /// <returns>The table options for method chaining.</returns>
    public TableOptions WithValueFormat(ValueFormat format)
    {
        ValueFormat = format;
        return this;
    }

    /// <summary>
    /// Specifies the partitioning column for the table.
    /// </summary>
    /// <param name="columnName">The name of the column to partition by.</param>
    /// <returns>The table options for method chaining.</returns>
    public TableOptions WithPartitionBy(string columnName)
    {
        PartitionBy = columnName;
        return this;
    }

    /// <summary>
    /// Specifies the timestamp column and format for the table.
    /// </summary>
    /// <param name="columnName">The name of the timestamp column.</param>
    /// <param name="format">The format of the timestamp.</param>
    /// <returns>The table options for method chaining.</returns>
    public TableOptions WithTimestamp(string columnName, string? format = null)
    {
        TimestampColumn = columnName;
        TimestampFormat = format;
        return this;
    }
}
