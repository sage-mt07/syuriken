 public class KsqlDbContext : IKsqlDbContext, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// コンテキストのオプション設定
        /// </summary>
        public KsqlDbContextOptions Options { get; }

        /// <summary>
        /// データベース操作のための補助オブジェクト
        /// </summary>
        public DatabaseFacade Database { get; }

        /// <summary>
        /// KsqlDbContextを初期化
        /// </summary>
        /// <param name="options">コンテキストのオプション</param>
        public KsqlDbContext(KsqlDbContextOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Database = new DatabaseFacade(this);
        }

        /// <summary>
        /// 指定した型のストリームを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="name">ストリーム名</param>
        /// <returns>ストリームオブジェクト</returns>
        public IKsqlStream<T> CreateStream<T>(string name)
        {
            // 実装はここで行います
            return new KsqlStream<T>(this, name);
        }

        /// <summary>
        /// 指定した型のテーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="name">テーブル名</param>
        /// <param name="builderAction">テーブル構築のためのアクション</param>
        /// <returns>テーブルオブジェクト</returns>
        public IKsqlTable<T> CreateTable<T>(string name, Action<TableBuilder<T>> builderAction = null)
        {
            var builder = new TableBuilder<T>();
            builderAction?.Invoke(builder);
            return new KsqlTable<T>(this, name, builder);
        }

        /// <summary>
        /// トピックが存在することを確認（存在しなければ作成）
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <returns>作成したトピック情報</returns>
        public async Task<TopicDescription> EnsureTopicCreatedAsync<T>()
        {
            // 型からトピック情報を取得
            var topicAttribute = typeof(T).GetCustomAttributes(typeof(TopicAttribute), true)
                .FirstOrDefault() as TopicAttribute;

            if (topicAttribute == null)
            {
                throw new InvalidOperationException($"型 {typeof(T).Name} にTopicAttributeが指定されていません。");
            }

            // トピックの設定
            var topicDescription = new TopicDescription
            {
                Name = topicAttribute.Name,
                PartitionCount = topicAttribute.PartitionCount,
                ReplicationFactor = topicAttribute.ReplicationFactor
            };

            // トピックの作成（実際のKafka API呼び出しはここで実装）
            await Task.CompletedTask; // 仮の実装

            return topicDescription;
        }

        /// <summary>
        /// 型情報からストリームを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <returns>作成したストリーム</returns>
        public async Task<IKsqlStream<T>> EnsureStreamCreatedAsync<T>()
        {
            // 型からトピック情報を取得
            var topicAttribute = typeof(T).GetCustomAttributes(typeof(TopicAttribute), true)
                .FirstOrDefault() as TopicAttribute;

            if (topicAttribute == null)
            {
                throw new InvalidOperationException($"型 {typeof(T).Name} にTopicAttributeが指定されていません。");
            }

            // まずトピックの作成を確認
            await EnsureTopicCreatedAsync<T>();

            // ストリーム名を生成（通常はトピック名に基づく）
            string streamName = $"{topicAttribute.Name}_stream";

            // ストリームの作成（実際のKSQL API呼び出しはここで実装）
            // 実装例はここで行います

            return CreateStream<T>(streamName);
        }

        /// <summary>
        /// 型情報からテーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <returns>作成したテーブル</returns>
        public async Task<IKsqlTable<T>> EnsureTableCreatedAsync<T>()
        {
            // 型からトピック情報を取得
            var topicAttribute = typeof(T).GetCustomAttributes(typeof(TopicAttribute), true)
                .FirstOrDefault() as TopicAttribute;

            if (topicAttribute == null)
            {
                throw new InvalidOperationException($"型 {typeof(T).Name} にTopicAttributeが指定されていません。");
            }

            // まずトピックの作成を確認
            await EnsureTopicCreatedAsync<T>();

            // テーブル名を生成（通常はトピック名に基づく）
            string tableName = $"{topicAttribute.Name}_table";

            // テーブルの作成（実際のKSQL API呼び出しはここで実装）
            // 実装例はここで行います

            return CreateTable<T>(tableName);
        }

        /// <summary>
        /// 特定のテーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="table">テーブルオブジェクト</param>
        /// <returns>作成したテーブル</returns>
        public async Task<IKsqlTable<T>> EnsureTableCreatedAsync<T>(IKsqlTable<T> table)
        {
            // テーブルの作成（実際のKSQL API呼び出しはここで実装）
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装

            return table;
        }

        /// <summary>
        /// トランザクションを開始
        /// </summary>
        /// <returns>トランザクションオブジェクト</returns>
        public async Task<IKsqlTransaction> BeginTransactionAsync()
        {
            // トランザクションの開始（実際のKafka API呼び出しはここで実装）
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装

            return new KsqlTransaction(this);
        }

        /// <summary>
        /// メタデータを更新
        /// </summary>
        public async Task RefreshMetadataAsync()
        {
            // メタデータの更新処理
            await Task.CompletedTask; // 仮の実装
        }

        /// <summary>
        /// 変更をコミット
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 変更のコミット処理
            // 実装例はここで行います
            await Task.CompletedTask; // 仮の実装

            return 0; // 変更された行数
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            // リソースの解放処理
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースの非同期解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // リソースの非同期解放処理
            await Task.CompletedTask; // 仮の実装
            GC.SuppressFinalize(this);
        }
    }
