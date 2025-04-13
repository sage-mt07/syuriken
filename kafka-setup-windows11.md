# Windows 11でのKafka設定手順

このドキュメントでは、Windows 11環境でApache Kafkaを設定する手順を説明します。

## 前提条件
1. **Javaのインストール**
   - KafkaはJavaランタイム環境（JRE）が必要です。
   - ライセンス料がかからないOpenJDKを使用することを推奨します。
   - [Adoptiumの公式サイト](https://adoptium.net/)から最新のOpenJDKをダウンロードしてインストールしてください。
   - 環境変数`JAVA_HOME`を設定し、`java -version`コマンドでインストールを確認します。

2. **Kafkaのダウンロード**
   - [Apache Kafkaの公式サイト](https://kafka.apache.org/downloads)から最新バージョンをダウンロードします。
   - ZIPファイルを解凍し、適切なディレクトリに配置します（例: `C:\kafka`）。

## Kafkaの設定

### 1. Zookeeperの起動
KafkaはZookeeperを使用してクラスタの管理を行います。

1. 解凍したKafkaディレクトリに移動します。
   ```cmd
   cd C:\kafka
   ```

2. Zookeeperを起動します。
   ```cmd
   .\bin\windows\zookeeper-server-start.bat .\config\zookeeper.properties
   ```

### 2. Kafkaブローカーの起動
1. 別のコマンドプロンプトを開き、Kafkaディレクトリに移動します。
   ```cmd
   cd C:\kafka
   ```

2. Kafkaブローカーを起動します。
   ```cmd
   .\bin\windows\kafka-server-start.bat .\config\server.properties
   ```

### 3. トピックの作成
1. 別のコマンドプロンプトを開き、Kafkaディレクトリに移動します。
   ```cmd
   cd C:\kafka
   ```

2. トピックを作成します。
   ```cmd
   .\bin\windows\kafka-topics.bat --create --topic test-topic --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1
   ```

3. トピックのリストを確認します。
   ```cmd
   .\bin\windows\kafka-topics.bat --list --bootstrap-server localhost:9092
   ```

### 4. メッセージの送受信

#### プロデューサーの起動
1. 別のコマンドプロンプトを開き、Kafkaディレクトリに移動します。
   ```cmd
   cd C:\kafka
   ```

2. プロデューサーを起動します。
   ```cmd
   .\bin\windows\kafka-console-producer.bat --topic test-topic --bootstrap-server localhost:9092
   ```

3. メッセージを入力して送信します。
   ```
   Hello Kafka
   ```

#### コンシューマーの起動
1. 別のコマンドプロンプトを開き、Kafkaディレクトリに移動します。
   ```cmd
   cd C:\kafka
   ```

2. コンシューマーを起動します。
   ```cmd
   .\bin\windows\kafka-console-consumer.bat --topic test-topic --from-beginning --bootstrap-server localhost:9092
   ```

3. プロデューサーで送信したメッセージが表示されます。

### 5. Kafkaの停止
1. Zookeeperを停止します。
   ```cmd
   .\bin\windows\zookeeper-server-stop.bat
   ```

2. Kafkaブローカーを停止します。
   ```cmd
   .\bin\windows\kafka-server-stop.bat
   ```

## 注意事項
- Kafkaはデフォルトでポート`9092`を使用します。他のアプリケーションと競合しないように注意してください。
- Zookeeperはデフォルトでポート`2181`を使用します。
- Windows環境では、KafkaのパフォーマンスがLinuxに比べて劣る場合があります。本番環境ではLinuxを推奨します。

以上で、Windows 11でのKafka設定手順は完了です。