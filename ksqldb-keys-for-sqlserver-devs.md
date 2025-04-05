# SQLServer技術者向け：KSQLDBのキー（KEY）の概念解説

KSQLDBのキー（KEY）の概念は、SQLServerの主キーに似ている部分もありますが、根本的に異なる点も多くあります。このドキュメントでは、SQLServerのキーの知識をベースにKSQLDBのキーについて解説します。
# 目次

1. [キーの基本概念：SQLServerとKSQLDBの比較](#キーの基本概念sqlserverとksqldbの比較)
   - [SQLServerのキー（おさらい）](#sqlserverのキーおさらい)
   - [KSQLDBのキー](#ksqldbのキー)
2. [KSQLDBのキーの特殊性](#ksqldbのキーの特殊性)
   - [Kafkaメッセージ構造とキー](#kafkaメッセージ構造とキー)
   - [KSQLDBのストリームとテーブルにおけるキー](#ksqldbのストリームとテーブルにおけるキー)
3. [キーの定義方法](#キーの定義方法)
   - [ストリーム作成時のキー指定](#ストリーム作成時のキー指定)
   - [テーブル作成時のキー指定](#テーブル作成時のキー指定)
4. [キーの重要な特性](#キーの重要な特性)
   - [パーティショニングとの関係](#パーティショニングとの関係)
   - [更新セマンティクス](#更新セマンティクス)
   - [NULL値とトゥームストーン](#null値とトゥームストーン)
5. [キーと結合操作](#キーと結合操作)
   - [ストリーム-ストリーム結合](#ストリーム-ストリーム結合)
   - [ストリーム-テーブル結合](#ストリーム-テーブル結合)
6. [複合キーの扱いと部分キー結合](#複合キーの扱いと部分キー結合)
   - [SQLServerの複合キーと部分キー結合](#sqlserverの複合キーと部分キー結合)
   - [KSQLDBの複合キーと結合の制約](#ksqldbの複合キーと結合の制約)
   - [部分キー結合に関する重要な考慮事項](#部分キー結合に関する重要な考慮事項)
7. [よくある課題と解決策](#よくある課題と解決策)
   - [キーを持たないデータの処理](#キーを持たないデータの処理)
   - [キーの変更](#キーの変更)
   - [キーと集約の関係](#キーと集約の関係)
8. [まとめ：SQLServerとKSQLDBのキーの比較表](#まとめsqlserverとksqldbのキーの比較表)
9. [キーに関するベストプラクティス](#キーに関するベストプラクティス)

## 1. キーの基本概念：SQLServerとKSQLDBの比較

### SQLServerのキー（おさらい）
- **主キー（PRIMARY KEY）**: テーブル内の行を一意に識別する
- **外部キー（FOREIGN KEY）**: 他のテーブルとの関係を定義する
- **クラスター化インデックス**: 物理的なデータ順序を決定する
- **非クラスター化インデックス**: 論理的な参照順序を提供する

### KSQLDBのキー
- **メッセージキー**: Kafkaのメッセージのキー部分を表す
- **パーティションキー**: データの分散方法を決定する
- **テーブルキー**: テーブルの結合や集約操作の基準となる
- **キー列**: ストリームやテーブルでキーとして指定された列

## 2. KSQLDBにおけるキーの特殊性

### 2.1 Kafkaメッセージ構造とキー

KSQLDBのキーを理解するには、まずKafkaのメッセージ構造を理解する必要があります：

```
Kafkaメッセージ = キー部分 + 値部分
```

- **キー部分**: メッセージを識別し、パーティション決定に使用
- **値部分**: 実際のデータペイロード

SQLServerでは、キーはレコードの属性の一つですが、Kafkaではメッセージ構造自体の一部として明確に分離されています。

### 2.2 KSQLDBのストリームとテーブルにおけるキー

#### ストリーム（STREAM）のキー
- ストリームではキーは必須ではない
- キーを持たないストリームも作成可能
- キーはメッセージのルーティングやジョイン操作で重要

#### テーブル（TABLE）のキー
- テーブルではキーが必須
- キーはレコードの一意性を保証する
- 同じキーを持つ新しいレコードは、既存のレコードを更新（上書き）する

## 3. キーの定義方法

### 3.1 ストリーム作成時のキー指定

```sql
-- SQLServer的な主キー定義との違い
CREATE STREAM orders (
  order_id VARCHAR KEY,  -- KEY として列を指定
  customer_id VARCHAR,
  amount DOUBLE
) WITH (
  KAFKA_TOPIC = 'orders',
  VALUE_FORMAT = 'JSON'
);

-- または WITH句でキー列を指定
CREATE STREAM orders (
  order_id VARCHAR,
  customer_id VARCHAR,
  amount DOUBLE
) WITH (
  KAFKA_TOPIC = 'orders',
  VALUE_FORMAT = 'JSON',
  KEY = 'order_id'  -- キー列を指定
);
```

### 3.2 テーブル作成時のキー指定

```sql
-- PRIMARY KEY による指定
CREATE TABLE customers (
  customer_id VARCHAR PRIMARY KEY,  -- PRIMARY KEY として指定
  name VARCHAR,
  email VARCHAR
) WITH (
  KAFKA_TOPIC = 'customers',
  VALUE_FORMAT = 'AVRO'
);

-- または WITH句でキー列を指定
CREATE TABLE customers (
  customer_id VARCHAR,
  name VARCHAR,
  email VARCHAR
) WITH (
  KAFKA_TOPIC = 'customers',
  VALUE_FORMAT = 'AVRO',
  KEY = 'customer_id'  -- キー列を指定
);
```

## 4. キーの重要な特性

### 4.1 パーティショニングとの関係

SQLServerでは、キーとパーティショニングは別の概念ですが、KSQLDBでは密接に関連しています：

- キーはKafkaトピックのパーティション割り当てを決定する
- 同じキーを持つメッセージは必ず同じパーティションに配置される
- これにより、キーベースの結合や集約が効率的に処理可能になる

```
キー「customer_123」→ ハッシュ関数 → パーティション2に割り当て
```

### 4.2 更新セマンティクス

KSQLDBテーブルでのキーの扱いは、SQLServerの主キーと似ていますが、更新の挙動が異なります：

- SQLServerでは `UPDATE` 文で明示的に更新
- KSQLDBでは同じキーの新しいメッセージが古いレコードを上書き
- テーブルは各キーの「最新状態」を表現

```sql
-- SQLServerの更新
UPDATE customers SET email = 'new@example.com' WHERE customer_id = 'cust123';

-- KSQLDBでの「更新」（実際には新しいメッセージの追加）
INSERT INTO customers (customer_id, name, email) VALUES ('cust123', 'John', 'new@example.com');
-- 同じキー 'cust123' に対する前の値は論理的に上書きされる
```

### 4.3 NULL値とトゥームストーン

KSQLDBでは、キーに対して値がNULLのメッセージは特別な意味を持ちます：

- NULL値を持つメッセージは「トゥームストーン」と呼ばれる
- テーブルではトゥームストーンはそのキーのレコードを論理的に削除する
- SQLServerの `DELETE` に相当する操作

```sql
-- SQLServerの削除
DELETE FROM customers WHERE customer_id = 'cust123';

-- KSQLDBでの「削除」（NULL値メッセージの挿入）
INSERT INTO customers (customer_id, name, email) VALUES ('cust123', NULL, NULL);
-- この操作により 'cust123' に関連するレコードはテーブルから「消える」
```

## 5. キーと結合操作

### 5.1 ストリーム-ストリーム結合

SQLServerのテーブル結合とは異なり、ストリーム結合では時間の概念が重要：

```sql
-- SQLServer結合（時間の概念なし）
SELECT o.order_id, c.customer_name
FROM Orders o
JOIN Customers c ON o.customer_id = c.customer_id;

-- KSQLDB ストリーム結合（時間ウィンドウあり）
SELECT o.order_id, c.customer_id, c.amount
FROM orders_stream o
JOIN shipments_stream s
  WITHIN 1 HOUR  -- 時間ウィンドウ指定
  ON o.order_id = s.order_id
EMIT CHANGES;
```

### 5.2 ストリーム-テーブル結合

ストリームとテーブルの結合は、トランザクションテーブルとマスターテーブルの結合に似ています：

```sql
-- SQLServerでの結合
SELECT o.order_id, c.customer_name, o.amount
FROM Orders o
JOIN Customers c ON o.customer_id = c.customer_id;

-- KSQLDBでのストリーム-テーブル結合
SELECT o.order_id, c.name, o.amount
FROM orders_stream o
JOIN customers_table c
  ON o.customer_id = c.customer_id
EMIT CHANGES;
```

ただし大きな違いは、KSQLDBでは：
- テーブルの「現在の状態」とストリームの各イベントが結合される
- 結合結果もストリームとして継続的に出力される

## 6. 複合キーの扱いと部分キー結合

### 6.1 SQLServerの複合キーと部分キー結合

SQLServerでは、複合キーを持つテーブル間の結合において、キーの一部のみを使用して結合することが一般的です：

```sql
-- SQLServerの複合主キー
CREATE TABLE OrderItems (
  order_id INT,
  item_id INT,
  quantity INT,
  PRIMARY KEY (order_id, item_id)
);

CREATE TABLE Orders (
  order_id INT PRIMARY KEY,
  customer_id INT,
  order_date DATE
);

-- 複合キーの一部（order_id）だけを使った結合
SELECT oi.order_id, oi.item_id, o.order_date
FROM OrderItems oi
JOIN Orders o ON oi.order_id = o.order_id;
```

### 6.2 KSQLDBの複合キーと結合の制約

KSQLDBでは複合キーを直接サポートしていないだけでなく、**結合は完全なメッセージキー同士でのみ可能**という重要な制約があります：

- SQLServerのような「キーの一部」だけを使用した結合は直接サポートされていない
- 結合は常に両方のストリーム/テーブルの「完全なキー」同士で行われる

**解決策1: 複合キーを文字列として連結**

```sql
-- 文字列連結で複合キーを作成
CREATE STREAM order_items (
  order_id VARCHAR,
  item_id VARCHAR,
  quantity INT
) WITH (
  KAFKA_TOPIC = 'order_items',
  VALUE_FORMAT = 'JSON',
  KEY_FORMAT = 'KAFKA'
);

-- 連結キーを使用したクエリ
CREATE STREAM order_items_with_key AS
  SELECT
    CONCAT(order_id, ':', item_id) AS order_item_key,
    order_id,
    item_id,
    quantity
  FROM order_items
  PARTITION BY CONCAT(order_id, ':', item_id);
```

**解決策2: 部分キー結合のための中間ストリーム/テーブルの作成**

SQLServerのように複合キーの一部で結合するには、キーを変更した中間ストリーム/テーブルを作成する必要があります：

```sql
-- 元のストリーム（複合キー）
CREATE STREAM order_items (
  order_id VARCHAR,
  item_id VARCHAR,
  quantity INT
) WITH (
  KAFKA_TOPIC = 'order_items',
  VALUE_FORMAT = 'JSON',
  KEY = 'order_id,item_id'  -- 複合キー（内部的には連結される）
);

-- order_idのみをキーとする中間ストリームを作成
CREATE STREAM order_items_by_order AS
  SELECT *
  FROM order_items
  PARTITION BY order_id;  -- order_idのみをキーにする

-- 単一キーのテーブル
CREATE TABLE orders (
  order_id VARCHAR PRIMARY KEY,
  customer_id VARCHAR,
  order_date VARCHAR
) WITH (
  KAFKA_TOPIC = 'orders',
  VALUE_FORMAT = 'JSON'
);

-- 中間ストリームとテーブルを結合
SELECT oi.order_id, oi.item_id, o.order_date
FROM order_items_by_order oi
JOIN orders o ON o.order_id = oi.order_id
EMIT CHANGES;
```

### 6.3 部分キー結合に関する重要な考慮事項

1. **パフォーマンスへの影響**:
   - 中間ストリーム/テーブルの作成は追加のリソースを消費する
   - 追加のKafkaトピックが作成される

2. **一貫性への影響**:
   - 中間ストリームは独自のライフサイクルを持つ
   - 中間ストリームの遅延が結合結果に影響する可能性がある

3. **設計上の推奨事項**:
   - 可能な限り、結合操作を考慮して最初からキー設計を行う
   - 頻繁に行われる結合のパターンを分析し、適切なキー戦略を選択する

## 7. よくある課題と解決策

### 7.1 キーを持たないデータの処理

元のトピックにキーがない場合：

```sql
-- キーを持たないストリームから、キーを持つストリームを作成
CREATE STREAM orders_keyed AS
  SELECT
    order_id,
    customer_id,
    amount
  FROM orders_raw
  PARTITION BY order_id;  -- この列がキーになる
```

### 7.2 キーの変更

KSQLDBではキーの変更に注意が必要です：

```sql
-- キーを変更した新しいストリームを作成
CREATE STREAM customers_by_region AS
  SELECT
    region as new_key,  -- 新しいキー
    customer_id,
    name,
    email
  FROM customers
  PARTITION BY region;  -- PARTITIONキーワードでキーを指定
```

### 7.3 キーと集約の関係

集約操作はキーに基づいて行われます：

```sql
-- SQLServerでの集約
SELECT customer_id, SUM(amount) as total_amount
FROM Orders
GROUP BY customer_id;

-- KSQLDBでの集約（継続的に更新される）
SELECT
  customer_id,
  SUM(amount) AS total_amount
FROM orders_stream
GROUP BY customer_id
EMIT CHANGES;
```

## 8. まとめ：SQLServerとKSQLDBのキーの比較表

| 概念 | SQLServer | KSQLDB |
|------|-----------|--------|
| 主キーの必須性 | テーブルによる | ストリームでは任意、テーブルでは必須 |
| キーの一意性 | 強制される | テーブルではキーごとに最新値のみ保持される |
| 更新メカニズム | UPDATE文 | 同じキーで新しいメッセージを送信 |
| 削除メカニズム | DELETE文 | キーを持ちNULL値のメッセージ（トゥームストーン） |
| 参照整合性 | 外部キー制約 | 明示的な制約なし、結合時に考慮が必要 |
| インデックス | プライマリ/セカンダリ | キーに基づくのみ |
| 複合キー | ネイティブサポート | 文字列連結などで実装 |
| ストレージへの影響 | 物理的な格納順序 | Kafkaパーティションへの配置 |

## 9. キーに関するベストプラクティス

1. **テーブル用途ではキーを常に指定する**：テーブルのセマンティクスに必要
2. **結合操作のためにキーを設計する**：結合するテーブル/ストリーム間で互換性のあるキーを使用
3. **パーティション分散を考慮する**：均等に分散されるキーを選択
4. **ビジネスロジックに適したキーを選択する**：単なる技術的な要件だけでなく、ユースケースに適したキーを選ぶ
5. **複合キーには一貫した区切り文字を使用する**：チーム内で標準化する

以上の理解を持って、SQLServerからKSQLDBへ移行する際のキー設計を行うと、より効果的なストリーム処理システムを構築できます。
