using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KsqlEntityFramework.Attributes;
using KsqlEntityFramework.Schema;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KsqlEntityFramework.Tests
{
    public class AvroSchemaGeneratorTests
    {
        [Fact]
        public void GenerateSchema_SimpleClass_GeneratesValidSchema()
        {
            // Arrange
            var expectedName = "SimpleClass";
            var expectedNamespace = "KsqlEntityFramework.Tests";

            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<SimpleClass>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            Assert.Equal("record", schemaObj["type"]);
            Assert.Equal(expectedName, schemaObj["name"]);
            Assert.Equal(expectedNamespace, schemaObj["namespace"]);
            
            // フィールドの検証
            var fields = schemaObj["fields"] as JArray;
            Assert.NotNull(fields);
            Assert.Equal(3, fields.Count);
            
            // Id フィールドの検証
            var idField = fields[0];
            Assert.Equal("Id", idField["name"]);
            Assert.Equal("string", idField["type"]);
            Assert.Equal("Primary key field", idField["doc"]);
            
            // Name フィールドの検証
            var nameField = fields[1];
            Assert.Equal("Name", nameField["name"]);
            Assert.Equal("string", nameField["type"]);
            
            // Value フィールドの検証
            var valueField = fields[2];
            Assert.Equal("Value", valueField["name"]);
            Assert.Equal("int", valueField["type"]);
        }
        
        [Fact]
        public void GenerateSchema_ClassWithTopic_IncludesTopicMetadata()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<OrderWithTopic>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            Assert.Equal("Schema for test_orders topic", schemaObj["doc"]);
        }
        
        [Fact]
        public void GenerateSchema_DecimalProperty_GeneratesCorrectType()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<OrderWithTopic>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            var fields = schemaObj["fields"] as JArray;
            
            // Amount フィールドを探す
            JObject amountField = null;
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == "Amount")
                {
                    amountField = field;
                    break;
                }
            }
            
            Assert.NotNull(amountField);
            
            // decimal型のプロパティが正しく変換されていることを確認
            var typeObj = amountField["type"] as JObject;
            Assert.Equal("bytes", typeObj["type"]);
            Assert.Equal("decimal", typeObj["logicalType"]);
            Assert.Equal(20, typeObj["precision"]);
            Assert.Equal(2, typeObj["scale"]);
        }
        
        [Fact]
        public void GenerateSchema_TimestampProperty_GeneratesCorrectType()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<OrderWithTopic>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            var fields = schemaObj["fields"] as JArray;
            
            // OrderTime フィールドを探す
            JObject orderTimeField = null;
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == "OrderTime")
                {
                    orderTimeField = field;
                    break;
                }
            }
            
            Assert.NotNull(orderTimeField);
            
            // タイムスタンプ型のプロパティが正しく変換されていることを確認
            var typeObj = orderTimeField["type"] as JObject;
            Assert.Equal("long", typeObj["type"]);
            Assert.Equal("timestamp-millis", typeObj["logicalType"]);
            Assert.Equal("Timestamp field (EventTime)", orderTimeField["doc"]);
            Assert.Equal("yyyy-MM-dd'T'HH:mm:ss.SSS", orderTimeField["format"]);
        }
        
        [Fact]
        public void GenerateSchema_NullableProperty_GeneratesUnionType()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<OrderWithTopic>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            var fields = schemaObj["fields"] as JArray;
            
            // DiscountPercent フィールドを探す
            JObject discountField = null;
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == "DiscountPercent")
                {
                    discountField = field;
                    break;
                }
            }
            
            Assert.NotNull(discountField);
            
            // null許容型のプロパティがunion型として正しく変換されていることを確認
            var typeArray = discountField["type"] as JArray;
            Assert.Equal(2, typeArray.Count);
            Assert.Equal("null", typeArray[0]);
            Assert.Equal("int", typeArray[1]);
        }
        
        [Fact]
        public void GenerateSchema_EnumProperty_GeneratesEnumType()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<CustomerWithEnum>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            var fields = schemaObj["fields"] as JArray;
            
            // CustomerType フィールドを探す
            JObject customerTypeField = null;
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == "CustomerType")
                {
                    customerTypeField = field;
                    break;
                }
            }
            
            Assert.NotNull(customerTypeField);
            
            // enum型のプロパティが正しく変換されていることを確認
            var typeObj = customerTypeField["type"] as JObject;
            Assert.Equal("enum", typeObj["type"]);
            Assert.Equal("CustomerType", typeObj["name"]);
            
            var symbols = typeObj["symbols"] as JArray;
            Assert.Equal(3, symbols.Count);
            Assert.Contains("Regular", symbols.Select(s => s.ToString()));
            Assert.Contains("Premium", symbols.Select(s => s.ToString()));
            Assert.Contains("VIP", symbols.Select(s => s.ToString()));
        }
        
        [Fact]
        public void GenerateSchema_CollectionProperty_GeneratesArrayType()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<CustomerWithEnum>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            var fields = schemaObj["fields"] as JArray;
            
            // Addresses フィールドを探す
            JObject addressesField = null;
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == "Addresses")
                {
                    addressesField = field;
                    break;
                }
            }
            
            Assert.NotNull(addressesField);
            
            // コレクション型のプロパティが配列型として正しく変換されていることを確認
            var typeObj = addressesField["type"] as JObject;
            Assert.Equal("array", typeObj["type"]);
            
            var itemsObj = typeObj["items"] as JObject;
            Assert.Equal("record", itemsObj["type"]);
            Assert.Equal("Address", itemsObj["name"]);
            
            var nestedFields = itemsObj["fields"] as JArray;
            Assert.True(nestedFields.Count >= 3); // 少なくとも3つのフィールドがあるべき
        }
        
        [Fact]
        public void GenerateSchema_DateTimeProperty_GeneratesCorrectType()
        {
            // Act
            var schema = AvroSchemaGenerator.GenerateSchema<CustomerWithEnum>();
            
            // Assert
            var schemaObj = JObject.Parse(schema);
            var fields = schemaObj["fields"] as JArray;
            
            // RegistrationDate フィールドを探す
            JObject regDateField = null;
            foreach (JObject field in fields)
            {
                if (field["name"].ToString() == "RegistrationDate")
                {
                    regDateField = field;
                    break;
                }
            }
            
            Assert.NotNull(regDateField);
            
            // DateFormatの適用されたプロパティが日付型として正しく変換されていることを確認
            var typeObj = regDateField["type"] as JObject;
            Assert.Equal("int", typeObj["type"]);
            Assert.Equal("date", typeObj["logicalType"]);
        }
        
        #region テスト用POCOクラス

        private class SimpleClass
        {
            [Key]
            public string Id { get; set; }
            public string Name { get; set; }
            public int Value { get; set; }
        }
        
        [Topic("test_orders", PartitionCount = 8, ReplicationFactor = 3)]
        private class OrderWithTopic
        {
            [Key]
            public string OrderId { get; set; }
            
            public string CustomerId { get; set; }
            
            [DecimalPrecision(20, 2)]
            public decimal Amount { get; set; }
            
            [Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
            public DateTimeOffset OrderTime { get; set; }
            
            [DefaultValue(false)]
            public bool IsProcessed { get; set; }
            
            public int? DiscountPercent { get; set; }
        }
        
        [Topic("test_customers")]
        private class CustomerWithEnum
        {
            [Key]
            public string CustomerId { get; set; }
            
            public string Name { get; set; }
            
            [DateTimeFormat(Format = "yyyy-MM-dd")]
            public DateTime RegistrationDate { get; set; }
            
            public CustomerType CustomerType { get; set; }
            
            public List<Address> Addresses { get; set; }
        }
        
        private enum CustomerType
        {
            Regular,
            Premium,
            VIP
        }
        
        private class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string ZipCode { get; set; }
            [DefaultValue("Japan")]
            public string Country { get; set; }
        }
        
        #endregion
    }
}
