# C#とKafka間のAvroデータ型マッピングと精度

このドキュメントでは、AvroシリアライゼーションフォーマットをベースにしたC#とKafka間のデータ型マッピングと、各データ型の精度に関する情報を提供します。

## 1. 基本的なデータ型マッピング

### 1.1 プリミティブ型

| Avro型 | C#型 | Kafka表現 | 精度/範囲 |
|-------|------|----------|----------|
| `null` | `null` | null | - |
| `boolean` | `bool` | Boolean | true/false |
| `int` | `int` | Int32 | -2,147,483,648 から 2,147,483,647 |
| `long` | `long` | Int64 | -9,223,372,036,854,775,808 から 9,223,372,036,854,775,807 |
| `float` | `float` | Single/Float | 約 ±1.5 × 10^−45 から ±3.4 × 10^38、約6-7桁の精度 |
| `double` | `double` | Double | 約 ±5.0 × 10^−324 から ±1.7 × 10^308、約15-16桁の精度 |
| `bytes` | `byte[]` | Binary data | バイナリデータ |
| `string` | `string` | UTF-8文字列 | UTF-8エンコード文字列 |

### 1.2 論理型（Logical Types）

| Avro論理型 | Avroベース型 | C#型 | 精度/範囲 |
|-----------|------------|------|----------|
| `decimal` | `bytes` | `decimal` | 28-29桁の有効数字、指定した精度とスケール |
| `uuid` | `string` | `Guid` | 16バイト (128ビット) 識別子 |
| `date` | `int` | `DateTime` | 1970-01-01からの日数、日付部分のみ |
| `time-millis` | `int` | `TimeSpan` | ミリ秒精度の時刻 (日付なし) |
| `time-micros` | `long` | `TimeSpan` | マイクロ秒精度の時刻 (日付なし) |
| `timestamp-millis` | `long` | `DateTime`/`DateTimeOffset` | エポックからのミリ秒、ミリ秒精度のタイムスタンプ |
| `timestamp-micros` | `long` | `DateTime`/`DateTimeOffset` | エポックからのマイクロ秒、マイクロ秒精度のタイムスタンプ |
| `duration` | `fixed` | `TimeSpan` | 月、日、ミリ秒の固定表現 |

### 1.3 複合型

| Avro型 | C#型 | 備考 |
|-------|------|------|
| `record` | クラス・構造体 | Avroレコードはクラスまたは構造体にマッピング |
| `enum` | `enum` | 列挙型 |
| `array` | `List<T>`, `T[]` | 配列や`List<T>`にマッピング |
| `map` | `Dictionary<string, T>` | キーは常に文字列、値は任意のAvro型 |
| `union` | Union型/Nullable型 | `[null, type]`はC#では`Nullable<T>`または参照型 |
| `fixed` | `byte[]` | 固定長バイト配列 |

## 2. データ型の精度と考慮事項

### 2.1 数値型の精度

#### 2.1.1 整数型

```csharp
// C#での整数型の表現
int avroInt = int.MaxValue;     // 2,147,483,647
long avroLong = long.MaxValue;  // 9,223,372,036,854,775,807
```

注意点：
- `int`から`long`への変換は安全
- `long`から`int`への変換は精度の損失が発生する可能性あり

#### 2.1.2 浮動小数点型

```csharp
// C#での浮動小数点の表現
float avroFloat = 123.456789f;  // 約123.4568（精度の損失あり）
double avroDouble = 123.4567890123456789;  // 約123.456789012346（精度の損失あり）
```

注意点：
- 浮動小数点型は概算値
- 金融計算には`decimal`を使用すべき
- `float`から`double`への変換は安全
- `double`から`float`への変換は精度の損失が発生する

#### 2.1.3 Decimal型

Avroの`decimal`論理型は`precision`と`scale`パラメータで制御：

```csharp
// C#でのdecimal表現
// precision=10, scale=2の場合
decimal avroDecimal = 12345678.90m;  // 有効桁数10桁、小数点以下2桁
```

Avroスキーマでの表現：
```json
{
  "type": "bytes",
  "logicalType": "decimal",
  "precision": 10,
  "scale": 2
}
```

注意点：
- C#の`decimal`は最大28-29桁の精度
- Avro定義の`precision`と`scale`に注意
- スケールを超える桁は切り捨て

### 2.2 時間関連型の精度

#### 2.2.1 日付型

```csharp
// C#での日付表現
DateTime avroDate = new DateTime(2023, 5, 15);  // 日付部分のみ
```

Avroスキーマでの表現：
```json
{
  "type": "int",
  "logicalType": "date"
}
```

注意点：
- 時刻情報は保持されない
- エポック（1970-01-01）からの日数として保存

#### 2.2.2 タイムスタンプ型

```csharp
// C#でのタイムスタンプ表現
// timestamp-millis
DateTime avroTimestampMillis = new DateTime(2023, 5, 15, 14, 30, 15, 500);

// timestamp-micros - C#ではマイクロ秒精度が直接サポートされないため注意が必要
DateTime avroTimestampMicros = new DateTime(2023, 5, 15, 14, 30, 15, 500);
// マイクロ秒部分は別途処理が必要
```

Avroスキーマでの表現：
```json
{
  "type": "long",
  "logicalType": "timestamp-millis"
}

{
  "type": "long",
  "logicalType": "timestamp-micros"
}
```

注意点：
- `timestamp-millis`はミリ秒精度（C#の`DateTime`と一致）
- `timestamp-micros`はマイクロ秒精度（C#の`DateTime`はTicksによる変換が必要）
- タイムゾーン情報は保持されないため、`DateTimeOffset`からの変換時は情報が失われる

#### 2.2.3 タイムゾーン対応

```csharp
// C#でのタイムゾーン対応日時
DateTimeOffset avroTimestampWithTz = new DateTimeOffset(2023, 5, 15, 14, 30, 15, 500, TimeSpan.FromHours(9));
```

注意点：
- Avroの標準仕様にはタイムゾーンを保持する型がない
- 必要に応じてカスタム拡張やタイムゾーンIDを別フィールドで保存する

## 3. 複合型とカスタム型の変換

### 3.1 レコード（クラス/構造体）

```csharp
// C#クラス
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

対応するAvroスキーマ：
```json
{
  "type": "record",
  "name": "User",
  "namespace": "MyApp.Models",
  "fields": [
    {"name": "Id", "type": "string"},
    {"name": "Name", "type": "string"},
    {"name": "Age", "type": "int"}
  ]
}
```

### 3.2 列挙型

```csharp
// C#列挙型
public enum UserRole
{
    Admin,
    Manager,
    User
}
```

対応するAvroスキーマ：
```json
{
  "type": "enum",
  "name": "UserRole",
  "namespace": "MyApp.Models",
  "symbols": ["Admin", "Manager", "User"]
}
```

注意点：
- Avroの`enum`は文字列シンボルのみ
- C#の列挙型の基底型（int等）の情報は失われる

### 3.3 コレクション型

```csharp
// C#コレクション
public class Order
{
    public string OrderId { get; set; }
    public List<string> ProductIds { get; set; }
    public Dictionary<string, double> ProductPrices { get; set; }
}
```

対応するAvroスキーマ：
```json
{
  "type": "record",
  "name": "Order",
  "namespace": "MyApp.Models",
  "fields": [
    {"name": "OrderId", "type": "string"},
    {"name": "ProductIds", "type": {"type": "array", "items": "string"}},
    {"name": "ProductPrices", "type": {"type": "map", "values": "double"}}
  ]
}
```

### 3.4 Nullable型と共用体（Union）

```csharp
// C#のNullable型
public class Customer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int? Age { get; set; }            // Nullable int
    public DateTime? LastLogin { get; set; } // Nullable DateTime
}
```

対応するAvroスキーマ：
```json
{
  "type": "record",
  "name": "Customer",
  "namespace": "MyApp.Models",
  "fields": [
    {"name": "Id", "type": "string"},
    {"name": "Name", "type": "string"},
    {"name": "Age", "type": ["null", "int"], "default": null},
    {"name": "LastLogin", "type": ["null", {"type": "long", "logicalType": "timestamp-millis"}], "default": null}
  ]
}
```

## 4. 精度に関する特別な考慮事項

### 4.1 Decimal型の取り扱い

C#の`decimal`型はAvroの`decimal`論理型にマッピングされますが、精度とスケールに注意が必要です：

```csharp
// C#
decimal amount = 1234.56789m;  // 精度が高い

// Avro (precision=7, scale=2)
// → 1234.57（小数点以下3桁目以降は切り捨てまたは四捨五入）
```

高精度な計算が必要な場合の推奨設定：
- 金融取引：少なくとも`precision=16, scale=4`
- 科学計算：用途に応じて適切な精度を設定

### 4.2 日時型の精度

C#とAvroの日時型のマッピングでは、精度に注意が必要です：

| シナリオ | 推奨Avro型 | 備考 |
|---------|----------|------|
| 日付のみ | `date` | 時刻情報なし |
| ミリ秒精度 | `timestamp-millis` | C#のDateTime標準精度と一致 |
| マイクロ秒精度 | `timestamp-micros` | C#では特別な処理が必要 |
| タイムゾーン付き | カスタム対応 | タイムゾーン情報を別フィールドで保存 |

微小時間の計測や高頻度取引では、`timestamp-micros`の使用が推奨されますが、C#での処理には注意が必要です。

### 4.3 文字列とバイナリデータの考慮事項

- **文字列エンコーディング**: Avroの`string`型はUTF-8でエンコードされます。C#側でも適切に処理する必要があります。
- **長いテキスト**: 非常に長いテキストデータは、パフォーマンス上の理由からバイナリ（`bytes`）として保存することを検討してください。
- **バイナリデータサイズ**: 大きなバイナリデータは分割を検討してください。

## 5. 実装例とベストプラクティス

### 5.1 Confluent.SchemaRegistry.Serdes.Avroライブラリの使用

```csharp
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

// スキーマレジストリの設定
var schemaRegistryConfig = new SchemaRegistryConfig
{
    Url = "http://localhost:8081"
};
var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

// プロデューサーの設定
var producerConfig = new ProducerConfig
{
    BootstrapServers = "localhost:9092"
};

// Avroシリアライザの設定
var avroSerializerConfig = new AvroSerializerConfig
{
    // 自動スキーマ登録を有効に
    AutoRegisterSchemas = true
};

// Avroシリアライザとプロデューサーの作成
var serializer = new AvroSerializer<User>(schemaRegistry, avroSerializerConfig);
using var producer = new ProducerBuilder<string, User>(producerConfig)
    .SetValueSerializer(serializer)
    .Build();

// メッセージ送信
var user = new User { Id = "user123", Name = "John Doe", Age = 30 };
await producer.ProduceAsync("users-topic", new Message<string, User> { Key = user.Id, Value = user });
```

### 5.2 スキーマ互換性に関するベストプラクティス

1. **前方互換性の維持**:
   - 新しいフィールドは常にNullable（`["null", "type"]`）にする
   - デフォルト値を指定する

2. **精度に関する変更を避ける**:
   - `int`から`long`への変更は安全
   - `double`から`float`への変更は避ける
   - `decimal`の精度変更は注意が必要

3. **初期設計時の考慮事項**:
   - 将来的な拡張を考慮して十分な精度を設定
   - 特に日時型と数値型の精度は余裕を持たせる

## 6. トラブルシューティング

### 6.1 一般的な変換エラー

| エラーパターン | 考えられる原因 | 解決策 |
|--------------|--------------|--------|
| 数値オーバーフロー | 値が型の範囲を超えている | より大きな型にアップグレード |
| 精度損失 | 高精度から低精度への変換 | 精度を保持できる型を使用 |
| シリアライズエラー | スキーマとオブジェクト構造の不一致 | スキーマとクラス定義を同期 |
| 日時変換エラー | タイムゾーンや精度の不一致 | 適切な日時型と変換ロジックを使用 |

### 6.2 デバッグのヒント

- シリアライズ前のデータを詳細にログに記録
- デシリアライズ後のデータと元データを比較
- スキーマレジストリに登録されたスキーマを確認
- 特に重要なフィールドの精度をテストケースで検証

## 7. まとめ

C#とKafka間でAvroを使用したデータ交換を行う際は、以下のポイントを考慮してください：

1. **適切なデータ型マッピングの選択**:
   - 数値精度の要件を満たす型を選択
   - 日時データの精度とタイムゾーン要件を明確にする

2. **スキーマ設計の注意点**:
   - 前方/後方互換性を考慮
   - 必要十分な精度を設定
   - Null許容フィールドの適切な処理

3. **実装とテスト**:
   - エッジケースのテスト（最大値、最小値、特殊なフォーマット）
   - シリアライズ/デシリアライズの往復テスト
   - 異なるバージョン間の互換性テスト

C#の強力な型システムとAvroのスキーマベースのシリアライゼーションを組み合わせることで、型安全で効率的なKafkaメッセージングシステムを構築できます。
