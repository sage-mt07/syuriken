# コード生成までのステップ



![指示](image-2.png)

![プロンプト](image-3.png)
クラス単位で確認される。

Yes,and don't ask againで全体のコード生成開始

全体のコード生成まで5分程度
![alt text](image-4.png)
作成ファイル

![alt text](image-6.png)

![alt text](image-7.png)    
コード内では参照エラーがある。
![alt text](image-8.png)    
ちょっと修正
![alt text](image-9.png)    

![alt text](image-10.png)   
ビルド結果

![alt text](image-11.png)   

![alt text](image-12.png)   

改行がうまくいっていない
![alt text](image-13.png)   
修正後、エラーが存在する
![alt text](image-14.png)   
修正指示
![alt text](image-15.png)   
Consoleでエラー発生

![alt text](image-16.png)   
再コンパイルでエラー
![alt text](image-17.png)   
再修正依頼後
![alt text](image-18.png)   
Exampleコードが対応していないので修正依頼
![alt text](image-19.png)   

作成したファイルとサイズ
![alt text](image-21.png)   
ワーニングは残るが、ビルド完了
![alt text](image-22.png)   
ここまで30分
事前作業として要件定義書をClaudeと一緒に作成

その際に、言語仕様の確認、EntityFrameworkはRDBを対象としているため、差異のまとめ方を検討し、要件定義書にまとめることを実施

ソースを確認し、動作するよう以下の指示をおこなう

Confluent.Kafkaパッケージを追加した動作するよう修正
![alt text](image-23.png)   
public methodに対してunit testを追加

![alt text](image-24.png)   
exit時に課金情報が見れる
![alt text](image-25.png)   

