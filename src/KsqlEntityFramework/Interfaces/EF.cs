public static class EF
    {
        /// <summary>
        /// 関数提供クラス
        /// </summary>
        public static class Functions
        {
            /// <summary>
            /// 2つの日時の日数差を計算
            /// </summary>
            /// <param name="startDate">開始日時</param>
            /// <param name="endDate">終了日時</param>
            /// <returns>日数</returns>
            public static int DateDiffDays(DateTimeOffset startDate, DateTimeOffset endDate)
            {
                // 実装はクエリ変換時に行われるため、ここではダミー実装
                return (int)(endDate - startDate).TotalDays;
            }
        }
    }
