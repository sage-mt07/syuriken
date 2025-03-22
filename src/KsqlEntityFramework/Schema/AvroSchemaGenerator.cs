public static class AvroSchemaGenerator
    {
        /// <summary>
        /// 指定された型からAvroスキーマを生成します
        /// </summary>
        /// <typeparam name="T">POCOクラスの型</typeparam>
        /// <returns>Avroスキーマの文字列表現</returns>
        public static string GenerateSchema<T>()
        {
            return GenerateSchema(typeof(T));
        }

        /// <summary>
        /// 指定された型からAvroスキーマを生成します
        /// </summary>
        /// <param name="type">POCOクラスの型</param>
        /// <returns>Avroスキーマの文字列表現</returns>
        public static string GenerateSchema(Type type)
        {
            var schema = new JObject
            {
                ["type"] = "record",
                ["name"] = type.Name,
                ["namespace"] = type.Namespace
            };

            // トピック属性からのメタデータ追加
            var topicAttribute = type.GetCustomAttribute<TopicAttribute>();
            if (topicAttribute != null)
            {
                schema["doc"] = $"Schema for {topicAttribute.Name} topic";
            }

            // フィールドの処理
            var fields = new JArray();
            foreach (var prop in type.GetProperties())
            {
                // プロパティからフィールド定義を生成
                var field = GenerateFieldSchema(prop);
                if (field != null)
                {
                    fields.Add(field);
                }
            }

            schema["fields"] = fields;
            return schema.ToString(Formatting.Indented);
        }

        /// <summary>
        /// プロパティからAvroフィールド定義を生成します
        /// </summary>
        /// <param name="property">プロパティ情報</param>
        /// <returns>Avroフィールド定義のJSONオブジェクト</returns>
        private static JObject GenerateFieldSchema(PropertyInfo property)
        {
            var field = new JObject
            {
                ["name"] = property.Name
            };

            // フィールドの型を決定
            var avroType = MapCSharpTypeToAvroType(property);
            field["type"] = avroType;

            // プロパティの属性を処理
            ProcessPropertyAttributes(property, field);

            return field;
        }

        /// <summary>
        /// C#の型をAvroの型にマッピングします
        /// </summary>
        /// <param name="property">プロパティ情報</param>
        /// <returns>Avro型の定義</returns>
        private static JToken MapCSharpTypeToAvroType(PropertyInfo property)
        {
            var type = property.PropertyType;
            var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            var isNullable = nullableUnderlyingType != null;
            
            // nullable型の場合は、基底型を使用
            type = nullableUnderlyingType ?? type;

            // 基本型のマッピング
            if (type == typeof(string))
            {
                return isNullable ? CreateNullableType("string") : "string";
            }
            else if (type == typeof(bool))
            {
                return isNullable ? CreateNullableType("boolean") : "boolean";
            }
            else if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
            {
                return isNullable ? CreateNullableType("int") : "int";
            }
            else if (type == typeof(long))
            {
                return isNullable ? CreateNullableType("long") : "long";
            }
            else if (type == typeof(float))
            {
                return isNullable ? CreateNullableType("float") : "float";
            }
            else if (type == typeof(double))
            {
                return isNullable ? CreateNullableType("double") : "double";
            }
            else if (type == typeof(decimal))
            {
                // Decimal型は特殊な属性があるか確認
                var precAttr = property.GetCustomAttribute<DecimalPrecisionAttribute>();
                
                // Avroでは固定小数点をbytesとlogicalTypeで表現
                var decimalSchema = new JObject
                {
                    ["type"] = "bytes",
                    ["logicalType"] = "decimal",
                    ["precision"] = precAttr?.Precision ?? 18, // デフォルトか属性で指定された精度
                    ["scale"] = precAttr?.Scale ?? 2           // デフォルトか属性で指定されたスケール
                };
                
                return isNullable ? CreateNullableType(decimalSchema) : decimalSchema;
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                // DateTime/DateTimeOffsetの処理
                var formatAttr = property.GetCustomAttribute<DateTimeFormatAttribute>();
                var timestampAttr = property.GetCustomAttribute<TimestampAttribute>();
                
                string logicalType = "timestamp-millis";
                if (formatAttr != null && formatAttr.Format.Contains("yyyy-MM-dd") && !formatAttr.Format.Contains("HH"))
                {
                    // 日付のみの場合
                    logicalType = "date";
                }
                
                var dateTimeSchema = new JObject
                {
                    ["type"] = logicalType == "date" ? "int" : "long",
                    ["logicalType"] = logicalType
                };
                
                return isNullable ? CreateNullableType(dateTimeSchema) : dateTimeSchema;
            }
            else if (type.IsEnum)
            {
                // Enum型の処理
                var enumSchema = new JObject
                {
                    ["type"] = "enum",
                    ["name"] = type.Name,
                    ["symbols"] = new JArray(Enum.GetNames(type))
                };
                
                return isNullable ? CreateNullableType(enumSchema) : enumSchema;
            }
            else if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                // 配列/リスト型の処理
                Type elementType;
                if (type.IsArray)
                {
                    elementType = type.GetElementType();
                }
                else
                {
                    elementType = type.GetGenericArguments()[0];
                }
                
                var arraySchema = new JObject
                {
                    ["type"] = "array",
                    ["items"] = ConvertTypeToAvroType(elementType)
                };
                
                return isNullable ? CreateNullableType(arraySchema) : arraySchema;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Dictionary型の処理
                var valueType = type.GetGenericArguments()[1];
                
                var mapSchema = new JObject
                {
                    ["type"] = "map",
                    ["values"] = ConvertTypeToAvroType(valueType)
                };
                
                return isNullable ? CreateNullableType(mapSchema) : mapSchema;
            }
            else if (!type.IsPrimitive && !type.IsEnum && type != typeof(string))
            {
                // 複合型の処理（別のレコード型）
                var recordSchema = new JObject
                {
                    ["type"] = "record",
                    ["name"] = type.Name,
                    ["namespace"] = type.Namespace
                };
                
                var fields = new JArray();
                foreach (var prop in type.GetProperties())
                {
                    var field = GenerateFieldSchema(prop);
                    if (field != null)
                    {
                        fields.Add(field);
                    }
                }
                
                recordSchema["fields"] = fields;
                return isNullable ? CreateNullableType(recordSchema) : recordSchema;
            }
            
            // デフォルトでは文字列として扱う
            return isNullable ? CreateNullableType("string") : "string";
        }

        /// <summary>
        /// 基本的な型をAvro型に変換します
        /// </summary>
        /// <param name="type">C#の型</param>
        /// <returns>Avro型の定義</returns>
        private static JToken ConvertTypeToAvroType(Type type)
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            var isNullable = nullableUnderlyingType != null;
            type = nullableUnderlyingType ?? type;
            
            if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(bool))
            {
                return "boolean";
            }
            else if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
            {
                return "int";
            }
            else if (type == typeof(long))
            {
                return "long";
            }
            else if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(double))
            {
                return "double";
            }
            else if (type == typeof(decimal))
            {
                // デフォルトのdecimal設定
                return new JObject
                {
                    ["type"] = "bytes",
                    ["logicalType"] = "decimal",
                    ["precision"] = 18,
                    ["scale"] = 2
                };
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return new JObject
                {
                    ["type"] = "long",
                    ["logicalType"] = "timestamp-millis"
                };
            }
            
            // その他の型は文字列として扱う
            return "string";
        }

        /// <summary>
        /// ヌル許容型を作成します
        /// </summary>
        /// <param name="avroType">基本のAvro型</param>
        /// <returns>ヌル許容のAvro型（ユニオン型）</returns>
        private static JArray CreateNullableType(JToken avroType)
        {
            return new JArray { "null", avroType };
        }

        /// <summary>
        /// プロパティの属性を処理してフィールド定義に追加します
        /// </summary>
        /// <param name="property">プロパティ情報</param>
        /// <param name="field">Avroフィールド定義</param>
        private static void ProcessPropertyAttributes(PropertyInfo property, JObject field)
        {
            // キー属性の処理
            var keyAttr = property.GetCustomAttribute<KeyAttribute>();
            if (keyAttr != null)
            {
                field["doc"] = "Primary key field";
            }
            
            // タイムスタンプ属性の処理
            var timestampAttr = property.GetCustomAttribute<TimestampAttribute>();
            if (timestampAttr != null)
            {
                field["doc"] = $"Timestamp field ({timestampAttr.Type})";
                if (!string.IsNullOrEmpty(timestampAttr.Format))
                {
                    field["format"] = timestampAttr.Format;
                }
            }
            
            // デフォルト値属性の処理
            var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValueAttr != null)
            {
                field["default"] = JToken.FromObject(defaultValueAttr.Value);
            }
        }
    }
