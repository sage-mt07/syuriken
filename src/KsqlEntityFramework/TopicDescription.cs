public class TopicDescription
    {
        /// <summary>
        /// トピック名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// パーティション数
        /// </summary>
        public int PartitionCount { get; set; }

        /// <summary>
        /// レプリケーションファクター
        /// </summary>
        public short ReplicationFactor { get; set; }
    }
