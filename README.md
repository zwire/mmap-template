# mmap-template

[![Gitpod ready-to-code](https://img.shields.io/badge/Gitpod-ready--to--code-blue?logo=gitpod)](https://gitpod.io/#https://github.com/husty530/mmap-template)

#### mmapとは
* mmapはディスク上にあるファイルを仮想メモリにマッピングして読み書きする技術です。  
* ファイルを開いたまま複数のプロセスが同時にアクセスすることができます。  
* 普通のメモリ上処理よりは遅いようですが，ネットワーク使うよりは圧倒的に高速です。  
* 排他制御が必要です。  

### 解説
* MappedMemoryFile(通称・共有メモリ)を使ってプロセス間通信を行うクラスとその実行サンプルです。  
* Python(画像・点群処理)とC++(ロボット)を連携させたいときなんかに使えそうですね。  
* [Python](/py), [C++](/cpp), [C#](/cs)それぞれで実装しました。Pythonが送信側でC++/C#が受信側です。  
* Windows, Linuxで動かせます。  
* テストはかなり適当で，映像を送ったのですがOpenCVのビルドがめんどくさかったので画像表示はしてません。  
* 排他制御に関して，本リポジトリでは簡易なブロッキング処理とメモリプールの確保により安全性と高速性を両取りする作戦をとりました。  
* プールサイズを1にするとただのmutexが効いた状態になります。2以上にすると一番古いバッファーがドロップするように書き込まれ，読み出しは最新のものから行うようになります。FIFOとはちょっと違うと思います。  
* 読み書きの同時アクセスが起きた場合は後から入るやつが失敗してfalseを返します。待機したりタイムアウトを設定したりなどは自分で実装してください。
* いずれかを実行するとshared.poolというファイルが生成されます。起動順は送り側・受け側どちらからでもよいですが，約束としてバッファー容量とプールサイズを合わせておく必要があります。  
* 複数種類のデータを送りたいときや双方向でやりとりしたい場合はその数だけインスタンスを作ってください。  
* 重くないデータはソケットかパイプで流しましょう。  

### ミニ実験：処理時間の目安
* フルHD(1920x1080)映像配信  
* Python (送信: 2 ms) -> C# (受信: 0.5 ms)  
* エンコード/デコードに数十ミリ秒かかるので，画像バッファーそのまま貼った方が高効率。  
* TCP/UDPソケットはパケットサイズの上限が65535バイトなので分割する必要アリ。すなわち往復が多くなるため大きなデータ転送方法としては論外。  