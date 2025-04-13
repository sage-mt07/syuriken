# KSQL Entity Framework 要件定義書

## 1. 概要

KSQL Entity Frameworkは、C#プログラマがEntityFrameworkライクなAPIを使用してKSQL/KafkaStreamsを操作できるようにするライブラリです。トピック中心の設計、POCOベースのクエリ定義、LINQライクなストリーム操作を特徴とします。

## 2. 基本原則

1. **トピック中心設計**: すべての操作はKafkaトピックを起点とする
2. **型安全性**: C#の型システムを活用してスキーマの整合性を確保
3. **使い慣れたAPI**: EntityFrameworkに類似したAPIデザイン
4. **LINQサポート**: ストリーム処理をLINQクエリとして表現
5. **段階的デプロイ**: 基本機能から高度な機能へと段階的に実装

## 3. 主要コンポーネント

### 3.1 トピック (Kafka Topics)

#### トピック定義
```csharp
// 属性によるマッピング
[Topic("orders", PartitionCount = 12, ReplicationFactor = 3)]
public class Order 
{
    [Key]
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OrderTime { get; set; }
}

// Fluent API
modelBuilder.Entity<Order>()
    .ToTopic("orders")
    .WithPartitions(12)
    .WithReplicationFactor(3);
```

#### トピック構成
- パーティション設定: パーティション数、パーティショニング戦略
- レプリケーション設定: レプリケーションファクター、ISRの最小数
- 保持ポリシー: メッセージの保持期間、サイズ制限
- 圧縮設定: トピックレベルの圧縮方式

#### スキーマ管理
- 自動スキーマ登録: POCOからAvroスキーマを生成し登録
- 互換性設定: スキーマ互換性ポリシーの指定
- スキーマ進化: スキーマバージョンの管理とマイグレーション

#### トピック操作
```csharp
// トピック作成
await context.EnsureTopicCreatedAsync<Order>();

// トピックの削除
await context.Database.DropTopicAsync("orders");
```

### 3.2 ストリーム (KSQL Streams)

#### ストリーム定義
```csharp
// コンテキスト内でのストリーム定義
public class KsqlContext : KsqlDbContext
{
    public IKsqlStream<Order> Orders { get; set; }
}

// 自動ストリーム作成
await context.EnsureStreamCreatedAsync<Order>();
```

#### ストリーム設定
```csharp
// タイムスタンプ列の指定
[Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
public DateTimeOffset TransactionTime { get; set; }

// キー設定
[Key]
public string CustomerId { get; set; }
```

#### ストリーム処理
```csharp
// フィルタリング
var highValueOrders = context.Orders
    .Where(o => o.Amount > 1000)
    .Select(o => new { o.OrderId, o.CustomerId, o.Amount });

// ウィンドウ処理
var hourlyStats = context.Orders
    .Window(TumblingWindow.Of(TimeSpan.FromHours(1)))
    .GroupBy(o => o.CustomerId)
    .Select(g => new HourlyStats 
    { 
        CustomerId = g.Key,
        Hour = g.Window.Start,
        OrderCount = g.Count() 
    });

// ウォーターマーク設定
context.Orders
    .WithWatermark(o => o.OrderTime, TimeSpan.FromMinutes(5))
    .Window(...);
```

### 3.3 テーブル (KSQL Tables)

#### テーブル定義とLATEST_BY_OFFSET/EARLIEST_BY_OFFSET
KSQL では、テーブルの作成時に `LATEST_BY_OFFSET` と `EARLIEST_BY_OFFSET` 関数を使用して、重複するキーに対する値の選択方法を制御できます。C# では以下のように実装できます：

```sql
-- KSQLでの例
CREATE TABLE customer_latest_orders AS
SELECT 
    customer_id,
    LATEST_BY_OFFSET(order_id) AS latest_order_id,
    LATEST_BY_OFFSET(order_time) AS latest_order_time,
    LATEST_BY_OFFSET(amount) AS latest_amount
FROM orders
GROUP BY customer_id;
```

```csharp
// KSQLをC#風に表現する例
await context.CreateTableAsync("customer_latest_orders",
    from o in context.Orders
    group o by o.CustomerId into g
    select new {
        CustomerId = g.Key,
        LatestOrderId = g.LatestByOffset(o => o.OrderId),
        LatestOrderTime = g.LatestByOffset(o => o.OrderTime),
        LatestAmount = g.LatestByOffset(o => o.Amount)
    });
```
```csharp
// ストリームからテーブルを作成
public IKsqlTable<OrderSummary> OrderSummaries => 
    CreateTable<OrderSummary>("order_summaries_table", 
        builder => builder.FromStream(Orders)...);

// トピックから直接テーブルを作成
public IKsqlTable<Customer> CustomerTable => 
    CreateTable<Customer>("customer_table", 
        builder => builder.FromTopic<Customer>("customer_data"));
```

#### テーブル操作
```csharp
// テーブル作成
await context.EnsureTableCreatedAsync(context.OrderSummaries);

// プライマリキーによる取得
var customer = await context.Customers.FindAsync("CUST001");

// クエリによる取得
var highValueCustomers = await context.Customers
    .Where(c => c.TotalPurchases > 10000)
    .OrderByDescending(c => c.TotalPurchases)
    .ToListAsync();

// テーブル更新
customer.Name = "Updated Name";
await context.SaveChangesAsync();

// テーブルレコード削除
context.Customers.Remove(customer);
await context.SaveChangesAsync();
```

#### 集約操作
```csharp
// グループ化と集約
var customerStats = context.Orders
    .GroupBy(o => o.CustomerId)
    .Aggregate(g => new CustomerStats 
    { 
        CustomerId = g.Key, 
        TotalAmount = g.Sum(o => o.Amount),
        OrderCount = g.Count()
    });

// LATEST_BY_OFFSET - 最新値の取得
var latestCustomerOrders = context.Orders
    .GroupBy(o => o.CustomerId)
    .Aggregate(g => new CustomerLatestOrder
    {
        CustomerId = g.Key,
        LatestOrderId = g.LatestByOffset(o => o.OrderId),
        LatestOrderTime = g.LatestByOffset(o => o.OrderTime),
        LatestAmount = g.LatestByOffset(o => o.Amount)
    });

// EARLIEST_BY_OFFSET - 最古値の取得
var firstTimeCustomers = context.Orders
    .GroupBy(o => o.CustomerId)
    .Aggregate(g => new CustomerFirstOrder
    {
        CustomerId = g.Key,
        FirstOrderId = g.EarliestByOffset(o => o.OrderId),
        FirstOrderTime = g.EarliestByOffset(o => o.OrderTime),
        FirstAmount = g.EarliestByOffset(o => o.Amount)
    });

// 両方を組み合わせた使用例
var customerOrderRange = context.Orders
    .GroupBy(o => o.CustomerId)
    .Aggregate(g => new CustomerOrderRange
    {
        CustomerId = g.Key,
        FirstOrderTime = g.EarliestByOffset(o => o.OrderTime),
        LatestOrderTime = g.LatestByOffset(o => o.OrderTime),
        OrderCount = g.Count(),
        TotalSpent = g.Sum(o => o.Amount),
        LoyaltyDays = EF.Functions.DateDiffDays(
            g.EarliestByOffset(o => o.OrderTime),
            g.LatestByOffset(o => o.OrderTime))
    });
```

#### テーブル結合
```csharp
// 単一キー結合
var query = from o in context.Orders
            join c in context.Customers
            on o.CustomerId equals c.CustomerId
            select new { o.OrderId, c.CustomerName, o.Amount };

// 複合キー結合
var query = from o in context.Orders
            join c in context.Customers
            on new { o.CustomerId, o.Region } equals 
               new { c.CustomerId, c.Region }
            select new { o.OrderId, c.CustomerName, o.Amount };

// 3テーブル結合
var query = from o in context.Orders
            join c in context.Customers
            on o.CustomerId equals c.CustomerId
            join p in context.Products
            on o.ProductId equals p.ProductId
            select new {
                o.OrderId,
                c.CustomerName,
                p.ProductName,
                o.Quantity,
                o.Amount
            };
```

### 3.4 クエリと購読

#### プッシュクエリ
```csharp
// リアルタイム購読
await foreach (var order in highValueOrders.SubscribeAsync())
{
    Console.WriteLine($"Received high-value order: {order.OrderId}");
}
```

#### プルクエリ
```csharp
// ポイントクエリ
var customerSummary = await context.CustomerSummaries
    .Where(s => s.CustomerId == "CUST001")
    .FirstOrDefaultAsync();
```

#### 変更の監視
```csharp
// テーブル変更の購読
await foreach (var change in context.Customers.ObserveChangesAsync())
{
    if (change.ChangeType == ChangeType.Insert)
    {
        Console.WriteLine($"New customer: {change.Entity.Name}");
    }
}
```

### Kafkaのメタデータを利用する例

Kafkaのメタデータを利用して、トピックやパーティションの情報を取得する方法を以下に示します。

#### メタデータ取得のコード例
```csharp
using Confluent.Kafka;

class KafkaMetadataExample
{
    public static void Main(string[] args)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "metadata-example-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            // メタデータを取得
            var metadata = consumer.GetMetadata(TimeSpan.FromSeconds(10));

            Console.WriteLine("Cluster ID: " + metadata.ClusterId);
            Console.WriteLine("Brokers:");
            foreach (var broker in metadata.Brokers)
            {
                Console.WriteLine($"  Broker: {broker.BrokerId}, Host: {broker.Host}, Port: {broker.Port}");
            }

            Console.WriteLine("Topics:");
            foreach (var topic in metadata.Topics)
            {
                Console.WriteLine($"  Topic: {topic.Topic}, Error: {topic.Error}");
                foreach (var partition in topic.Partitions)
                {
                    Console.WriteLine($"    Partition: {partition.PartitionId}, Leader: {partition.Leader}, Replicas: {string.Join(",", partition.Replicas)}, InSyncReplicas: {string.Join(",", partition.InSyncReplicas)}");
                }
            }
        }
    }
}
```

#### 説明
1. **ConsumerConfigの設定**:
   - `BootstrapServers`: Kafkaブローカーのアドレスを指定します。
   - `GroupId`: コンシューマーグループのIDを指定します。
   - `AutoOffsetReset`: オフセットが見つからない場合の動作を指定します。

2. **GetMetadataメソッド**:
   - Kafkaクラスターのメタデータを取得します。
   - トピック、パーティション、ブローカーの情報を含みます。

3. **出力例**:
   - クラスターID、ブローカー情報、トピック情報、パーティション情報をコンソールに出力します。

#### 使用シナリオ
- **トラブルシューティング**: クラスターの構成やトピックの状態を確認する。
- **モニタリング**: パーティションのリーダーやレプリカの状態を監視する。
- **動的設定**: メタデータを基に動的にトピックやパーティションを操作する。

## 4. POCO (Plain Old CLR Objects) の設計

### 4.1 基本定義
- シンプルなC#クラス: 特別な基底クラス不要
- 標準的なプロパティ: 一般的な.NET型のサポート
- コレクション・複合型: List、Dictionaryなどのサポート

### 4.2 特殊型のサポート

#### Decimal型の精度指定
```csharp
[DecimalPrecision(precision: 18, scale: 4)]
public decimal Amount { get; set; }
```

#### DateTime/DateTimeOffset
```csharp
// DateTimeOffset推奨（タイムゾーン情報保持）
public DateTimeOffset TransactionTime { get; set; }

// または設定付きのDateTime
[DateTimeFormat(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Locale = "en-US")]
public DateTime OrderDate { get; set; }
```

#### null許容性
```csharp
// C#標準の ?修飾子を使用
public int? OptionalQuantity { get; set; }
```

#### 数値型のデフォルト値
```csharp
[DefaultValue(0)]
public int Quantity { get; set; }
```

## 5. プロデュース/コンシューム操作

### 5.1 プロデューサー (データ送信)
```csharp
// 単一レコードのプロデュース
await context.Orders.ProduceAsync(new Order { OrderId = "123", Amount = 100 });

// キーを明示的に指定
await context.Orders.ProduceAsync("customer-123", 
    new Order { OrderId = "123", CustomerId = "customer-123", Amount = 100 });

// バッチプロデュース
await context.Orders.ProduceBatchAsync(ordersList);

// EntityFramework風のAPI
context.Orders.Add(new Order { OrderId = "123", Amount = 100 });
context.Orders.Add(new Order { OrderId = "124", Amount = 200 });
await context.SaveChangesAsync(); // バッチでプロデュース
```

### 5.2 コンシューマー (データ受信)
```csharp
// プル型クエリ (テーブル)
var highValueOrders = await context.OrdersTable
    .Where(o => o.Amount > 1000)
    .ToListAsync();

// プッシュ型クエリ (ストリーム購読)
await foreach (var order in context.Orders
    .Where(o => o.Amount > 1000)
    .SubscribeAsync())
{
    Console.WriteLine($"Received order: {order.OrderId}");
}
```

### 5.3 トランザクション処理
```csharp
// トランザクション処理
using (var transaction = await context.BeginTransactionAsync())
{
    try
    {
        context.Orders.Add(new Order { OrderId = "123", Amount = 100 });
        context.Orders.Add(new Order { OrderId = "124", Amount = 200 });
        
        // 他のコンテキストやDB操作を含めた調整
        await dbContext.SaveChangesAsync();
        
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.AbortAsync();
        throw;
    }
}
```

## 6. エラー処理とデータ品質

### 6.1 エラー処理戦略
```csharp
// エラー処理ポリシーの設定
context.Options.DeserializationErrorPolicy = ErrorPolicy.Skip;

// エラーハンドリング付きストリーム処理
var processedOrders = context.Orders
    .OnError(ErrorAction.Skip)  // エラーレコードをスキップ
    .Map(order => ProcessOrder(order))
    .WithRetry(3);  // 失敗時に3回リトライ
```

### 6.2 デッドレターキュー
```csharp
// デッドレターキューの設定
context.Options.DeadLetterQueue = "order_errors";

// エラー情報付きでデッドレターキューに送信
context.Options.DeadLetterQueueErrorHandler = (data, error) => 
{
    return new DeadLetterMessage
    {
        OriginalData = data,
        ErrorMessage = error.Message,
        Timestamp = DateTime.UtcNow
    };
};
```

## 7. テーブル管理操作

### 7.1 テーブル作成と更新
```csharp
// テーブルの作成
await context.Database.CreateTableAsync<Customer>("customers", 
    options => options
        .WithKeyColumns(c => c.CustomerId)
        .WithTopic("customer_data")
        .WithValueFormat(ValueFormat.Avro));

// テーブルスキーマの更新
await context.Database.ExecuteKsqlAsync(@"
    ALTER TABLE customers
    ADD COLUMN loyalty_level VARCHAR;
");
```

### 7.2 テーブルの再構築と管理
```csharp
// テーブルの再構築
await context.Database.DropTableAsync("customers");
await context.Database.CreateTableAsync<Customer>(...);

// メタデータの更新
await context.RefreshMetadataAsync();
```

## 8. リリース計画

### フェーズ1: 基盤構築 (v0.1-v0.3)
- トピック定義と基本操作
- スキーマ管理
- 基本的なストリーム操作

### フェーズ2: 高度なストリーム処理 (v0.4-v0.6)
- テーブル操作
- 集約操作
- ウィンドウ操作

### フェーズ3: 高度なデータ連携 (v0.7-v0.9)
- ストリーム結合
- 複雑なトポロジー
- エラー処理とリトライ

### フェーズ4: エンタープライズ機能 (v1.0+)
- 分散トレーシングとメトリクス
- トランザクショナルメッセージング
- マルチクラスタサポート

## 9. アーキテクチャ概要

### コアコンポーネント
1. **KsqlDbContext**: メインのエントリーポイント
2. **TopicDescriptor**: Avroスキーマ定義とトピック設定を管理
3. **QueryTranslator**: LINQ式からKSQLクエリへの変換を担当
4. **StreamProcessor**: ストリーム処理のランタイムエンジン
5. **SchemaManager**: Avroスキーマとスキーマレジストリの相互作用を管理

### 主要インターフェース
```csharp
// ストリームインターフェース
public interface IKsqlStream<T> : IQueryable<T>
{
    Task<long> ProduceAsync(T entity);
    IAsyncEnumerable<T> SubscribeAsync();
    // 他のストリーム操作
}

// テーブルインターフェース
public interface IKsqlTable<T> : IQueryable<T>
{
    Task<T> GetAsync(object key);
    Task<bool> InsertAsync(T entity);
    // 他のテーブル操作
}

// コンテキストのインターフェース
public interface IKsqlDbContext : IDisposable, IAsyncDisposable
{
    IKsqlStream<T> CreateStream<T>(string name);
    IKsqlTable<T> CreateTable<T>(string name);
    Task<IKsqlTransaction> BeginTransactionAsync();
    // 他のコンテキスト操作
}
```

## 10. エンベロープパターンとスキーマ拡張

### 10.1 エンベロープパターンの基本設計
エンベロープパターンでは、メッセージの本来の業務データ（Payload）と、それに付随するメタデータを分離してラッピングします。以下のような共通のラッパークラスを定義します。

```csharp
public class MessageEnvelope<T>
{
    public T Payload { get; set; }
}
```

この設計では、アプリケーションが通常処理する際は `Payload` 部分のみを対象とし、joinのキーやフィルタ条件も業務データ（Payload）から抽出できます。

### 10.2 Avroスキーマへの組み込み
ksqlでjoin処理を行う際には、Kafkaの値に含まれるフィールドが対象となりますので、エンベロープ全体またはその一部として扱います。以下はAvroスキーマの例です。

```json
{
  "namespace": "com.example",
  "type": "record",
  "name": "OrderEnvelope",
  "fields": [
    {
      "name": "Payload",
      "type": {
        "type": "record",
        "name": "Order",
        "fields": [
          {"name": "orderId", "type": "string"},
          {"name": "customerId", "type": "string"},
          {"name": "amount", "type": "double"},
          {"name": "orderTime", "type": "long"}
        ]
      }
    }
  ]
}
```

このようにすることで、プロダクションのメッセージは通常の業務データとして扱われます。

### 10.3 ksqlでのjoin処理

#### 10.3.1 joinキーの抽出
エンベロープ形式の場合、ksql側では `Payload` の中からjoinキーを抽出する必要があります。以下はjoin処理の例です。

```sql
CREATE STREAM joined_orders AS
  SELECT a.Payload->orderId AS orderIdA,
         b.Payload->orderId AS orderIdB,
         a.Payload->customerId AS customerId
  FROM orders_envelope_stream a
  JOIN orders_envelope_stream b
    ON a.Payload->customerId = b.Payload->customerId;
```

この例では、joinの条件に業務上のキー（`customerId`）を使用して処理を行います。