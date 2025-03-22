namespace KsqlEntityFramework;

    /// <summary>
    /// KSQL DbContextのオプション設定
    /// </summary>
    public class KsqlDbContextOptions
    {
        /// <summary>
        /// KSQLサーバーのURL
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// Schema Registryのアドレス
        /// </summary>
        public string SchemaRegistryUrl { get; set; }

        /// <summary>
        /// デシリアライズエラー発生時のポリシー
        /// </summary>
        public ErrorPolicy DeserializationErrorPolicy { get; set; } = ErrorPolicy.Skip;

        /// <summary>
        /// デッドレターキュー（DLQ）のトピック名
        /// </summary>
        public string DeadLetterQueue { get; set; }

        /// <summary>
        /// デッドレターキューにエラーメッセージを送信するハンドラー
        /// </summary>
        public Func<object, Exception, object> DeadLetterQueueErrorHandler { get; set; }
    }
