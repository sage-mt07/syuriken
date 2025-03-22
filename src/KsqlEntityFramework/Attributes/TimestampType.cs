namespace KsqlEntityFramework.Attributes;
public enum TimestampType
    {
        /// <summary>
        /// イベント発生時のタイムスタンプ
        /// </summary>
        EventTime,

        /// <summary>
        /// イベント処理時のタイムスタンプ
        /// </summary>
        ProcessingTime
    }
    
