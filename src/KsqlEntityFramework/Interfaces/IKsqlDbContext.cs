public interface IKsqlDbContext : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 指定した型のストリームを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="name">ストリーム名</param>
        /// <returns>ストリームオブジェクト</returns>
        IKsqlStream<T> CreateStream<T>(string name);

        /// <summary>
        /// 指定した型のテーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="name">テーブル名</param>
        /// <param name="builderAction">テーブル構築アクション</param>
        /// <returns>テーブルオブジェクト</returns>
        IKsqlTable<T> CreateTable<T>(string name, Action<TableBuilder<T>> builderAction = null);

        /// <summary>
        /// トランザクションを開始
        /// </summary>
        /// <returns>トランザクションオブジェクト</returns>
        Task<IKsqlTransaction> BeginTransactionAsync();

        /// <summary>
        /// 変更をコミット
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>変更された行数</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// トピックが存在することを確認（存在しなければ作成）
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <returns>作成したトピック情報</returns>
        Task<TopicDescription> EnsureTopicCreatedAsync<T>();

        /// <summary>
        /// 型情報からストリームを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <returns>作成したストリーム</returns>
        Task<IKsqlStream<T>> EnsureStreamCreatedAsync<T>();

        /// <summary>
        /// 型情報からテーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <returns>作成したテーブル</returns>
        Task<IKsqlTable<T>> EnsureTableCreatedAsync<T>();

        /// <summary>
        /// 特定のテーブルを作成
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="table">テーブルオブジェクト</param>
        /// <returns>作成したテーブル</returns>
        Task<IKsqlTable<T>> EnsureTableCreatedAsync<T>(IKsqlTable<T> table);

        /// <summary>
        /// メタデータを更新
        /// </summary>
        Task RefreshMetadataAsync();
    }
