 public class EntityChange<T>
    {
        /// <summary>
        /// 変更の種類
        /// </summary>
        public ChangeType ChangeType { get; set; }

        /// <summary>
        /// 変更されたエンティティ
        /// </summary>
        public T Entity { get; set; }
    }
