   public class DatabaseFacade
    {
        private readonly KsqlDbContext _context;

        /// <summary>
        /// DatabaseFacadeを初期化
        /// </summary>
        /// <param name="context">コンテキスト</param>
        public DatabaseFacade(KsqlDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// トピックを削除
        /// </summary>
        /// <param name="topicName">トピック名</param>
        public async Task DropTopicAsync(string topicName)
        {
            // トピックの削除処理
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装
        }

        /// <summary>
        /// テーブルを削除
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        public async Task DropTableAsync(string tableName)
        {
            // テーブルの削除処理
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装
        }

        /// <summary>
        /// テーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="tableName">テーブル名</param>
        /// <param name="optionsAction">テーブルオプション設定アクション</param>
        public async Task CreateTableAsync<T>(string tableName, Action<TableOptions<T>> optionsAction)
        {
            var options = new TableOptions<T>();
            optionsAction?.Invoke(options);

            // テーブルの作成処理
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装
        }

        /// <summary>
        /// KSQLを実行
        /// </summary>
        /// <param name="ksql">実行するKSQL</param>
        public async Task ExecuteKsqlAsync(string ksql)
        {
            // KSQLの実行処理
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装
        }
    }
