# KSQL Query Examples for LINQ Provider

This document outlines example KSQL queries that the LINQ to KSQL provider aims to support.

## 1. Basic Selection

**Description:** Select all columns from a stream.
```linq
var query = context.MyStream;
```
```ksql
SELECT * FROM MyStream EMIT CHANGES;
```

## 2. Selection with Filter (WHERE clause)
```linq
var query = context.MyStream.Where(s => s.Id == "123" && s.Amount > 100);

```
```ksql
SELECT * FROM MyStream WHERE Id = '123' AND Amount > 100 EMIT CHANGES;

```
##  3.Projection (SELECT specific columns)
```linq
var query = context.MyStream.Select(s => new { s.RowKey, s.Id, s.Name });
```
```ksql
SELECT RowKey, Id, Name FROM MyStream EMIT CHANGES;
```

## 4.Stream-Table Join (INNER JOIN)

```linq
var query = context.MyStream
    .Join(
        context.MyTable,
        stream => stream.ForeignKey,
        table => table.RowKey,
        (stream, table) => new { StreamId = stream.Id, StreamData = stream.Data, TableInfo = table.Info }
    );

```
```ksql
SELECT s.Id AS StreamId, s.Data AS StreamData, t.Info AS TableInfo
FROM MyStream s
INNER JOIN MyTable t ON s.ForeignKey = t.RowKey
EMIT CHANGES;

```

## 5. Stream-Stream Join with Window

```linq
// Assuming WindowSpecification can be passed or configured
var query = context.Stream1
    .Join(
        context.Stream2,
        s1 => s1.JoinKey,
        s2 => s2.JoinKey,
        (s1, s2) => new { Id1 = s1.Id, Value2 = s2.Value },
        Window.Hopping(TimeSpan.FromDays(7), TimeSpan.FromHours(1)) // Example window
    );

```
```ksql
SELECT s1.Id AS Id1, s2.Value AS Value2
FROM Stream1 s1
INNER JOIN Stream2 s2 WITHIN 7 DAYS ON s1.JoinKey = s2.JoinKey
EMIT CHANGES;

```

## 6. Aggregation (COUNT with GROUP BY)

```linq
// This might translate to a more complex KSQL or require specific GroupBy/Aggregate methods
var query = context.MyStream
    .Window(Window.Tumbling(TimeSpan.FromHours(1))) // Assuming windowing can be applied
    .GroupBy(s => s.Category)
    .Select(g => new { Category = g.Key, CategoryCount = g.Count() });

```
```ksql
SELECT Category, COUNT(*) AS CategoryCount
FROM MyStream
WINDOW TUMBLING (SIZE 1 HOUR)
GROUP BY Category
EMIT CHANGES;

```


## 7. Simple Aggregation (COUNT)
```linq
var count = context.MyStream.Count(); // Translation to KSQL needs careful consideration.

```
```ksql
-- For a materialized table:
-- SELECT COUNT(*) FROM my_aggregated_table;
-- For a stream, it's typically part of a windowed aggregation.

```
## 8.Limit Results (TAKE)

```linq
var query = context.MyStream.Take(10); // Translation to KSQL needs to consider query type (push/pull).

```
```ksql
SELECT * FROM MyStream_TABLE LIMIT 10;

```
## 9. Filtering with LIKE

```linq
var query = context.MyStream.Where(s => s.Name.StartsWith("Test")); // Or a custom .Like() extension

```
```ksql
SELECT * FROM MyStream WHERE Name LIKE 'Test%' EMIT CHANGES;

```
## 10. Ordering (ORDER BY)

```linq
var query = context.MyStream.OrderByDescending(s => s.Timestamp).Take(5);

```
```ksql
SELECT * FROM MyStream_TABLE ORDER BY Timestamp DESC LIMIT 5;

```
## 11. Stream-Table Join with Multiple Keys + +Description: Join a stream with a table using multiple keys for the join condition

```linq
var query = context.MyStream
    .Join(
        context.MyTable,
        stream => new { stream.KeyPart1, stream.KeyPart2 }, // Composite key from stream
   table => new { table.KeyPart1, table.KeyPart2 },   // Composite key from table
   (stream, table) => new { stream.Id, stream.Data, table.Info }

    );
```
```ksql
SELECT s.Id, s.Data, t.Info +FROM MyStream s +INNER JOIN MyTable t ON s.KeyPart1 = t.KeyPart1 AND s.KeyPart2 = t.KeyPart2 +EMIT CHANGES;
```

