# 今後の拡張予定

## エンベロープパターンとスキーマ拡張

### エンベロープパターンの基本設計
エンベロープパターンでは、メッセージの本来の業務データ（Payload）と、それに付随するメタデータを分離してラッピングします。以下のような共通のラッパークラスを定義します。

```csharp
public class MessageEnvelope<T>
{
    public T Payload { get; set; }
}
```

この設計では、アプリケーションが通常処理する際は `Payload` 部分のみを対象とし、joinのキーやフィルタ条件も業務データ（Payload）から抽出できます。

### Avroスキーマへの組み込み
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

### ksqlでのjoin処理

#### joinキーの抽出
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

## Kafkaのメタデータ利用

### メタデータの活用方法
Kafkaのメタデータを利用して、トピックやパーティションの情報を取得し、システムの動的設定やモニタリングに役立てることができます。

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

#### 使用シナリオ
- **トラブルシューティング**: クラスターの構成やトピックの状態を確認する。
- **モニタリング**: パーティションのリーダーやレプリカの状態を監視する。
- **動的設定**: メタデータを基に動的にトピックやパーティションを操作する。

### メタデータの利点
1. **システムの可視性向上**: クラスターやトピックの状態を把握しやすくなります。
2. **動的なリソース管理**: メタデータを活用して、リソースの動的な割り当てや調整が可能です。
3. **エラー検出の迅速化**: メタデータを利用することで、トピックやパーティションの問題を迅速に特定できます。

## 曳光弾の例

曳光弾（トレーサーメッセージ）は、システム全体のデータフローを確認するために使用される特別なメッセージです。以下は、曳光弾を使用してトピックからストリーム、テーブルへのデータフローを確認する例です。

### 曳光弾メッセージの設計
曳光弾メッセージは、通常の業務データに特別なフラグを追加することで実現します。以下は、C#での曳光弾メッセージの例です。

```csharp
public class TracerMessage<T>
{
    public T Payload { get; set; }
    public bool IsTracer { get; set; } = true;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 曳光弾メッセージの送信
以下のコードは、曳光弾メッセージをKafkaトピックに送信する例です。

```csharp
var tracerMessage = new TracerMessage<Order>
{
    Payload = new Order
    {
        OrderId = "TRACER-123",
        CustomerId = "TEST-CUSTOMER",
        Amount = 0,
        OrderTime = DateTime.UtcNow
    }
};

await kafkaProducer.ProduceAsync("orders", tracerMessage);
```

### 曳光弾メッセージの追跡
曳光弾メッセージがストリームやテーブルに正しく流れているかを確認するには、以下のようにクエリを実行します。

#### ksqlでの曳光弾メッセージのフィルタリング
```sql
SELECT *
FROM orders_stream
WHERE IsTracer = true
EMIT CHANGES;
```

このクエリは、曳光弾メッセージのみをフィルタリングしてリアルタイムで表示します。

### 曳光弾の利点
1. **非侵襲性**: 通常の業務データに影響を与えずにシステムの動作を確認できます。
2. **デバッグとモニタリング**: 曳光弾を使用することで、データフローの問題を迅速に特定できます。
3. **システム全体の可視性向上**: 曳光弾を追跡することで、システム全体のデータフローを把握できます。