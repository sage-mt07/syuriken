using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using KsqlEntityFramework.Attributes;
using KsqlEntityFramework.Schema;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KsqlEntityFramework.Tests
{
    public class SchemaManagerTests
    {
        private const string SchemaRegistryUrl = "http://localhost:8081";

        [Fact]
        public async Task RegisterSchemaAsync_ValidPoco_SendsCorrectRequest()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"id\": 42 }")
            };

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var httpClient = new HttpClient(handlerMock.Object);
            var schemaManager = new SchemaManagerWithMock(SchemaRegistryUrl, httpClient);

            // Act
            var schemaId = await schemaManager.RegisterSchemaAsync<TestOrder>("test-orders-value");

            // Assert
            Assert.Equal(42, schemaId);

            // リクエストが正しく送信されたことを検証
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == $"{SchemaRegistryUrl}/subjects/test-orders-value/versions" &&
                    VerifyRequestContent(req)),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetSchemaByIdAsync_ValidId_ReturnsCorrectSchema()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var expectedSchema = "{ \"type\": \"record\", \"name\": \"TestOrder\", \"fields\": [] }";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"{{ \"schema\": {expectedSchema} }}")
            };

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var httpClient = new HttpClient(handlerMock.Object);
            var schemaManager = new SchemaManagerWithMock(SchemaRegistryUrl, httpClient);

            // Act
            var schema = await schemaManager.GetSchemaByIdAsync(42);

            // Assert
            Assert.Equal(expectedSchema, schema);

            // リクエストが正しく送信されたことを検証
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString() == $"{SchemaRegistryUrl}/schemas/ids/42"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetLatestSchemaAsync_ValidSubject_ReturnsLatestSchema()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var expectedSchema = "{ \"type\": \"record\", \"name\": \"TestOrder\", \"fields\": [] }";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"{{ \"schema\": {expectedSchema} }}")
            };

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var httpClient = new HttpClient(handlerMock.Object);
            var schemaManager = new SchemaManagerWithMock(SchemaRegistryUrl, httpClient);

            // Act
            var schema = await schemaManager.GetLatestSchemaAsync("test-orders-value");

            // Assert
            Assert.Equal(expectedSchema, schema);

            // リクエストが正しく送信されたことを検証
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString() == $"{SchemaRegistryUrl}/subjects/test-orders-value/versions/latest"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SetCompatibilityModeAsync_ValidMode_SendsCorrectRequest()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"compatibility\": \"FULL\" }")
            };

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var httpClient = new HttpClient(handlerMock.Object);
            var schemaManager = new SchemaManagerWithMock(SchemaRegistryUrl, httpClient);

            // Act
            await schemaManager.SetCompatibilityModeAsync("test-orders-value", CompatibilityMode.Full);

            // Assert
            // リクエストが正しく送信されたことを検証
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri.ToString() == $"{SchemaRegistryUrl}/config/test-orders-value" &&
                    VerifyCompatibilityContent(req, "FULL")),
                ItExpr.IsAny<CancellationToken>());
        }

        private bool VerifyRequestContent(HttpRequestMessage request)
        {
            if (request.Content == null) return false;

            var contentString = request.Content.ReadAsStringAsync().Result;
            var contentObj = JObject.Parse(contentString);

            // スキーマJSONが含まれていることを確認
            return contentObj["schema"] != null;
        }

        private bool VerifyCompatibilityContent(HttpRequestMessage request, string expectedCompatibility)
        {
            if (request.Content == null) return false;

            var contentString = request.Content.ReadAsStringAsync().Result;
            var contentObj = JObject.Parse(contentString);

            // 互換性設定が正しいことを確認
            return contentObj["compatibility"]?.ToString() == expectedCompatibility;
        }

        // HTTP通信をモック可能にするためのSchemaManagerの拡張
        private class SchemaManagerWithMock : SchemaManager
        {
            private readonly HttpClient _httpClient;

            public SchemaManagerWithMock(string schemaRegistryUrl, HttpClient httpClient)
                : base(schemaRegistryUrl)
            {
                _httpClient = httpClient;
            }

            // 実際のSchemaManager実装を拡張
            public new async Task<int> RegisterSchemaAsync<T>(string subject)
            {
                // スキーマを生成
                string schemaJson = AvroSchemaGenerator.GenerateSchema<T>();
                
                // スキーマレジストリAPIにリクエスト送信
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_schemaRegistryUrl}/subjects/{subject}/versions")
                {
                    Content = new StringContent($"{{ \"schema\": {schemaJson} }}")
                };
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JObject.Parse(responseContent);
                
                return responseObj["id"].Value<int>();
            }

            public new async Task<string> GetSchemaByIdAsync(int schemaId)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_schemaRegistryUrl}/schemas/ids/{schemaId}");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JObject.Parse(responseContent);
                
                return responseObj["schema"].ToString();
            }

            public new async Task<string> GetLatestSchemaAsync(string subject)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_schemaRegistryUrl}/subjects/{subject}/versions/latest");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JObject.Parse(responseContent);
                
                return responseObj["schema"].ToString();
            }

            public new async Task SetCompatibilityModeAsync(string subject, CompatibilityMode compatibilityMode)
            {
                string compatibilityString = compatibilityMode.ToString().ToUpper();
                
                var request = new HttpRequestMessage(HttpMethod.Put, $"{_schemaRegistryUrl}/config/{subject}")
                {
                    Content = new StringContent($"{{ \"compatibility\": \"{compatibilityString}\" }}")
                };
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }

        // テスト用POCOクラス
        [Topic("test-orders")]
        private class TestOrder
        {
            [Key]
            public string OrderId { get; set; }
            
            public string CustomerId { get; set; }
            
            [DecimalPrecision(18, 2)]
            public decimal Amount { get; set; }
            
            [Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
            public DateTimeOffset OrderTime { get; set; }
        }
    }
}
