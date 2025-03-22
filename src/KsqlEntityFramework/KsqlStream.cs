public class KsqlStream<T> : IKsqlStream<T>
    {
        private readonly KsqlDbContext _context;
        private readonly string _streamName;
        private readonly List<T> _pendingEntities = new List<T>();

        /// <summary>
        /// ストリームを初期化
        /// </summary>
        /// <param name="context">コンテキスト</param>
        /// <param name="streamName">ストリーム名</param>
        public KsqlStream(KsqlDbContext context, string streamName)
        {
            _context = context;
            _streamName = streamName;
        }

        /// <summary>
        /// 単一エンティティをプロデュース
        /// </summary>
        /// <param name="entity">エンティティ</param>
        /// <returns>処理されたレコード数</returns>
        public async Task<long> ProduceAsync(T entity)
        {
            // 実際のKafkaプロデューサー呼び出しはここで実装
            // デモ実装
            await Task.CompletedTask;
            return 1;
        }

        /// <summary>
        /// キーを指定してエンティティをプロデュース
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="entity">エンティティ</param>
        /// <returns>処理されたレコード数</returns>
        public async Task<long> ProduceAsync(string key, T entity)
        {
            // キーを指定してプロデュース
            // 実際のKafkaプロデューサー呼び出しはここで実装
            // デモ実装
            await Task.CompletedTask;
            return 1;
        }

        /// <summary>
        /// バッチでエンティティをプロデュース
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        /// <returns>処理されたレコード数</returns>
        public async Task<long> ProduceBatchAsync(IEnumerable<T> entities)
        {
            // バッチプロデュース
            // 実際のKafkaプロデューサー呼び出しはここで実装
            // デモ実装
            await Task.CompletedTask;
            return entities.Count();
        }

        /// <summary>
        /// ストリームを購読
        /// </summary>
        /// <returns>エンティティの非同期列挙</returns>
        public async IAsyncEnumerable<T> SubscribeAsync()
        {
            // ストリーム購読の実装
            // 実際のKafkaコンシューマー呼び出しはここで実装
            // デモ実装
            await Task.CompletedTask;
            yield break;
        }

        /// <summary>
        /// 新しいエンティティを追加
        /// </summary>
        /// <param name="entity">エンティティ</param>
        public void Add(T entity)
        {
            // 保留中のエンティティリストに追加
            _pendingEntities.Add(entity);
        }

        /// <summary>
        /// 複数のエンティティを追加
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        public void AddRange(IEnumerable<T> entities)
        {
            // 保留中のエンティティリストに範囲追加
            _pendingEntities.AddRange(entities);
        }

        /// <summary>
        /// ウォーターマークを設定
        /// </summary>
        /// <param name="timestampSelector">タイムスタンプ選択関数</param>
        /// <param name="delay">遅延時間</param>
        /// <returns>ウォーターマーク付きストリーム</returns>
        public IKsqlStream<T> WithWatermark(Expression<Func<T, DateTimeOffset>> timestampSelector, TimeSpan delay)
        {
            // ウォーターマークの設定
            // 実装はここで行います
            return this;
        }

        /// <summary>
        /// ウィンドウ処理を適用
        /// </summary>
        /// <param name="window">ウィンドウ定義</param>
        /// <returns>ウィンドウ付きストリーム</returns>
        public IWindowedKsqlStream<T> Window(WindowDefinition window)
        {
            // ウィンドウの適用
            return new WindowedKsqlStream<T>(this, window);
        }

        /// <summary>
        /// エラー処理ポリシーを設定
        /// </summary>
        /// <param name="action">エラー処理アクション</param>
        /// <returns>エラー処理設定付きストリーム</returns>
        public IKsqlStream<T> OnError(ErrorAction action)
        {
            // エラー処理の設定
            // 実装はここで行います
            return this;
        }

        /// <summary>
        /// リトライ回数を設定
        /// </summary>
        /// <param name="retryCount">リトライ回数</param>
        /// <returns>リトライ設定付きストリーム</returns>
        public IKsqlStream<T> WithRetry(int retryCount)
        {
            // リトライの設定
            // 実装はここで行います
            return this;
        }

        /// <summary>
        /// マッピング関数を適用
        /// </summary>
        /// <typeparam name="TResult">結果の型</typeparam>
        /// <param name="mapper">マッピング関数</param>
        /// <returns>マッピングされたストリーム</returns>
        public IKsqlStream<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            // マッピングの適用
            // 実装はここで行います
            throw new NotImplementedException();
        }

        #region IQueryable<T> の実装

        /// <summary>
        /// エンティティのタイプを取得
        /// </summary>
        public Type ElementType => typeof(T);

        /// <summary>
        /// 式ツリーを取得
        /// </summary>
        public Expression Expression => Expression.Constant(this);

        /// <summary>
        /// クエリプロバイダを取得
        /// </summary>
        public IQueryProvider Provider => new KsqlQueryProvider();

        /// <summary>
        /// 列挙子を取得
        /// </summary>
        /// <returns>列挙子</returns>
        public IEnumerator<T> GetEnumerator()
        {
            // デモ実装
            return Enumerable.Empty<T>().GetEnumerator();
        }

        /// <summary>
        /// 列挙子を取得（非ジェネリック）
        /// </summary>
        /// <returns>列挙子</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
