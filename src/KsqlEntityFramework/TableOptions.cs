 public class TableOptions<T>
    {
        /// <summary>
        /// キー列の定義
        /// </summary>
        public Func<T, object> KeyColumns { get; private set; }

        /// <summary>
        /// 基となるトピック名
        /// </summary>
        public string TopicName { get; private set; }

        /// <summary>
        /// 値のフォーマット
        /// </summary>
        public ValueFormat ValueFormat { get; private set; } = ValueFormat.Json;

        /// <summary>
        /// キー列を設定
        /// </summary>
        /// <param name="keySelector">キー選択関数</param>
        /// <returns>このインスタンス</returns>
        public TableOptions<T> WithKeyColumns(Func<T, object> keySelector)
        {
            KeyColumns = keySelector;
            return this;
        }

        /// <summary>
        /// トピックを設定
        /// </summary>
        /// <param name="topicName">トピック名</param>
        /// <returns>このインスタンス</returns>
        public TableOptions<T> WithTopic(string topicName)
        {
            TopicName = topicName;
            return this;
        }

        /// <summary>
        /// 値のフォーマットを設定
        /// </summary>
        /// <param name="format">フォーマット</param>
        /// <returns>このインスタンス</returns>
        public TableOptions<T> WithValueFormat(ValueFormat format)
        {
            ValueFormat = format;
            return this;
        }
    }
