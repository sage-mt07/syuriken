using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using KsqlEntityFramework.Attributes;
using KsqlEntityFramework.Schema;

namespace KsqlEntityFramework.Samples
{
    public class KsqlEntityFrameworkSample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("KSQL Entity Framework サンプル");
            Console.WriteLine("------------------------------");

            // シナリオ1: POCOからAvroスキーマの生成
            GenerateAndDisplaySchema();
            
            // シナリオ2: スキーマレジストリへの登録
            await RegisterSchemaToRegistry();
            
            // シナリオ3: KSQLコンテキストを使用したトピック操作
            await UseKsqlContext();
            
            Console.WriteLine("サンプル完了");
        }
        
        private static void GenerateAndDisplaySchema()
        {
            Console.WriteLine("\n1. POCOからAvroスキーマを生成");
            Console.WriteLine("---------------------------");
            
            // 注文クラスのAvroスキーマを生成
            string orderSchema = AvroSchemaGenerator.GenerateSchema<Order>();
            Console.WriteLine($"Order Schema:\n{orderSchema}\n");
            
            // 顧客クラスのAvroスキーマを生成
            string customerSchema = AvroSchemaGenerator.GenerateSchema<Customer>();
            Console.WriteLine($"Customer Schema:\n{customerSchema}\n");
        }
        
        private static async Task RegisterSchemaToRegistry()
        {
            Console.WriteLine("\n2. スキーマレジストリへの登録");
            Console.WriteLine("---------------------------");
            
            // スキーママネージャーの初期化
            var schemaManager = new SchemaManager("http://localhost:8081");
            
            try
            {
                // 注文スキーマの登録
                int orderSchemaId = await schemaManager.RegisterSchemaAsync<Order>("orders-value");
                Console.WriteLine($"注文スキーマを登録しました。スキーマID: {orderSchemaId}");
                
                // 互換性設定
                await schemaManager.SetCompatibilityModeAsync("orders-value", CompatibilityMode.Backward);
                Console.WriteLine("注文スキーマの互換性モードを設定しました: BACKWARD");
                
                // 顧客スキーマの登録
                int customerSchemaId = await schemaManager.RegisterSchemaAsync<Customer>("customers-value");
                Console.WriteLine($"顧客スキーマを登録しました。スキーマID: {customerSchemaId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"スキーマ登録中にエラーが発生しました: {ex.Message}");
                Console.WriteLine("スキーマレジストリが実行されていることを確認してください。");
            }
        }
        
        private static async Task UseKsqlContext()
        {
            Console.WriteLine("\n3. KSQLコンテキストを使用したトピック操作");
            Console.WriteLine("-------------------------------------");
            
            try
            {
                // KSQLコンテキストの初期化
                using (var context = new SampleKsqlContext("http://localhost:8088"))
                {
                    // トピックの作成確認
                    await context.EnsureTopicCreatedAsync<Order>();
                    Console.WriteLine("注文トピックの存在を確認しました");
                    
                    await context.EnsureTopicCreatedAsync<Customer>();
                    Console.WriteLine("顧客トピックの存在を確認しました");
                    
                    // ストリームの作成確認
                    await context.EnsureStreamCreatedAsync<Order>();
                    Console.WriteLine("注文ストリームの存在を確認しました");
                    
                    // データの送信
                    var order = new Order
                    {
                        OrderId = $"ORD-{DateTime.Now.Ticks % 10000}",
                        CustomerId = "CUST-123",
                        Amount = 299.99m,
                        OrderTime = DateTimeOffset.Now
                    };
                    
                    await context.Orders.ProduceAsync(order);
                    Console.WriteLine($"注文データを送信しました: {order.OrderId}");
                    
                    // データの取得
                    var query = context.CreateQueryAsync(
                        $"SELECT * FROM orders WHERE OrderId = '{order.OrderId}'");
                        
                    Console.WriteLine("データ取得のためにクエリを実行しました...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KSQL操作中にエラーが発生しました: {ex.Message}");
                Console.WriteLine("KSQL ServerとKafkaが実行されていることを確認してください。");
            }
        }
        
        // サンプルKSQLコンテキスト
        public class SampleKsqlContext : IDisposable
        {
            private readonly string _ksqlServerUrl;
            
            public SampleKsqlContext(string ksqlServerUrl)
            {
                _ksqlServerUrl = ksqlServerUrl;
            }
            
            // ストリームプロパティ
            public IKsqlStream<Order> Orders { get; set; }
            public IKsqlStream<Customer> Customers { get; set; }
            
            // デモ用の実装
            public Task EnsureTopicCreatedAsync<T>()
            {
                // 実際の実装ではKafka Admin APIでトピックの存在を確認し、存在しなければ作成します
                return Task.CompletedTask;
            }
            
            public Task EnsureStreamCreatedAsync<T>()
            {
                // 実際の実装ではKSQLでストリームの存在を確認し、存在しなければ作成します
                return Task.CompletedTask;
            }
            
            public Task CreateQueryAsync(string ksqlQuery)
            {
                // 実際の実装ではKSQL HTTPエンドポイントにクエリを送信します
                return Task.CompletedTask;
            }
            
            public void Dispose()
            {
                // リソースのクリーンアップ
            }
        }
        
        // ストリームインターフェース
        public interface IKsqlStream<T>
        {
            Task<long> ProduceAsync(T entity);
        }
    }
    
    // POCOクラスの定義
    [Topic("orders", PartitionCount = 12, ReplicationFactor = 3)]
    public class Order
    {
        [Key]
        public string OrderId { get; set; }
        
        public string CustomerId { get; set; }
        
        [DecimalPrecision(18, 2)]
        public decimal Amount { get; set; }
        
        [Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
        public DateTimeOffset OrderTime { get; set; }
        
        [DefaultValue(false)]
        public bool IsProcessed { get; set; }
        
        public int? DiscountPercent { get; set; }
    }
    
    [Topic("customers")]
    public class Customer
    {
        [Key]
        public string CustomerId { get; set; }
        
        public string Name { get; set; }
        
        [DateTimeFormat(Format = "yyyy-MM-dd")]
        public DateTime RegistrationDate { get; set; }
        
        public CustomerType CustomerType { get; set; }
        
        public List<Address> Addresses { get; set; }
    }
    
    public enum CustomerType
    {
        Regular,
        Premium,
        VIP
    }
    
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        [DefaultValue("Japan")]
        public string Country { get; set; }
    }
}
