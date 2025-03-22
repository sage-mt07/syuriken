public interface IKsqlTable<T> : IQueryable<T>
    {
        /// <summary>
        /// キーを指定して単一エンティティを取得
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>エンティティ</returns>
        Task<T> GetAsync(object key);

        /// <summary>
        /// キーを指定して単一エンティティを取得
        /// </summary>
        /// <param name="keys">複合キー</param>
        /// <returns>エンティティ</returns>
        Task<T> FindAsync(params object[] keys);

        /// <summary>
        /// エンティティを挿入
        /// </summary>
        /// <param name="entity">エンティティ</param>
        /// <returns>成功したかどうか</returns>
        Task<bool> InsertAsync(T entity);

        /// <summary>
        /// エンティティを追加
        /// </summary>
        /// <param name="entity">エンティティ</param>
        void Add(T entity);

        /// <summary>
        /// 複数のエンティティを追加
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        void AddRange(IEnumerable<T> entities);

        /// <summary>
        /// エンティティを削除
        /// </summary>
        /// <param name="entity">エンティティ</param>
        void Remove(T entity);

        /// <summary>
        /// 複数のエンティティを削除
        /// </summary>
        /// <param name="entities">エンティティのリスト</param>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// 変更を監視
        /// </summary>
        /// <returns>変更の非同期列挙</returns>
        IAsyncEnumerable<EntityChange<T>> ObserveChangesAsync();
    }
