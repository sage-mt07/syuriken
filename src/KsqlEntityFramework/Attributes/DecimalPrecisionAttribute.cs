namespace KsqlEntityFramework.Attributes;
 [AttributeUsage(AttributeTargets.Property)]
    public class DecimalPrecisionAttribute : Attribute
    {
        /// <summary>
        /// 精度（全体の桁数）
        /// </summary>
        public int Precision { get; }

        /// <summary>
        /// スケール（小数点以下の桁数）
        /// </summary>
        public int Scale { get; }

        /// <summary>
        /// Decimal精度属性の初期化
        /// </summary>
        /// <param name="precision">精度（全体の桁数）</param>
        /// <param name="scale">スケール（小数点以下の桁数）</param>
        public DecimalPrecisionAttribute(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }
    }
    
