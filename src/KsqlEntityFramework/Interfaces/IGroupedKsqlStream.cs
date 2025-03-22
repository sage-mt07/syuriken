 public interface IGroupedKsqlStream<TKey, TElement>
    {
        /// <summary>
        /// グループのキー
        /// </summary>
        TKey Key { get; }

        /// <summary>
        /// ウィンドウ情報
        /// </summary>
        WindowInfo Window { get; }

        /// <summary>
        /// 集計操作
        /// </summary>
        /// <typeparam name="TResult">結果の型</typeparam>
        /// <param name="aggregateSelector">集計関数</param>
        /// <returns>集計結果のストリーム</returns>
        IKsqlStream<TResult> Aggregate<TResult>(Expression<Func<IGroupedKsqlStream<TKey, TElement>, TResult>> aggregateSelector);

        /// <summary>
        /// 要素数を取得
        /// </summary>
        /// <returns>要素数</returns>
        int Count();

        /// <summary>
        /// 指定したプロパティの合計を計算
        /// </summary>
        /// <typeparam name="TProperty">プロパティの型</typeparam>
        /// <param name="selector">プロパティ選択関数</param>
        /// <returns>合計値</returns>
        TProperty Sum<TProperty>(Expression<Func<TElement, TProperty>> selector);

        /// <summary>
        /// 最新の値を取得（LATEST_BY_OFFSET）
        /// </summary>
        /// <typeparam name="TProperty">プロパティの型</typeparam>
        /// <param name="selector">プロパティ選択関数</param>
        /// <returns>最新のプロパティ値</returns>
        TProperty LatestByOffset<TProperty>(Expression<Func<TElement, TProperty>> selector);

        /// <summary>
        /// 最古の値を取得（EARLIEST_BY_OFFSET）
        /// </summary>
        /// <typeparam name="TProperty">プロパティの型</typeparam>
        /// <param name="selector">プロパティ選択関数</param>
        /// <returns>最古のプロパティ値</returns>
        TProperty EarliestByOffset<TProperty>(Expression<Func<TElement, TProperty>> selector);
    }
