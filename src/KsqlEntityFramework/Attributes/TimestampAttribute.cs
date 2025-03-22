using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace KsqlEntityFramework.Attributes;
[AttributeUsage(AttributeTargets.Property)]
    public class TimestampAttribute : Attribute
    {
        /// <summary>
        /// タイムスタンプのフォーマット
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// タイムスタンプのタイプ
        /// </summary>
        public TimestampType Type { get; set; } = TimestampType.EventTime;
    }
    
