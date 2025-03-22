public enum ErrorAction
    {
        /// <summary>
        /// エラーが発生したレコードをスキップ
        /// </summary>
        Skip,

        /// <summary>
        /// エラーが発生したレコードを再試行
        /// </summary>
        Retry,

        /// <summary>
        /// エラーが発生したらDLQに送信
        /// </summary>
        DeadLetterQueue,

        /// <summary>
        /// エラーが発生したら例外をスロー
        /// </summary>
        Fail
    }
