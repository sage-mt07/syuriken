# Examplesの動作手順

このドキュメントでは、`source/Examples`ディレクトリ内のサンプルコードを動作させるための手順を説明します。

## 前提条件
1. **Kafkaのセットアップ**
   - [kafka-setup-windows11.md](./kafka-setup-windows11.md)を参照して、Kafkaをセットアップしてください。

2. **ksqlDBのセットアップ**
   - [ksqldb-usage-guide.md](./ksqldb-usage-guide.md)を参照して、ksqlDBをセットアップしてください。

3. **必要なツールのインストール**
   - .NET SDK（バージョン6.0以上）をインストールしてください。
   - [公式サイト](https://dotnet.microsoft.com/download)からダウンロードできます。

4. **依存パッケージの復元**
   - プロジェクトのルートディレクトリで以下のコマンドを実行して、依存パッケージを復元します。
     ```cmd
     dotnet restore
     ```

## サンプルコードの実行手順

### 1. KafkaとksqlDBの起動
- KafkaとksqlDBが起動していることを確認してください。
- 必要に応じて、KafkaトピックやksqlDBストリームを作成してください。

### 2. サンプルコードのビルド
1. `source/Examples`ディレクトリに移動します。
   ```cmd
   cd source/Examples
   ```
2. 以下のコマンドを実行してプロジェクトをビルドします。
   ```cmd
   dotnet build
   ```

### 3. サンプルコードの実行
1. サンプルコードを実行するには、以下のコマンドを使用します。
   ```cmd
   dotnet run
   ```
2. 実行中に必要なKafkaトピックやksqlDBストリームが存在しない場合は、エラーメッセージが表示されることがあります。その場合は、エラーメッセージに従ってリソースを作成してください。

### 4. 動作確認
- サンプルコードが正しく動作している場合、コンソールに出力が表示されます。
- KafkaトピックやksqlDBストリームのデータを確認するには、KafkaコンシューマーやksqlDB CLIを使用してください。

## 注意事項
- KafkaとksqlDBが正しくセットアップされていない場合、サンプルコードは動作しません。
- 必要に応じて、`OrderContext`や`Models`ディレクトリ内の設定を確認し、環境に合わせて調整してください。

以上で、Examplesの動作手順の説明は完了です。