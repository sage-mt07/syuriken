 public static class KsqlQueryableExtensions
    {
        /// <summary>
        /// 非同期でクエリ結果のリストを取得
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="source">クエリソース</param>
        /// <returns>エンティティのリスト</returns>
        public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> source)
        {
            // 非同期リスト取得の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 非同期で最初の要素を取得
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="source">クエリソース</param>
        /// <returns>最初の要素</returns>
        public static async Task<T> FirstAsync<T>(this IQueryable<T> source)
        {
            // 非同期First取得の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return default;
        }

        /// <summary>
        /// 非同期で最初の要素を取得（存在しない場合はデフォルト値）
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="source">クエリソース</param>
        /// <returns>最初の要素またはデフォルト値</returns>
        public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source)
        {
            // 非同期FirstOrDefault取得の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return default;
        }

        /// <summary>
        /// 非同期で要素数を取得
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="source">クエリソース</param>
        /// <returns>要素数</returns>
        public static async Task<int> CountAsync<T>(this IQueryable<T> source)
        {
            // 非同期Count取得の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 非同期で要素の存在確認
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="source">クエリソース</param>
        /// <returns>要素が存在するかどうか</returns>
        public static async Task<bool> AnyAsync<T>(this IQueryable<T> source)
        {
            // 非同期Any取得の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// 条件を指定した非同期での要素の存在確認
        /// </summary>
        /// <typeparam name="T">エンティティの型</typeparam>
        /// <param name="source">クエリソース</param>
        /// <param name="predicate">条件</param>
        /// <returns>条件を満たす要素が存在するかどうか</returns>
        public static async Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            // 非同期Any(predicate)取得の実装
            // 実際のKSQLクエリ実行はここで行います
            await Task.CompletedTask;
            return false;
        }
    }
