namespace KsqlEntityFramework.Attributes;
[AttributeUsage(AttributeTargets.Property)]
    public class DateTimeFormatAttribute : Attribute
    {
        /// <summary>
        /// 日時のフォーマット
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// ロケール
        /// </summary>
        public string Locale { get; set; } = "en-US";
    }
    
