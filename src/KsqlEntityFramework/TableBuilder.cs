 public class TableBuilder<T>
    {
        /// <summary>
        /// ベースとなるストリーム
        /// </summary>
        private IKsqlStream<T> _baseStream;

        /// <summary>
        /// ベースとなるトピック名
        /// </summary>
        private string _baseTopic;

        /// <summary>
        /// ストリームからテーブルを構築
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <returns>このインスタンス</returns>
        public TableBuilder<T> FromStream(IKsqlStream<T> stream)
        {
            _baseStream = stream;
            return this;
        }

        /// <summary>
        /// トピックからテーブルを構築
        /// </summary>
        /// <typeparam name="TEntity">エンティティの型</typeparam>
        /// <param name="topicName">トピック名</param>
        /// <returns>このインスタンス</returns>
        public TableBuilder<T> FromTopic<TEntity>(string topicName)
        {
            _baseTopic = topicName;
            return this;
        }
    }
