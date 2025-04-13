# ksqlDBの使用手順と動作確認

このドキュメントでは、ksqlDBを使用するための手順と動作確認方法を説明します。

## ksqlDBのセットアップ

### 1. ksqlDBのダウンロードとインストール
1. [Confluentの公式サイト](https://www.confluent.io/download/)からksqlDBを含むConfluent Platformをダウンロードします。
2. ダウンロードしたファイルを解凍し、適切なディレクトリに配置します（例: `C:\confluent`）。

### 2. ksqlDBサーバーの起動
1. 解凍したディレクトリに移動します。
   ```cmd
   cd C:\confluent
   ```
2. ksqlDBサーバーを起動します。
   ```cmd
   .\bin\ksql-server-start .\etc\ksqldb\ksql-server.properties
   ```

### 3. ksqlDB CLIの起動
1. 別のコマンドプロンプトを開き、ksqlDB CLIを起動します。
   ```cmd
   .\bin\ksql .\etc\ksqldb\ksql-cli.properties
   ```

## ksqlDBの使用方法

### 1. ストリームの作成
Kafkaトピックからストリームを作成します。
```sql
CREATE STREAM orders_stream (
    orderId STRING,
    customerId STRING,
    amount DOUBLE,
    orderTime BIGINT
) WITH (
    KAFKA_TOPIC='orders',
    VALUE_FORMAT='JSON'
);
```

### 2. テーブルの作成
ストリームから集約テーブルを作成します。
```sql
CREATE TABLE customer_totals AS
SELECT customerId,
       SUM(amount) AS totalAmount,
       COUNT(*) AS orderCount
FROM orders_stream
GROUP BY customerId
EMIT CHANGES;
```

### 3. クエリの実行
作成したストリームやテーブルに対してクエリを実行します。
```sql
SELECT * FROM customer_totals EMIT CHANGES;
```

## 動作確認方法

### 1. Kafkaトピックへのデータ送信
Kafkaプロデューサーを使用して、`orders`トピックにデータを送信します。
```cmd
.\bin\windows\kafka-console-producer.bat --topic orders --bootstrap-server localhost:9092
```
以下のようなJSONデータを入力します。
```json
{"orderId": "1", "customerId": "CUST001", "amount": 100.0, "orderTime": 1680000000000}
{"orderId": "2", "customerId": "CUST002", "amount": 200.0, "orderTime": 1680000001000}
```

### 2. ksqlDBでのデータ確認
ksqlDB CLIで以下のクエリを実行し、データが正しく処理されていることを確認します。
```sql
SELECT * FROM orders_stream EMIT CHANGES;
```

### 3. 集約結果の確認
テーブル`customer_totals`に対してクエリを実行し、集約結果を確認します。
```sql
SELECT * FROM customer_totals EMIT CHANGES;
```

以上で、ksqlDBの使用手順と動作確認方法の説明は完了です。