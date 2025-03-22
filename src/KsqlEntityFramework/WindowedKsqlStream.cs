 public class WindowedKsqlStream<T> : IWindowedKsqlStream<T>
    {
        private readonly IKsqlStream<T> _stream;
        private readonly WindowDefinition _window;

        /// <summary>
        /// ウィンドウ付きストリームを初期化
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="window">ウィンドウ定義</param>
        public WindowedKsqlStream(IKsqlStream<T> stream, WindowDefinition window)
        {
            _stream = stream;
            _window = window;
        }

        /// <summary>
        /// グループ化操作
        /// </summary>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <param name="keySelector">キー選択関数</param>
        /// <returns>グループ化されたストリーム</returns>
        public IGroupedKsqlStream<TKey, T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            // グループ化の実装
            return new GroupedKsqlStream<TKey, T>(keySelector, _window);
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
