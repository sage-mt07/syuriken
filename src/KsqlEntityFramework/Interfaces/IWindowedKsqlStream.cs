    public interface IWindowedKsqlStream<T> : IQueryable<T>
    {
        /// <summary>
        /// グループ化操作
        /// </summary>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <param name="keySelector">キー選択関数</param>
        /// <returns>グループ化されたストリーム</returns>
        IGroupedKsqlStream<TKey, T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector);
    }
