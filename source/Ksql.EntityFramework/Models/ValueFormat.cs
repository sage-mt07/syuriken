namespace Ksql.EntityFramework.Models
{
    /// <summary>
    /// Specifies the serialization format for values in a Kafka topic.
    /// </summary>
    public enum ValueFormat
    {       
        /// <summary>
        /// Avro binary format with schema registry support.
        /// </summary>
        Avro,

        /// <summary>
        /// JSON format.
        /// </summary>
        Json,

        /// <summary>
        /// Protobuf binary format.
        /// </summary>
        Protobuf,

        /// <summary>
        /// CSV format.
        /// </summary>
        Csv,

        /// <summary>
        /// Delimited text format.
        /// </summary>
        Delimited
    }
}
