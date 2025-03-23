using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using KsqlEntityFramework.Attributes;
using KsqlEntityFramework.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KsqlEntityFramework.Tests.Integration
{
    public class AvroSchemaIntegrationTests
    {
        // テスト用POCOクラス定義
        [Topic("complex_orders")]
        private class ComplexOrder
        {
            [Key]
            public string OrderId { get; set; }
            
            public string CustomerId { get; set; }
            
            [DecimalPrecision(18, 2)]
            public decimal Amount { get; set; }
            
            [Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
            public DateTimeOffset OrderTime { get; set; }
            
            public Address ShippingAddress { get; set; }
            
            public List<OrderItem> Items { get; set; }
            
            public OrderStatus Status { get; set; }
            
            public Dictionary<string, string> Tags { get; set; }
            
            public string? Notes { get; set; }
        }
        
        private class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string ZipCode { get; set; }
            [DefaultValue("Japan")]
            public string Country { get; set; }
        }
        
        private class OrderItem
        {
            public string ProductId { get; set; }
            public int Quantity { get; set; }
            [DecimalPrecision(10, 2)]
            public decimal Price { get; set; }
        }
        
        private enum OrderStatus
        {
            New,
            Processing,
            Shipped,
            Delivered
        }
        [Fact]
        public void FullSchemaGenerationProcess_ComplexPoco_CreatesValidSchema()
        {
            // テスト対象のPOCOクラス
            var order = new ComplexOrder
            {
                OrderId = "ORD-12345",
                CustomerId = "CUST-789",
                Amount = 299.99m,
                OrderTime = DateTimeOffset.Now,
                ShippingAddress = new Address
                {
                    Street = "123 Main St",
                    City = "Tokyo",
                    ZipCode = "100-0001",
                    Country = "Japan"
                },
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = "PROD-1", Quantity = 2, Price = 99.99m },
                    new OrderItem { ProductId = "PROD-2", Quantity = 1, Price = 100.01m }
                },
                Status = OrderStatus.Processing,
                Tags = new Dictionary<string, string>
                {
                    { "priority", "high" },
                    { "channel", "online" }
                }
            };

            // スキーマを生成
            string schemaJson = AvroSchemaGenerator.GenerateSchema<ComplexOrder>();
            
            // 生成されたスキーマが有効なJSONであることを確認
            var schema = JObject.Parse(schemaJson);
            
            // スキーマの基本構造を検証
            Assert.Equal("record", schema["type"]);
            Assert.Equal("ComplexOrder", schema["name"]);
            Assert.Equal("Schema for complex_orders topic", schema["doc"]);
            
            // フィールドの検証
            var fields = schema["fields"] as JArray;
            Assert.NotNull(fields);
            
            // スキーマからデータをシリアライズできることをシミュレート（実際のシリアライズは行わない）
            var serializable = CanSerialize(order, schema);
            Assert.True(serializable, "スキーマを使用してオブジェクトをシリアライズできるはずです");
            
            // スキーマをファイルに保存できることを確認（テスト用）
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, schemaJson, Encoding.UTF8);
            
            // ファイルから読み込んで同じスキーマであることを確認
            var loadedSchema = File.ReadAllText(tempFile, Encoding.UTF8);
            Assert.Equal(
                JObject.Parse(schemaJson).ToString(Formatting.None), 
                JObject.Parse(loadedSchema).ToString(Formatting.None)
            );
            
            // テンポラリファイルを削除
            File.Delete(tempFile);
            
            // スキーマレジストリとの統合テスト (モック使用)
            var schemaManager = new MockSchemaManager();
            var schemaId = schemaManager.RegisterSchemaAsync<ComplexOrder>("complex_orders-value").Result;
            
            // スキーマIDが生成されていることを確認
            Assert.True(schemaId > 0, "スキーマIDが生成されているはずです");
            
            // 登録したスキーマを取得できることを確認
            var retrievedSchema = schemaManager.GetLatestSchemaAsync("complex_orders-value").Result;
            Assert.NotNull(retrievedSchema);
            Assert.NotEmpty(retrievedSchema);
        }
        
        // スキーマレジストリをモックするためのクラス
        private class MockSchemaManager
        {
            private readonly Dictionary<string, string> _schemas = new Dictionary<string, string>();
            private int _nextSchemaId = 1;
            
            public Task<int> RegisterSchemaAsync<T>(string subject)
            {
                var schema = AvroSchemaGenerator.GenerateSchema<T>();
                _schemas[subject] = schema;
                return Task.FromResult(_nextSchemaId++);
            }
            
            public Task<string> GetLatestSchemaAsync(string subject)
            {
                if (_schemas.TryGetValue(subject, out var schema))
                {
                    return Task.FromResult(schema);
                }
                
                return Task.FromResult<string>(null);
            }
        }
        
        [Fact]
        public void VerifyFieldTypes_ComplexPoco_CorrectTypeMappings()
        {
            // スキーマを生成
            string schemaJson = AvroSchemaGenerator.GenerateSchema<ComplexOrder>();
            var schema = JObject.Parse(schemaJson);
            var fields = schema["fields"] as JArray;
            
            // 各フィールドタイプの検証
            VerifyField(fields, "OrderId", "string");
            VerifyField(fields, "CustomerId", "string");
            
            // decimal型の検証
            var amountField = FindField(fields, "Amount");
            var amountType = amountField["type"] as JObject;
            Assert.Equal("bytes", amountType["type"]);
            Assert.Equal("decimal", amountType["logicalType"]);
            Assert.Equal(18, amountType["precision"]);
            Assert.Equal(2, amountType["scale"]);
            
            // 日時型の検証
            var orderTimeField = FindField(fields, "OrderTime");
            var orderTimeType = orderTimeField["type"] as JObject;
            Assert.Equal("long", orderTimeType["type"]);
            Assert.Equal("timestamp-millis", orderTimeType["logicalType"]);
            
            // 複合型(Address)の検証
            var addressField = FindField(fields, "ShippingAddress");
            var addressType = addressField["type"] as JObject;
            Assert.Equal("record", addressType["type"]);
            Assert.Equal("Address", addressType["name"]);
            
            var addressFields = addressType["fields"] as JArray;
            Assert.NotNull(addressFields);
            Assert.Equal(4, addressFields.Count);
            
            // リスト型の検証
            var itemsField = FindField(fields, "Items");
            var itemsType = itemsField["type"] as JObject;
            Assert.Equal("array", itemsType["type"]);
            
            var itemType = itemsType["items"] as JObject;
            Assert.Equal("record", itemType["type"]);
            Assert.Equal("OrderItem", itemType["name"]);
            
            // Enum型の検証
            var statusField = FindField(fields, "Status");
            var statusType = statusField["type"] as JObject;
            Assert.Equal("enum", statusType["type"]);
            Assert.Equal("OrderStatus", statusType["name"]);
            
            var symbols = statusType["symbols"] as JArray;
            Assert.Equal(4, symbols.Count);
            
            // Dictionary型の検証
            var tagsField = FindField(fields, "Tags");
            var tagsType = tagsField["type"] as JObject;
            Assert.Equal("map", tagsType["type"]);
            Assert.Equal("string", tagsType["values"]);
            
            // nullable型の検証
            var notesField = FindField(fields, "Notes");
            var notesType = notesField["type"] as JArray;
            Assert.Equal("null", notesType[0]);
            Assert.Equal("string", notesType[1]);
        }
        
        private JObject FindField(JArray fields, string fieldName)
        {
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == fieldName)
                {
                    return field;
                }
            }
            
            Assert.Fail($"フィールド '{fieldName}' が見つかりませんでした");
            return null; // ここには到達しないが、コンパイラのために必要
        }
        
        private void VerifyField(JArray fields, string fieldName, string expectedType)
        {
            var field = FindField(fields, fieldName);
            Assert.Equal(expectedType, field["type"].ToString());
        }
        
        private bool CanSerialize(object data, JObject schema)
        {
            // 実際のシリアライズは行わず、スキーマの構造を検証するためのメソッド
            // 実際のシリアライズにはAvroやConfluent.Kafkaライブラリを使用する
            
            try
            {
                // スキーマのフィールドを取得
                var fields = schema["fields"] as JArray;
                if (fields == null) return false;
                
                // データ型がスキーマに合致するか簡易的に検証
                var type = data.GetType();
                foreach (JObject field in fields)
                {
                    var fieldName = field["name"].ToString();
                    var property = type.GetProperty(fieldName);
                    
                    // プロパティが存在するか確認
                    if (property == null) return false;
                    
                    // プロパティの型がスキーマの型と互換性があるか確認
                    // (厳密な検証ではないが、基本的な構造の整合性を確認)
                    // 実際のAvroシリアライズでは、より詳細な型チェックが必要
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
