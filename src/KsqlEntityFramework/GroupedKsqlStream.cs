 public class GroupedKsqlStream<TKey, TElement> : IGroupedKsqlStream<TKey, TElement>
    {
        private readonly Expression<Func<TElement, TKey>> _keySelector;
        private readonly WindowDefinition _window;

        /// <summary>
        /// グループのキー
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// ウィンドウ情報
        /// </summary>
        public WindowInfo Window { get; } = new WindowInfo();

        /// <summary>
        /// グループ化されたストリームを初期化
        /// </summary>
        /// <param name="keySelector">キー選択関数</param>
        /// <param name="window">ウィンドウ定義</param>
        public GroupedKsqlStream(Expression<Func<TElement, TKey>> keySelector, WindowDefinition window)
        {
            _keySelector = keySelector;
            _window = window;
        }

        /// <summary>
        /// 集計操作
        /// </summary>
        /// <typeparam name="TResult">結果の型</typeparam>
        /// <param name="aggregateSelector">集計関数</param>
        /// <returns>集計結果のストリーム</returns>
        public IKsqlStream<TResult> Aggregate<TResult>(Expression<Func<IGroupedKsqlStream<TKey, TElement>, TResult>> aggregateSelector)
        {
            // 集計の実装
            // 実際のKSQL構文への変換はここで行います
            throw new NotImplementedException();
        }

        /// <summary>
        /// 要素数を取得
        /// </summary>
        /// <returns>要素数</returns>
        public int Count()
        {
            // カウント集計の実装
            // 実際のKSQL構文への変換はここで行います
            return 0;
        }

        /// <summary>
        /// 指定したプロパティの合計を計算
        /// </summary>
        /// <typeparam name="TProperty">プロパティの型</typeparam>
        /// <param name="selector">プロパティ選択関数</param>
        /// <returns>合計値</returns>
        public TProperty Sum<TProperty>(Expression<Func<TElement, TProperty>> selector)
        {
            // 合計集計の実装
            // 実際のKSQL構文への変換はここで行います
            return default;
        }

        /// <summary>
        /// 最新の値を取得（LATEST_BY_OFFSET）
        /// </summary>
        /// <typeparam name="TProperty">プロパティの型</typeparam>
        /// <param name="selector">プロパティ選択関数</param>
        /// <returns>最新のプロパティ値</returns>
        public TProperty LatestByOffset<TProperty>(Expression<Func<TElement, TProperty>> selector)
        {
            // LATEST_BY_OFFSET集計の実装
            // 実際のKSQL構文への変換はここで行います
            return default;
        }

        /// <summary>
        /// 最古の値を取得（EARLIEST_BY_OFFSET）
        /// </summary>
        /// <typeparam name="TProperty">プロパティの型</typeparam>
        /// <param name="selector">プロパティ選択関数</param>
        /// <returns>最古のプロパティ値</returns>
        public TProperty EarliestByOffset<TProperty>(Expression<Func<TElement, TProperty>> selector)
        {
            // EARLIEST_BY_OFFSET集計の実装
            // 実際のKSQL構文への変換はここで行います
            return default;
        }
    }
