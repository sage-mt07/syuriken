using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ksql.EntityFramework.Models;

/// <summary>
/// Represents the type of a join operation in KSQL.
/// </summary>
public enum JoinType
{
    /// <summary>
    /// Inner join - returns records that have matching values in both tables.
    /// </summary>
    Inner,

    /// <summary>
    /// Left join - returns all records from the left table and matching records from the right table.
    /// </summary>
    Left,

    /// <summary>
    /// Full outer join - returns all records when there is a match in either the left or right table.
    /// </summary>
    FullOuter
}
/// Represents a join operation between two KSQL sources (streams or tables).
/// </summary>
internal class JoinOperation
{
    /// <summary>
    /// Gets the type of the join operation.
    /// </summary>
    public JoinType Type { get; }

    /// <summary>
    /// Gets the name of the left source.
    /// </summary>
    public string LeftSource { get; }

    /// <summary>
    /// Gets the name of the right source.
    /// </summary>
    public string RightSource { get; }

    /// <summary>
    /// Gets the join condition.
    /// </summary>
    public string JoinCondition { get; }

    /// <summary>
    /// Gets the window specification for the join, if applicable.
    /// </summary>
    public string WindowSpecification { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinOperation"/> class.
    /// </summary>
    /// <param name="type">The type of the join operation.</param>
    /// <param name="leftSource">The name of the left source.</param>
    /// <param name="rightSource">The name of the right source.</param>
    /// <param name="joinCondition">The join condition.</param>
    /// <param name="windowSpecification">The window specification for the join, if applicable.</param>
    public JoinOperation(JoinType type, string leftSource, string rightSource, string joinCondition, string windowSpecification = null)
    {
        Type = type;
        LeftSource = leftSource ?? throw new ArgumentNullException(nameof(leftSource));
        RightSource = rightSource ?? throw new ArgumentNullException(nameof(rightSource));
        JoinCondition = joinCondition ?? throw new ArgumentNullException(nameof(joinCondition));
        WindowSpecification = windowSpecification;
    }

    /// <summary>
    /// Gets the KSQL representation of the join operation.
    /// </summary>
    /// <returns>The KSQL representation of the join operation.</returns>
    public string ToKsqlString()
    {
        var joinTypeString = Type switch
        {
            JoinType.Inner => "JOIN",
            JoinType.Left => "LEFT JOIN",
            JoinType.FullOuter => "FULL OUTER JOIN",
            _ => throw new InvalidOperationException($"Unsupported join type: {Type}")
        };

        var windowClause = string.IsNullOrEmpty(WindowSpecification)
            ? string.Empty
            : $" WITHIN {WindowSpecification}";

        return $"{LeftSource} {joinTypeString} {RightSource} ON {JoinCondition}{windowClause}";
    }
}
/// A generic class to represent the result of a join operation.
/// </summary>
/// <typeparam name="TLeft">The type of the left entity.</typeparam>
/// <typeparam name="TRight">The type of the right entity.</typeparam>
public class JoinResult<TLeft, TRight>
    where TLeft : class
    where TRight : class
{
    /// <summary>
    /// Gets the left entity.
    /// </summary>
    public TLeft Left { get; }

    /// <summary>
    /// Gets the right entity.
    /// </summary>
    public TRight Right { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinResult{TLeft, TRight}"/> class.
    /// </summary>
    /// <param name="left">The left entity.</param>
    /// <param name="right">The right entity.</param>
    public JoinResult(TLeft left, TRight right)
    {
        Left = left;
        Right = right;
    }
}