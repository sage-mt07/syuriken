# Kafka Topic, Stream, Table の関係と依存関係（Avro前提）

このドキュメントでは、Kafka エコシステムにおける Topic、Stream、Table の関係と依存関係について、Avro スキーマを使用する前提で解説します。

## 1. 基本コンポーネントの概要

### 1.1 Kafka Topic

**定義**: Kafka の基本的なデータストレージ単位で、メッセージの時系列ログを保持します。

**特徴**:
- 追記専用のログ構造
- パーティション分割されたイベントシーケンス
- キーと値のペアとしてメッセージを格納
- 不変性（一度書き込まれたメッセージは変更不可）
- 設定可能な保持期間

### 1.2 KSQLDB Stream

**定義**: Kafka トピックの上に構築される読み取り専用のビュー。イベントの時系列としてデータを処理します。

**特徴**:
- トピックのデータをイベントシーケンスとして表現
- 基本的に追記専用の性質を継承
- SQL ライクなクエリ言語でアクセス可能
- 変換、フィルタリング、結合などの操作をサポート
- 新しいストリームまたはテーブルを作成するための基盤

### 1.3 KSQLDB Table

**定義**: Kafka トピックのデータを、キーごとの最新状態として抽象化したビュー。

**特徴**:
- キーごとに最新の値だけを論理的に保持
- 更新の概念（同じキーのメッセージが来ると前の値を「上書き」）
- 内部的には状態ストアを使用して最新値へのインデックスを維持
- null 値のメッセージ（トゥームストーン）がレコード削除として扱われる

## 2. Avro スキーマを使用した場合の特性

### 2.1 Avro とは

**定義**: Avro はデータシリアライゼーションフレームワークで、スキーマを使用してデータを構造化します。

**特徴**:
- コンパクトなバイナリフォーマット
- スキーマとデータの分離
- スキーマ進化をサポート
- Kafka との統合に適した設計

### 2.2 スキーマレジストリの役割

**定義**: スキーマを一元管理するリポジトリで、スキーマの互換性を確保します。

**機能**:
- スキーマのバージョン管理
- スキーマ ID によるスキーマ参照
- 互換性チェック（前方/後方/完全互換性）
- スキーマの自動登録

## 3. 依存関係の詳細

### 3.1 物理的な依存関係

```
Kafka Topic ← KSQLDB Stream
Kafka Topic ← KSQLDB Table
KSQLDB Stream ← 派生 Stream/Table（CSAS/CTAS）
```

**説明**:
1. **Topic → Stream/Table**: すべてのストリームとテーブルは1つ以上のトピックに基づいています
2. **Stream/Table → 派生 Stream/Table**: `CREATE STREAM AS SELECT` (CSAS) や `CREATE TABLE AS SELECT` (CTAS) で作成された派生オブジェクト

### 3.2 スキーマの依存関係（Avro使用時）

```
Subject (Topic-value) → Schema ID → Avro Schema
Topic → Subject → Schema
Stream/Table → Topic → Schema
```

**説明**:
1. **トピックとサブジェクト**: トピック名がスキーマレジストリのサブジェクト名の基盤（通常 `<topic-name>-value`）
2. **サブジェクトとスキーマ**: サブジェクトはスキーマのバージョン履歴を保持
3. **ストリーム/テーブルとスキーマ**: ストリーム/テーブルは作成時のスキーマに依存

### 3.3 操作とライフサイクルの依存関係

#### 3.3.1 作成順序

正しい作成順序:
1. Topic の作成（または存在確認）
2. Avro スキーマの登録（スキーマレジストリに）
3. Stream/Table の作成（トピックを参照）
4. 派生 Stream/Table の作成（既存の Stream/Table に基づく）

#### 3.3.2 削除順序と CASCADE オプション

正しい削除順序:
1. 派生 Stream/Table の削除
2. 基本 Stream/Table の削除
3. Topic の削除（任意）
4. スキーマ（削除は通常不要・不可）

#### CASCADE オプションによる依存関係の自動削除

KSQLDB 0.14.0 以降では、`CASCADE` オプションを使用して依存するオブジェクトを連鎖的に削除できます:

```sql
-- CASCADEなしでの削除（依存関係があるとエラー）
DROP STREAM users_stream;

-- CASCADEありでの削除（依存するすべてのオブジェクトも削除）
DROP STREAM users_stream CASCADE;
```

**CASCADE の挙動**:
1. 指定したストリーム/テーブルに依存するすべてのKSQLDBオブジェクト（派生ストリーム、テーブル、永続クエリ）を連鎖的に削除
2. 基盤となるKafkaトピックは削除されない（データは保持される）
3. 複雑な依存関係グラフをたどって、すべての依存オブジェクトを削除

**注意事項**:
1. CASCADEは強力なオプションで、意図せず重要なストリーム処理パイプラインを削除する可能性がある
2. 本番環境での使用前に依存関係のマッピングを十分に理解すること
3. トピックは削除されないため、データ自体は失われないが、処理パイプライン全体が削除される

## 4. スキーマ進化と互換性

### 4.1 Avro スキーマ進化の基本ルール

- **後方互換性**: 新しいスキーマで古いデータを読める
- **前方互換性**: 古いスキーマで新しいデータを読める
- **完全互換性**: 前方・後方の両方の互換性を持つ

### 4.2 互換性のある変更の例

- フィールドの追加（デフォルト値あり）
- フィールドの削除（使用しなくなったフィールド）
- フィールド名の変更（エイリアスを使用）
- 型の拡張（例: int → long）

### 4.3 互換性のない変更の例

- 既存フィールドの型の変更（互換性のない型への変更）
- 必須フィールドの追加（デフォルト値なし）
- フィールド制約の追加（検証が厳しくなる）

## 5. Topic, Stream, Table のスキーマ管理

### 5.1 Topic のスキーマ管理

```
# トピック作成
bin/kafka-topics.sh --create --topic users --partitions 6 --replication-factor 3

# スキーマの登録（概念的な表現）
curl -X POST -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  --data '{"schema": "{\"type\":\"record\",\"name\":\"User\",\"fields\":[...]}"}' \
  http://schema-registry:8081/subjects/users-value/versions
```

トピックにメッセージを送信する際:
1. データを Avro でシリアライズ（スキーマ ID を含む）
2. シリアライズされたデータをトピックに送信

### 5.2 Stream の作成とスキーマの関係

```sql
-- Avro フォーマットを指定してストリームを作成
CREATE STREAM users_stream (
  user_id STRING, 
  name STRING, 
  email STRING
) WITH (
  KAFKA_TOPIC = 'users',
  VALUE_FORMAT = 'AVRO'
);
```

この操作の背後では:
1. スキーマレジストリからトピックのスキーマ情報を取得
2. テーブル定義と比較して互換性を確認
3. ストリームのメタデータを KSQLDB に保存

### 5.3 Table の作成とスキーマの関係

```sql
-- Avro フォーマットを指定してテーブルを作成
CREATE TABLE users_table (
  user_id STRING PRIMARY KEY, 
  name STRING, 
  email STRING
) WITH (
  KAFKA_TOPIC = 'users',
  VALUE_FORMAT = 'AVRO'
);
```

この操作の背後では:
1. スキーマレジストリからトピックのスキーマ情報を取得
2. テーブル定義と比較して互換性を確認
3. キー情報を検証（テーブルには PRIMARY KEY が必要）
4. テーブルのメタデータを KSQLDB に保存

## 6. スキーマ変更時の影響と対応

### 6.1 トピックスキーマ変更時の影響

1. **既存データへの影響**:
   - 古いデータは古いスキーマでシリアライズされたまま
   - 新しいデータは新しいスキーマでシリアライズされる
   - トピック内には複数バージョンのスキーマでシリアライズされたデータが混在

2. **ストリーム/テーブルへの影響**:
   - 既存のストリーム/テーブルは作成時のスキーマ定義を保持
   - 新しいフィールドは自動的に認識されない
   - 変更されたフィールドは正しく解釈されない可能性がある

### 6.2 スキーマ変更時の対応

#### 6.2.1 互換性のある変更の場合

1. スキーマレジストリに新バージョンのスキーマを登録
2. プロデューサーを更新して新スキーマでデータを送信
3. 必要に応じてストリーム/テーブルを再作成

```sql
-- 既存のストリームを削除
DROP STREAM users_stream;

-- 新しいスキーマに対応したストリームを再作成
CREATE STREAM users_stream WITH (
  KAFKA_TOPIC = 'users',
  VALUE_FORMAT = 'AVRO'
);
```

#### 6.2.2 互換性のない変更の場合

1. 新しいトピックを作成
2. 新スキーマを新トピックに関連付けて登録
3. 新しいストリーム/テーブルを新トピックに基づいて作成
4. データ移行または並行処理の戦略を実装

```sql
-- 新トピックに基づく新ストリームの作成
CREATE STREAM users_stream_v2 WITH (
  KAFKA_TOPIC = 'users_v2',
  VALUE_FORMAT = 'AVRO'
);

-- データ移行クエリの例
CREATE STREAM users_migration WITH (
  KAFKA_TOPIC = 'users_v2',
  VALUE_FORMAT = 'AVRO'
) AS 
SELECT 
  user_id,
  name,
  email,
  'unknown' AS new_required_field
FROM users_stream;
```

## 7. 依存関係を管理するためのベストプラクティス

### 7.1 スキーマ設計のベストプラクティス

1. **前方互換性を優先**: 消費者側を先に更新せずに済むように
2. **後方互換性も確保**: 古いデータの読み取りを保証
3. **デフォルト値の活用**: 新フィールド追加時は必ずデフォルト値を設定
4. **NULL許容の検討**: 厳格な制約は避け、NULL許容フィールドを活用
5. **Union型の活用**: 型の柔軟な進化のためにユニオン型を使用

### 7.2 トピック、ストリーム、テーブルの命名規則

```
# 基本トピック
<entity>

# バージョニングが必要な場合
<entity>_v<version>

# ストリーム
<entity>_stream

# テーブル
<entity>_table

# 派生ストリーム/テーブル
<entity>_<transformation>_stream
<entity>_<transformation>_table
```

例:
- `users` - 基本トピック
- `users_v2` - バージョン2トピック
- `users_stream` - ユーザーストリーム
- `users_table` - ユーザーテーブル
- `users_enriched_stream` - 拡充されたユーザーストリーム
- `users_aggregated_table` - 集計されたユーザーテーブル

### 7.3 依存関係の文書化

1. トピック、スキーマ、ストリーム、テーブルの関係を図で表現
2. 依存関係マトリックスの作成と保守
3. スキーマ変更の影響範囲分析と文書化
4. 変更管理プロセスの確立

### 7.4 スキーマ変更のデプロイ戦略

#### 7.4.1 段階的デプロイ

1. 新スキーマを登録し互換性を確認
2. 消費者アプリケーションを更新
3. プロデューサーアプリケーションを更新
4. ストリーム/テーブル定義を更新

#### 7.4.2 ブルー/グリーンデプロイメント

1. 新バージョンのトピック、ストリーム、テーブルを作成
2. データ移行または並行処理を実装
3. 検証後にコンシューマーを新バージョンに切り替え
4. 古いバージョンを廃止

## 8. 具体的なユースケース例

### 8.1 ユーザープロファイル管理システム

**初期設計**:

1. **トピックとスキーマ**:
   ```
   Topic: users
   Schema: 
     - user_id (string, key)
     - name (string)
     - email (string)
     - created_at (timestamp)
   ```

2. **ストリームとテーブル**:
   ```sql
   CREATE STREAM users_stream (
     user_id STRING,
     name STRING,
     email STRING,
     created_at TIMESTAMP
   ) WITH (
     KAFKA_TOPIC = 'users',
     VALUE_FORMAT = 'AVRO',
     KEY = 'user_id'
   );

   CREATE TABLE users_table (
     user_id STRING PRIMARY KEY,
     name STRING,
     email STRING,
     created_at TIMESTAMP
   ) WITH (
     KAFKA_TOPIC = 'users',
     VALUE_FORMAT = 'AVRO'
   );
   ```

**スキーマ進化（互換性あり）**:

1. **スキーマの更新**:
   ```
   新フィールド: 
     - address (string, nullable, default: null)
     - phone (string, nullable, default: null)
   ```

2. **ストリーム/テーブルの更新**:
   ```sql
   -- 既存のストリーム/テーブルを削除
   DROP STREAM users_stream;
   DROP TABLE users_table;

   -- 更新されたスキーマで再作成
   CREATE STREAM users_stream WITH (
     KAFKA_TOPIC = 'users',
     VALUE_FORMAT = 'AVRO',
     KEY = 'user_id'
   );

   CREATE TABLE users_table WITH (
     KAFKA_TOPIC = 'users',
     VALUE_FORMAT = 'AVRO'
   );
   ```

**派生ストリームとテーブル**:

```sql
-- アクティブユーザーの派生ストリーム
CREATE STREAM active_users_stream AS
SELECT user_id, name, email
FROM users_stream
WHERE email IS NOT NULL;

-- ユーザー統計の派生テーブル
CREATE TABLE user_stats_table AS
SELECT 
  SUBSTRING(email, INSTR(email, '@') + 1) AS domain,
  COUNT(*) AS user_count
FROM users_stream
GROUP BY SUBSTRING(email, INSTR(email, '@') + 1);
```

### 8.2 依存関係図の具体例

```
[users topic] <- users-value schema
       |
       |-------> [users_stream] -------> [active_users_stream]
       |              |
       |              |
       |-------> [users_table]
                      |
                      v
              [user_stats_table]
```

この依存関係図からわかる情報:
1. `users` トピックが基盤
2. `users_stream` と `users_table` は直接トピックに依存
3. `active_users_stream` は `users_stream` に依存
4. `user_stats_table` は `users_table` に依存

## 9. CASCADE と依存関係の詳細

### 9.1 CASCADE オプションの詳細な動作

CASCADE オプションは KSQLDB で依存関係のあるオブジェクトを管理するための重要な機能です。ここではその詳細な動作と実践的な使用方法を説明します。

#### CASCADE による削除の範囲

CASCADE を使用した場合に削除されるもの:
- 指定したストリーム/テーブルから派生した他のストリーム/テーブル
- 関連する永続クエリ (CSAS/CTAS で作成されたクエリ)
- 上記のオブジェクトからさらに派生したオブジェクト（無制限の深さ）

CASCADE を使用しても削除されないもの:
- 基盤となるKafkaトピック
- トピックに関連付けられたAvroスキーマ
- 外部システムとの連携（コネクタなど）

#### CASCADE の使用例

**単純な依存関係の場合**:
```sql
-- 基本ストリームの作成
CREATE STREAM users_stream (id STRING, name STRING) 
WITH (KAFKA_TOPIC='users', VALUE_FORMAT='AVRO');

-- 派生ストリームの作成
CREATE STREAM active_users_stream 
AS SELECT * FROM users_stream WHERE name IS NOT NULL;

-- CASCADEを使用して削除すると、active_users_streamも削除される
DROP STREAM users_stream CASCADE;
```

**複雑な依存関係の場合**:
```sql
-- 階層的な依存関係の例
CREATE STREAM users_stream (...) WITH (KAFKA_TOPIC='users', ...);
CREATE STREAM filtered_users AS SELECT * FROM users_stream WHERE ...;
CREATE TABLE user_counts AS SELECT id, COUNT(*) FROM filtered_users GROUP BY id;
CREATE TABLE high_value_users AS SELECT * FROM user_counts WHERE count > 10;

-- users_streamを削除すると、すべての派生オブジェクトが削除される
DROP STREAM users_stream CASCADE;
```

#### CASCADE を使用する際の推奨プラクティス

1. **削除前に依存関係を確認**:
   ```sql
   -- ストリームに依存するオブジェクトを確認
   SHOW STREAMS EXTENDED;
   SHOW QUERIES EXTENDED;
   ```

2. **段階的な削除の検討**:
   複雑なシステムでは、CASCADE の代わりに計画的に段階的に削除することを検討

3. **バックアップとして CREATE ステートメントを保存**:
   ```sql
   -- 削除前に再作成用の CREATE ステートメントを取得
   SHOW CREATE STREAM stream_name;
   SHOW CREATE TABLE table_name;
   ```

4. **本番環境での注意**:
   本番環境では CASCADE の使用を慎重に行い、影響範囲を事前に理解しておく

### 9.2 トラブルシューティング

#### スキーマの互換性エラー

**症状**: スキーマ登録エラー
```
Schema being registered is incompatible with an earlier schema
```

**原因**: 互換性のないスキーマ変更  
**解決策**: 
1. 互換性のある変更に修正
2. 互換性モードの変更（一時的に NONE に設定）
3. 新トピックでの再設計

#### ストリーム/テーブル作成エラー

**症状**: KSQLDB のストリーム/テーブル作成エラー
```
Could not fetch the AVRO schema from schema registry
```

**原因**: 
1. スキーマレジストリとの接続問題
2. トピックが存在しない
3. トピックにデータがなく自動スキーマ登録が行われていない

**解決策**:
1. スキーマレジストリの接続確認
2. トピックの存在確認
3. サンプルデータの送信

#### データシリアライゼーションエラー

**症状**: デシリアライズエラー
```
Error deserializing Avro message for id X
```

**原因**:
1. スキーマとデータの不一致
2. スキーマレジストリの問題
3. クライアントライブラリの互換性問題

**解決策**:
1. スキーマとデータの整合性確認
2. クライアントライブラリのアップデート
3. デバッグを有効にしてデータ構造を検証

## 10. まとめ

Kafka のトピック、KSQLDB のストリーム、テーブルの関係と依存関係について理解することは、堅牢なストリーム処理アプリケーションを構築する上で重要です。Avro スキーマを活用することで、データの一貫性と進化可能性を確保しながら、これらのコンポーネントを適切に管理できます。

主なポイント:
1. **階層的依存関係**: Topic → Stream/Table → 派生 Stream/Table
2. **スキーマ管理**: スキーマレジストリで一元管理し、互換性を確保
3. **ライフサイクル管理**: 依存関係を考慮した作成・削除順序
4. **CASCADE機能**: 依存関係のある複数オブジェクトを適切に管理する手段
5. **スキーマ進化**: 互換性を保ちながら進化させる戦略
6. **文書化と管理**: 依存関係の明示的な記録と変更管理プロセスの確立

CASCADEオプションは、複雑な依存関係を持つシステムを効率的に管理するための強力なツールです。ただし、その影響範囲を十分に理解した上で使用することが重要です。

これらの原則と機能を適切に活用することで、拡張性と保守性に優れたイベントストリーミングアーキテクチャを実現できます。
