  public class SessionWindow : WindowDefinition
    {
        /// <summary>
        /// セッションの非アクティブ時間のしきい値
        /// </summary>
        public TimeSpan Inactivity { get; }

        /// <summary>
        /// ウィンドウの種類
        /// </summary>
        public override WindowType Type => WindowType.Session;

        private SessionWindow(TimeSpan inactivity)
        {
            Inactivity = inactivity;
        }

        /// <summary>
        /// 指定した非アクティブ時間のセッションウィンドウを作成
        /// </summary>
        /// <param name="inactivity">非アクティブ時間</param>
        /// <returns>セッションウィンドウ</returns>
        public static SessionWindow WithInactivityGapOf(TimeSpan inactivity)
        {
            return new SessionWindow(inactivity);
        }
    }
