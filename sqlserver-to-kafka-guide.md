# SQLServer技術者のためのKafkaおよびKSQLDB概念ガイド

このドキュメントは、SQLServerのバックグラウンドを持つ技術者がKafkaとKSQLDBの概念を理解しやすくするためのガイドです。
# 目次

1. [概念マッピング](#概念マッピング)
   - [基本的なコンポーネント対応表](#基本的なコンポーネント対応表)
   - [詳細な対応関係と重要な違い](#詳細な対応関係と重要な違い)
2. [1. テーブル vs トピック](#1-テーブル-vs-トピック)
3. [2. ビュー vs ストリーム/テーブル](#2-ビュー-vs-ストリームテーブル)
4. [3. データモデルとスキーマ](#3-データモデルとスキーマ)
5. [4. トランザクションとデータ整合性](#4-トランザクションとデータ整合性)
6. [5. クエリモデル](#5-クエリモデル)
7. [6. 永続性と耐久性](#6-永続性と耐久性)
8. [7. スケーリングモデル](#7-スケーリングモデル)
9. [8. ユースケースの違い](#8-ユースケースの違い)
10. [トピックのライフサイクル管理](#トピックのライフサイクル管理)
11. [KSQLDB Tableとキー更新の一貫性](#ksqldb-tableとキー更新の一貫性)
12. [まとめ](#まとめ)


## 概念マッピング

### 基本的なコンポーネント対応表

| SQLServer | Kafka/KSQLDB | 適合度 |
|-----------|--------------|--------|
| データベース | Kafka クラスター | 部分的 |
| テーブル | トピック | 部分的 |
| ビュー | ストリーム | 部分的 |
| マテリアライズドビュー | テーブル | 部分的 |
| トランザクションログ | トピック自体 | 良好 |
| インデックス | 状態ストア | 部分的 |
| トリガー | ストリーム処理 | 部分的 |
| ストアドプロシージャ | KSQL UDF/UDAF | 部分的 |

### 詳細な対応関係と重要な違い

## 1. テーブル vs トピック

**SQLServer テーブル**:
- 行と列の構造でデータを格納
- CRUD操作（挿入、更新、削除）をサポート
- 更新は既存データを上書き
- プライマリキーや外部キー制約をサポート
- レコードの物理的な順序は通常保証されない

**Kafka トピック**:
- イベントログとしてメッセージを保存
- 追記専用（Append-only）
- 既存データは変更不可能
- 各メッセージはオフセット（位置）で識別
- メッセージは到着順に厳密に保存される
- メッセージはキーと値のペアで構成

```
-- SQLServer テーブル操作例
CREATE TABLE Customers (
    CustomerID INT PRIMARY KEY,
    Name NVARCHAR(100),
    Email NVARCHAR(100)
);

INSERT INTO Customers VALUES (1, 'John', 'john@example.com');
UPDATE Customers SET Email = 'john.doe@example.com' WHERE CustomerID = 1;
DELETE FROM Customers WHERE CustomerID = 1;
```

```
# Kafka トピック操作例（概念的な表現）
# トピック作成
kafka-topics.sh --create --topic customers --partitions 3 --replication-factor 3

# メッセージ送信（挿入）
kafka-console-producer.sh --topic customers
> {"CustomerID": 1, "Name": "John", "Email": "john@example.com"}

# 更新はただの新しいメッセージ
> {"CustomerID": 1, "Name": "John", "Email": "john.doe@example.com"}

# 削除は特殊なnullメッセージ（トゥームストーン）
> {"CustomerID": 1, "value": null}
```

## 2. ビュー vs ストリーム/テーブル

**SQLServer ビュー**:
- 1つ以上のテーブルから派生したデータの論理的なビュー
- 通常は読み取り専用
- クエリを保存して再利用する方法
- データは必要に応じて再計算される
- マテリアライズドビューは結果を物理的に保存

**KSQLDB ストリーム**:
- トピックデータの時系列ビュー
- 追記専用のイベントシーケンス
- データの「動き」や「変化」を表現
- すべてのメッセージが保持される
- ウィンドウ処理や時間ベース処理が可能

**KSQLDB テーブル**:
- トピックデータのキーベースの最新状態ビュー
- キーごとに最新の値だけを保持
- データの「現在の状態」を表現
- 更新の概念があるが、背後のトピックは追記のまま
- マテリアライズドビューに似ているが動的に更新される

```sql
-- SQLServer ビュー例
CREATE VIEW CustomerOrders AS
SELECT c.Name, COUNT(o.OrderID) AS OrderCount, SUM(o.Amount) AS TotalAmount
FROM Customers c
JOIN Orders o ON c.CustomerID = o.CustomerID
GROUP BY c.Name;

-- クエリ実行
SELECT * FROM CustomerOrders WHERE TotalAmount > 1000;
```

```sql
-- KSQLDB ストリーム例
CREATE STREAM orders_stream (
    OrderID STRING,
    CustomerID STRING,
    Amount DECIMAL(10,2),
    OrderTime TIMESTAMP
) WITH (
    KAFKA_TOPIC = 'orders',
    VALUE_FORMAT = 'JSON'
);

-- ストリームからのクエリ
SELECT OrderID, Amount FROM orders_stream WHERE Amount > 1000 EMIT CHANGES;

-- KSQLDB テーブル例
CREATE TABLE customer_orders AS
SELECT
    CustomerID,
    COUNT(*) AS OrderCount,
    SUM(Amount) AS TotalAmount
FROM orders_stream
GROUP BY CustomerID
EMIT CHANGES;

-- テーブルからのクエリ
SELECT * FROM customer_orders WHERE TotalAmount > 1000;
```

## 3. データモデルとスキーマ

**SQLServer**:
- スキーマは固定的で厳格に強制される
- テーブル作成時にスキーマを定義
- ALTER TABLE で変更可能だが操作は重い
- リレーショナルモデルに基づく
- 正規化が推奨される

**Kafka/KSQLDB**:
- スキーマは柔軟で進化可能
- スキーマレジストリで管理（オプション）
- Avro, JSON, Protobuf などの形式をサポート
- 同じトピックに異なるバージョンのスキーマが混在可能
- イベントモデルに基づく
- 非正規化が一般的

## 4. トランザクションとデータ整合性

**SQLServer**:
- ACID トランザクションをネイティブサポート
- BEGIN, COMMIT, ROLLBACK の明示的な制御
- 複数のテーブルにまたがるトランザクション
- ロック機構による並行性制御
- トランザクション分離レベルを設定可能

**Kafka**:
- トランザクショナルプロデューサーとコンシューマー（限定的）
- 複数パーティションへの原子的書き込み
- 厳密なACIDではなく、イベンチュアルコンシステンシーに基づく
- 「正確に一度」の処理保証
- ロールバックはなく、補償トランザクションが必要

## 5. クエリモデル

**SQLServer**:
- リクエスト/レスポンスモデル（プル型）
- ポイントインタイムクエリ
- クエリは実行時に一度だけ結果を返す
- 静的な結果セット

**KSQLDB**:
- プッシュクエリとプルクエリの両方をサポート
- プッシュクエリ: 継続的に結果を返し続ける
- プルクエリ: SQL-likeな一回限りのクエリ
- 時間の概念がクエリに組み込まれている
- イベントタイムとプロセシングタイムの区別

```sql
-- SQLServer クエリ例（ポイントインタイムクエリ）
SELECT * FROM Orders WHERE CustomerID = 'CUST001';
```

```sql
-- KSQLDB プルクエリ（現在の状態のみ）
SELECT * FROM customer_orders WHERE CustomerID = 'CUST001';

-- KSQLDB プッシュクエリ（継続的に変化を通知）
SELECT * FROM orders_stream WHERE CustomerID = 'CUST001' EMIT CHANGES;
```

## 6. 永続性と耐久性

**SQLServer**:
- データファイル (.mdf) とログファイル (.ldf)
- WAL（Write-Ahead Logging）によるリカバリ
- チェックポイントによる定期的な状態保存
- データベースバックアップを通じた復旧

**Kafka**:
- パーティション化されたログファイル
- レプリケーションによる冗長性
- 設定可能な保持期間
- コンパクションによるログ最適化
- コンシューマーグループによるオフセット管理

## 7. スケーリングモデル

**SQLServer**:
- 主に垂直スケーリング（より大きなサーバー）
- 読み取りスケール用のレプリカ
- シャーディングは複雑で手動設定が必要
- 一般的に単一リージョン設計

**Kafka**:
- 水平スケーリングが基本設計
- ブローカーの追加で容量拡大
- パーティションによる並列処理
- 複数データセンターレプリケーション
- コンシューマーグループによる消費の並列化

## 8. ユースケースの違い

**SQLServer 向き**:
- トランザクション処理（OLTP）
- 複雑なクエリと分析（OLAP）
- マスターデータ管理
- バッチ処理
- 複雑な結合と集計

**Kafka/KSQLDB 向き**:
- イベントソーシング
- リアルタイムデータパイプライン
- 非同期処理
- マイクロサービス間通信
- リアルタイムダッシュボードとモニタリング
- IoTデータ処理
- 変更データキャプチャ（CDC）

## トピックのライフサイクル管理

**SQLServer テーブル**:
1. CREATE TABLE で作成
2. INSERT/UPDATE/DELETE でデータ操作
3. ALTER TABLE でスキーマ変更
4. DROP TABLE で削除

**Kafka トピック**:
1. トピック作成
2. プロデューサーがメッセージ送信
3. コンシューマーがメッセージ購読
4. スキーマ進化（互換性に注意）
5. ログコンパクション/保持ポリシーによるクリーンアップ
6. トピック削除

## KSQLDB Tableとキー更新の一貫性

KSQLDB Tableのキー更新は特に注意が必要な点です：

1. テーブルは内部的にKafkaトピックのマテリアライズドビュー
2. キーごとに最新の値のみを保持する概念
3. トピックに送信されるnullメッセージ（トゥームストーン）がレコード削除として扱われる
4. テーブル再作成時は過去データの再処理が必要で一時的な不整合が発生しうる

**トゥームストーンの例**:
```
# オリジナルのメッセージ
Key: "customer_123", Value: {"name": "John", "age": 30}

# 更新メッセージ
Key: "customer_123", Value: {"name": "John", "age": 31}

# 削除メッセージ（トゥームストーン）
Key: "customer_123", Value: null
```

このデータがKafkaトピックに存在する場合、KSQLDB Tableでは"customer_123"キーのレコードは表示されません。

## まとめ

SQLServerとKafka/KSQLDBは根本的な設計思想が異なります：

- SQLServerは「現在の状態」を中心としたリレーショナルモデル
- Kafkaは「イベントの流れ」を中心としたストリーミングモデル

これらのシステムを適切に組み合わせることで、トランザクション処理の強みとリアルタイムストリーミングの柔軟性を活かしたアーキテクチャを構築できます。例えば、SQLServerをシステムのレコードとし、Kafkaを変更データのストリーミングと統合に使用するパターンが一般的です。

それぞれのシステムの長所を理解し、適材適所で使い分けることが重要です。
