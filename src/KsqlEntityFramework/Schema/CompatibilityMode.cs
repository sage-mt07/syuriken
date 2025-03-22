 public enum CompatibilityMode
    {
        /// <summary>
        /// 互換性チェックなし
        /// </summary>
        None,
        
        /// <summary>
        /// 後方互換性（新スキーマで古いデータを読める）
        /// </summary>
        Backward,
        
        /// <summary>
        /// 前方互換性（古いスキーマで新しいデータを読める）
        /// </summary>
        Forward,
        
        /// <summary>
        /// 完全互換性（前方・後方両方）
        /// </summary>
        Full
    }
}
