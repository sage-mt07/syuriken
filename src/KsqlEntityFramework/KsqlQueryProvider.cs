  public class KsqlQueryProvider : IQueryProvider
    {
        /// <summary>
        /// クエリを作成
        /// </summary>
        /// <typeparam name="TElement">要素の型</typeparam>
        /// <param name="expression">式ツリー</param>
        /// <returns>クエリオブジェクト</returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            // クエリ作成の実装
            throw new NotImplementedException();
        }

        /// <summary>
        /// クエリを作成（非ジェネリック）
        /// </summary>
        /// <param name="expression">式ツリー</param>
        /// <returns>クエリオブジェクト</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            // クエリ作成の実装
            throw new NotImplementedException();
        }

        /// <summary>
        /// クエリを実行
        /// </summary>
        /// <typeparam name="TResult">結果の型</typeparam>
        /// <param name="expression">式ツリー</param>
        /// <returns>実行結果</returns>
        public TResult Execute<TResult>(Expression expression)
        {
            // クエリ実行の実装
            throw new NotImplementedException();
        }

        /// <summary>
        /// クエリを実行（非ジェネリック）
        /// </summary>
        /// <param name="expression">式ツリー</param>
        /// <returns>実行結果</returns>
        public object Execute(Expression expression)
        {
            // クエリ実行の実装
            throw new NotImplementedException();
        }
    }
