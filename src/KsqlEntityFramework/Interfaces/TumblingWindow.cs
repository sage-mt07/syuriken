public class TumblingWindow : WindowDefinition
    {
        /// <summary>
        /// ウィンドウの長さ
        /// </summary>
        public TimeSpan Size { get; }

        /// <summary>
        /// ウィンドウの種類
        /// </summary>
        public override WindowType Type => WindowType.Tumbling;

        private TumblingWindow(TimeSpan size)
        {
            Size = size;
        }

        /// <summary>
        /// 指定したサイズのタンブリングウィンドウを作成
        /// </summary>
        /// <param name="size">ウィンドウサイズ</param>
        /// <returns>タンブリングウィンドウ</returns>
        public static TumblingWindow Of(TimeSpan size)
        {
            return new TumblingWindow(size);
        }
    }
