 public class SchemaManager
    {
        private readonly string _schemaRegistryUrl;
        
        /// <summary>
        /// スキーママネージャーを初期化
        /// </summary>
        /// <param name="schemaRegistryUrl">スキーマレジストリのURL</param>
        public SchemaManager(string schemaRegistryUrl)
        {
            _schemaRegistryUrl = schemaRegistryUrl;
        }
        
        /// <summary>
        /// POCOクラスのスキーマをレジストリに登録
        /// </summary>
        /// <typeparam name="T">POCOクラスの型</typeparam>
        /// <param name="subject">スキーマのサブジェクト名（通常はトピック名）</param>
        /// <returns>登録されたスキーマのID</returns>
        public async Task<int> RegisterSchemaAsync<T>(string subject)
        {
            // Avroスキーマを生成
            string schemaJson = AvroSchemaGenerator.GenerateSchema<T>();
            
            // スキーマレジストリAPIにリクエスト送信
            // 実際の実装ではHttpClientなどを使用してAPIと通信します
            // デモ実装
            await Task.CompletedTask;
            return 1; // デモ用のスキーマID
        }
        
        /// <summary>
        /// 指定されたスキーマIDのスキーマを取得
        /// </summary>
        /// <param name="schemaId">スキーマID</param>
        /// <returns>スキーマの文字列表現</returns>
        public async Task<string> GetSchemaByIdAsync(int schemaId)
        {
            // スキーマレジストリからスキーマを取得
            // デモ実装
            await Task.CompletedTask;
            return "{}"; // デモ用の空スキーマ
        }
        
        /// <summary>
        /// 指定されたサブジェクトの最新バージョンのスキーマを取得
        /// </summary>
        /// <param name="subject">スキーマのサブジェクト名</param>
        /// <returns>スキーマの文字列表現</returns>
        public async Task<string> GetLatestSchemaAsync(string subject)
        {
            // サブジェクトの最新スキーマを取得
            // デモ実装
            await Task.CompletedTask;
            return "{}"; // デモ用の空スキーマ
        }
        
        /// <summary>
        /// 指定されたサブジェクトのスキーマ互換性モードを設定
        /// </summary>
        /// <param name="subject">スキーマのサブジェクト名</param>
        /// <param name="compatibilityMode">互換性モード</param>
        public async Task SetCompatibilityModeAsync(string subject, CompatibilityMode compatibilityMode)
        {
            // 互換性モードを設定
            // デモ実装
            await Task.CompletedTask;
        }
    }
