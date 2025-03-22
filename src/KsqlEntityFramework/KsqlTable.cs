public class KsqlTable<T> : IKsqlTable<T>
    {
        private readonly KsqlDbContext _context;
        private readonly string _tableName;
        private readonly TableBuilder<T> _builder;
        private readonly List<T> _addedEntities = new List<T>();
        private readonly List<T> _removedEntities = new List<T>();

        /// <summary>
        /// テーブルを初期化
        /// </summary>
        /// <param name="context">コンテキスト</param>
        /// <param name="tableName">テーブル名</param>
        /// <param name="builder">テーブル構築情報</param>
        public KsqlTable(KsqlDbContext context, string tableName, TableBuilder<T> builder = null)
        {
            _context = context;
            _tableName = tableName;
            _builder = builder;
        }

        /// <summary>
        /// キーを指定して単一エンティティを取得
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>エンティティ</returns>
        public async Task<T> GetAsync(object key)
        {
            // キーによる検索の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return default;
        }

        /// <summary>
        /// キーを指定して単一エンティティを取得
        /// </summary>
        /// <param name="keys">複合キー</param>
        /// <returns>エンティティ</returns>
        public async Task<T> FindAsync(params object[] keys)
        {
            // 複合キーによる検索の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return default;
        }

        /// <summary>
        /// エンティティを挿入
        /// </summary>
        /// <param name="entity">エンティティ</param>
        /// <returns>成功したかどうか</returns>
        public async Task<bool> InsertAsync(T entity)
        {
            // 挿入の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// エンティティを追加
        /// </summary>
        /// <param name="entity">エンティティ</param>
        public void Add(T entity)
        {
            // 追加対象リストに追加
            _addedEntities.Add(entity);
        }

        /// <summary>
        /// 複数のエンティティを追加
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        public void AddRange(IEnumerable<T> entities)
        {
            // 追加対象リストに範囲追加
            _addedEntities.AddRange(entities);
        }

        /// <summary>
        /// エンティティを削除
        /// </summary>
        /// <param name="entity">エンティティ</param>
        public void Remove(T entity)
        {
            // 削除対象リストに追加
            _removedEntities.Add(entity);
        }

        /// <summary>
        /// 複数のエンティティを削除
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        public void RemoveRange(IEnumerable<T> entities)
        {
            // 削除対象リストに範囲追加
            _removedEntities.AddRange(entities);
        }

        /// <summary>
        /// 変更を監視
        /// </summary>
        /// <returns>変更の非同期列挙</returns>
        public async IAsyncEnumerable<EntityChange<T>> ObserveChangesAsync()
        {
            // 変更監視の実装
            // 実際のKafkaコンシューマー呼び出しはここで実装
            await Task.CompletedTask;
            yield break;
        }

        /// <summary>
        /// テーブルの結合操作
        /// </summary>
        /// <typeparam name="TInner">結合するテーブルの型</typeparam>
        /// <typeparam name="TKey">結合キーの型</typeparam>
        /// <typeparam name="TResult">結果の型</typeparam>
        /// <param name="inner">結合するテーブル</param>
        /// <param name="outerKeySelector">外部キー選択関数</param>
        /// <param name="innerKeySelector">内部キー選択関数</param>
        /// <param name="resultSelector">結果選択関数</param>
        /// <returns>結合結果のクエリ</returns>
        public IQueryable<TResult> Join<TInner, TKey, TResult>(
            IQueryable<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Expression<Func<T, TInner, TResult>> resultSelector)
        {
            // テーブル結合の実装
            // LINQの標準Joinメソッドを利用
            return Queryable.Join(
                this,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector);
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
