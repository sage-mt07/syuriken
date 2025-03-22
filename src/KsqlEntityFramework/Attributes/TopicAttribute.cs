using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace KsqlEntityFramework.Attributes;
 [AttributeUsage(AttributeTargets.Class)]
    public class TopicAttribute : Attribute
    {
        /// <summary>
        /// トピック名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// パーティション数
        /// </summary>
        public int PartitionCount { get; set; } = 1;

        /// <summary>
        /// レプリケーションファクター
        /// </summary>
        public short ReplicationFactor { get; set; } = 1;

        /// <summary>
        /// トピック属性の初期化
        /// </summary>
        /// <param name="name">トピック名</param>
        public TopicAttribute(string name)
        {
            Name = name;
        }
    }
