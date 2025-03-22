public interface IKsqlStream<T> : IQueryable<T>
    {
        /// <summary>
        /// 単一エンティティをプロデュース
        /// </summary>
        /// <param name="entity">エンティティ</param>
        /// <returns>処理されたレコード数</returns>
        Task<long> ProduceAsync(T entity);

        /// <summary>
        /// キーを指定してエンティティをプロデュース
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="entity">エンティティ</param>
        /// <returns>処理されたレコード数</returns>
        Task<long> ProduceAsync(string key, T entity);

        /// <summary>
        /// バッチでエンティティをプロデュース
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        /// <returns>処理されたレコード数</returns>
        Task<long> ProduceBatchAsync(IEnumerable<T> entities);

        /// <summary>
        /// ストリームを購読
        /// </summary>
        /// <returns>エンティティの非同期列挙</returns>
        IAsyncEnumerable<T> SubscribeAsync();

        /// <summary>
        /// 新しいエンティティを追加
        /// </summary>
        /// <param name="entity">エンティティ</param>
        void Add(T entity);

        /// <summary>
        /// 複数のエンティティを追加
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        void AddRange(IEnumerable<T> entities);

        /// <summary>
        /// ウォーターマークを設定
        /// </summary>
        /// <param name="timestampSelector">タイムスタンプ選択関数</param>
        /// <param name="delay">遅延時間</param>
        /// <returns>ウォーターマーク付きストリーム</returns>
        IKsqlStream<T> WithWatermark(Expression<Func<T, DateTimeOffset>> timestampSelector, TimeSpan delay);

        /// <summary>
        /// ウィンドウ処理を適用
        /// </summary>
        /// <param name="window">ウィンドウ定義</param>
        /// <returns>ウィンドウ付きストリーム</returns>
        IWindowedKsqlStream<T> Window(WindowDefinition window);

        /// <summary>
        /// エラー処理ポリシーを設定
        /// </summary>
        /// <param name="action">エラー処理アクション</param>
        /// <returns>エラー処理設定付きストリーム</returns>
        IKsqlStream<T> OnError(ErrorAction action);
    }
