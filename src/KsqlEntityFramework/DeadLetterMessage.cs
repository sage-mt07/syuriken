public class DeadLetterMessage
    {
        /// <summary>
        /// 元のデータ
        /// </summary>
        public object OriginalData { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// タイムスタンプ
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
