# KSQLDBの代表的なクエリタイプ一覧

KSQLDBでは、データストリームやテーブルに対して様々な種類のクエリを実行できます。以下はKSQLDBの代表的なクエリタイプとその用途、構文例です。

## 1. プッシュクエリとプルクエリ

### 1.1 プッシュクエリ (Push Queries)

**概要**: 結果が継続的に更新される非終了型のクエリ。データが到着するたびに新しい結果が生成される。

**特徴**:
- `EMIT CHANGES` 句で識別される
- 無限に実行され続ける
- リアルタイムイベント処理に最適

**構文例**:
```sql
SELECT * FROM orders_stream 
WHERE total_price > 100 
EMIT CHANGES;
```

### 1.2 プルクエリ (Pull Queries)

**概要**: 現在の状態のスナップショットを返す一回限りのクエリ。伝統的なSQLクエリに類似。

**特徴**:
- テーブルに対してのみ実行可能
- 現在の状態のみを返す（履歴データにアクセスできない）
- `EMIT CHANGES` 句を使用しない

**構文例**:
```sql
SELECT * FROM customers_table 
WHERE customer_id = 'C123';
```

## 2. データ定義・操作クエリ

### 2.1 CREATE STREAM

**概要**: 新しいストリームを作成するクエリ。

**構文例**:
```sql
CREATE STREAM orders_stream (
  order_id STRING KEY,
  product_id STRING,
  quantity INT,
  price DECIMAL(10,2)
) WITH (
  KAFKA_TOPIC = 'orders',
  VALUE_FORMAT = 'AVRO',
  PARTITIONS = 6
);
```

### 2.2 CREATE TABLE

**概要**: 新しいテーブルを作成するクエリ。キーが必須。

**構文例**:
```sql
CREATE TABLE product_inventory (
  product_id STRING PRIMARY KEY,
  product_name STRING,
  quantity INT,
  price DECIMAL(10,2)
) WITH (
  KAFKA_TOPIC = 'inventory',
  VALUE_FORMAT = 'JSON',
  PARTITIONS = 6
);
```

### 2.3 CREATE STREAM AS SELECT (CSAS)

**概要**: 既存のストリームやテーブルからクエリの結果として新しいストリームを作成。

**特徴**:
- 新しいKafkaトピックも自動的に作成される
- 永続的なクエリとして実行され続ける

**構文例**:
```sql
CREATE STREAM high_value_orders AS
  SELECT * FROM orders_stream
  WHERE price * quantity > 1000
  EMIT CHANGES;
```

### 2.4 CREATE TABLE AS SELECT (CTAS)

**概要**: 既存のストリームやテーブルからクエリの結果として新しいテーブルを作成。

**特徴**:
- 新しいKafkaトピックも自動的に作成される
- 集計操作を含むことが多い
- GROUP BY 句が必要

**構文例**:
```sql
CREATE TABLE daily_product_sales AS
  SELECT product_id,
         SUM(quantity * price) AS total_sales
  FROM orders_stream
  WINDOW TUMBLING (SIZE 24 HOURS)
  GROUP BY product_id
  EMIT CHANGES;
```

### 2.5 INSERT INTO

**概要**: 既存のストリームやテーブルに新しいデータを挿入するクエリ。

**構文例**:
```sql
INSERT INTO high_value_orders
  SELECT * FROM orders_stream
  WHERE price * quantity > 2000;
```

### 2.6 DROP と TERMINATE

**概要**: ストリーム、テーブル、クエリを削除または終了するクエリ。

**構文例**:
```sql
-- ストリームの削除
DROP STREAM orders_stream;

-- 依存オブジェクトも含めて削除（カスケード）
DROP STREAM orders_stream CASCADE;

-- クエリの終了
TERMINATE QUERY 'CSAS_HIGH_VALUE_ORDERS_0';
```

## 3. 結合操作クエリ

### 3.1 Stream-Stream Join

**概要**: 2つのストリーム間の結合。時間ウィンドウ内のイベントを結合。

**特徴**:
- WITHIN句でウィンドウサイズを指定
- 内部結合、左結合、完全外部結合をサポート

**構文例**:
```sql
SELECT o.order_id, o.customer_id, p.payment_id, p.amount
FROM orders_stream o
JOIN payments_stream p
  WITHIN 1 HOURS
  ON o.order_id = p.order_id
EMIT CHANGES;
```

### 3.2 Stream-Table Join

**概要**: ストリームとテーブルの結合。テーブルの現在の状態とストリームのイベントを結合。

**特徴**:
- テーブルルックアップとして機能
- WITHIN句は不要

**構文例**:
```sql
SELECT o.order_id, c.customer_name, o.product_id
FROM orders_stream o
JOIN customers_table c
  ON o.customer_id = c.customer_id
EMIT CHANGES;
```

### 3.3 Table-Table Join

**概要**: 2つのテーブル間の結合。両方のテーブルの現在の状態を結合。

**構文例**:
```sql
SELECT c.customer_id, c.customer_name, s.total_spent
FROM customers_table c
JOIN customer_spending_table s
  ON c.customer_id = s.customer_id
EMIT CHANGES;
```

## 4. ウィンドウ処理クエリ

### 4.1 タンブリングウィンドウ (Tumbling Window)

**概要**: 固定サイズの重複しない時間枠でデータをグループ化。

**特徴**:
- 隣接するウィンドウ間に隙間はない
- 各イベントは1つのウィンドウのみに属する

**構文例**:
```sql
SELECT product_id, COUNT(*) AS purchase_count
FROM purchases_stream
WINDOW TUMBLING (SIZE 1 HOUR)
GROUP BY product_id
EMIT CHANGES;
```

### 4.2 ホッピングウィンドウ (Hopping Window)

**概要**: 固定サイズの重複可能な時間枠でデータをグループ化。

**特徴**:
- ウィンドウが前進する間隔を指定可能
- 同じイベントが複数のウィンドウに属することがある

**構文例**:
```sql
SELECT product_id, COUNT(*) AS purchase_count
FROM purchases_stream
WINDOW HOPPING (SIZE 1 HOUR, ADVANCE BY 10 MINUTES)
GROUP BY product_id
EMIT CHANGES;
```

### 4.3 セッションウィンドウ (Session Window)

**概要**: 非アクティブ期間によって区切られるイベントのグループ。

**特徴**:
- アクティビティがあればウィンドウが拡張される
- 指定した非アクティブ間隔を超えると新しいセッションが開始

**構文例**:
```sql
SELECT user_id, COUNT(*) AS click_count
FROM user_clicks_stream
WINDOW SESSION (30 MINUTES)
GROUP BY user_id
EMIT CHANGES;
```

## 5. 集計クエリ

### 5.1 単純集計

**概要**: COUNT, SUM, AVG, MIN, MAXなどの集計関数を使用。

**構文例**:
```sql
SELECT product_id,
       COUNT(*) AS order_count,
       SUM(quantity) AS total_quantity,
       AVG(price) AS average_price
FROM orders_stream
GROUP BY product_id
EMIT CHANGES;
```

### 5.2 HAVING句による集計フィルタリング

**概要**: 集計結果に条件を適用するクエリ。

**構文例**:
```sql
SELECT product_id, COUNT(*) AS order_count
FROM orders_stream
GROUP BY product_id
HAVING COUNT(*) > 5
EMIT CHANGES;
```

### 5.3 時間ベースの集計

**概要**: 時間ウィンドウとともに集計を行うクエリ。

**構文例**:
```sql
SELECT product_id,
       TIMESTAMPTOSTRING(WINDOWSTART, 'yyyy-MM-dd HH:mm:ss') AS window_start,
       TIMESTAMPTOSTRING(WINDOWEND, 'yyyy-MM-dd HH:mm:ss') AS window_end,
       COUNT(*) AS order_count
FROM orders_stream
WINDOW TUMBLING (SIZE 1 HOUR)
GROUP BY product_id
EMIT CHANGES;
```

## 6. 特殊クエリ

### 6.1 EXPLAIN

**概要**: クエリの実行計画を表示するメタクエリ。

**構文例**:
```sql
EXPLAIN SELECT * FROM orders_stream WHERE order_id = 'ORD-123';
```

### 6.2 DESCRIBE

**概要**: ストリームやテーブルの構造を表示するメタクエリ。

**構文例**:
```sql
DESCRIBE orders_stream;
DESCRIBE EXTENDED customers_table;
```

### 6.3 SHOW QUERIES

**概要**: 実行中のクエリを表示するメタクエリ。

**構文例**:
```sql
SHOW QUERIES;
SHOW QUERIES EXTENDED;
```

### 6.4 PRINT

**概要**: トピックの内容を表示するメタクエリ。

**構文例**:
```sql
PRINT 'orders' FROM BEGINNING LIMIT 10;
```

## 7. 時系列と時間処理クエリ

### 7.1 タイムスタンプ抽出と操作

**概要**: タイムスタンプを処理するクエリ。

**構文例**:
```sql
SELECT order_id,
       TIMESTAMPTOSTRING(order_time, 'yyyy-MM-dd HH:mm:ss') AS formatted_time,
       EXTRACTHOUR(order_time) AS hour_of_day
FROM orders_stream
EMIT CHANGES;
```

### 7.2 遅延データ処理 (Watermarking)

**概要**: 遅延して到着するデータを適切に処理するためのクエリ。

**構文例**:
```sql
CREATE STREAM orders_with_watermark AS
  SELECT *
  FROM orders_stream
  TIMESTAMP order_time
  EMIT CHANGES;
```

## まとめ

KSQLDBには上記以外にも多くの機能と構文があります。これらのクエリタイプを組み合わせることで、リアルタイムデータ処理パイプラインを構築できます。用途に応じて適切なクエリタイプを選択することが重要です。
