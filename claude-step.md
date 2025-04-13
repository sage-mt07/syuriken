# コード生成までのステップ

![指示](images/image-2.png)

![プロンプト](images/image-3.png)
クラス単位で確認される。

Yes,and don't ask againで全体のコード生成開始

全体のコード生成まで5分程度
![alt text](images/image-4.png)
作成ファイル

![alt text](images/image-6.png)

![alt text](images/image-7.png)    
コード内では参照エラーがある。
![alt text](images/image-8.png)    
ちょっと修正
![alt text](images/image-9.png)    

![alt text](images/image-10.png)   
ビルド結果

![alt text](images/image-11.png)   

![alt text](images/image-12.png)   

改行がうまくいっていない
![alt text](images/image-13.png)   
修正後、エラーが存在する
![alt text](images/image-14.png)   
修正指示
![alt text](images/image-15.png)   
Consoleでエラー発生

![alt text](images/image-16.png)   
再コンパイルでエラー
![alt text](images/image-17.png)   
再修正依頼後
![alt text](images/image-18.png)   
Exampleコードが対応していないので修正依頼
![alt text](images/image-19.png)   

作成したファイルとサイズ
![alt text](images/image-21.png)   
ワーニングは残るが、ビルド完了
![alt text](images/image-22.png)   
ここまで30分
事前作業として要件定義書をClaudeと一緒に作成

その際に、言語仕様の確認、EntityFrameworkはRDBを対象としているため、差異のまとめ方を検討し、要件定義書にまとめることを実施

ソースを確認し、動作するよう以下の指示をおこなう

Confluent.Kafkaパッケージを追加した動作するよう修正
![alt text](images/image-23.png)   
public methodに対してunit testを追加

![alt text](images/image-24.png)   
exit時に課金情報が見れる
![alt text](images/image-25.png)

