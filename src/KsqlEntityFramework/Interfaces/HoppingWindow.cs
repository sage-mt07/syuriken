public class HoppingWindow : WindowDefinition
    {
        /// <summary>
        /// ウィンドウの長さ
        /// </summary>
        public TimeSpan Size { get; }

        /// <summary>
        /// ウィンドウの移動間隔
        /// </summary>
        public TimeSpan AdvanceBy { get; }

        /// <summary>
        /// ウィンドウの種類
        /// </summary>
        public override WindowType Type => WindowType.Hopping;

        private HoppingWindow(TimeSpan size, TimeSpan advanceBy)
        {
            Size = size;
            AdvanceBy = advanceBy;
        }

        /// <summary>
        /// 指定したサイズと間隔のホッピングウィンドウを作成
        /// </summary>
        /// <param name="size">ウィンドウサイズ</param>
        /// <param name="advanceBy">移動間隔</param>
        /// <returns>ホッピングウィンドウ</returns>
        public static HoppingWindow Of(TimeSpan size, TimeSpan advanceBy)
        {
            return new HoppingWindow(size, advanceBy);
        }
    }
