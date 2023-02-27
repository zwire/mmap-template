# mmap-template

[![Gitpod ready-to-code](https://img.shields.io/badge/Gitpod-ready--to--code-blue?logo=gitpod)](https://gitpod.io/#https://github.com/husty530/mmap-template)  
#### How to Run in Gitpod Terminal
* Python
```
cd /workspace/mmap-template/py && python main.py
```
* C++ 
```
cd /workspace/mmap-template/cpp && ./main
```
* C#
```
cd /workspace/mmap-template/cs/bin/Debug/net6.0 && ./cs
```

(Translation follows)  

### mmapとは
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
* ファイルが既に存在する状態で開始すると前回のプロセスの残骸が残っています。キャッシュをクリアするためにflush関数を用意していますので，各セッションで最初に立ち上がるプロセスではなるべくflushを入れることを推奨します。  
* 複数種類のデータを送りたいときや双方向でやりとりしたい場合はその数だけインスタンスを作ってください。  
* 重くないデータはソケットかパイプで流しましょう。  

### ミニ実験：処理時間の目安
* フルHD(1920x1080)映像配信  
* Python (送信: 2 ms) -> C# (受信: 0.5 ms)  
* エンコード/デコードに数十ミリ秒かかるので，画像バッファーそのまま貼った方が高効率。  
* TCP/UDPソケットはパケットサイズの上限が65535バイトなので分割する必要アリ。すなわち往復が多くなるため大きなデータ転送方法としては論外。  

### What is mmap?
* mmap is a technology that maps files on disk to virtual memory for reading and writing.  
* Multiple processes can access the file at the same time while it is open.  
* It may be slower than normal in-memory processing, but it is much faster than network processing.  
* Exclusion control is required.  

### Description
* A class that uses MappedMemoryFile (a.k.a. shared memory) for inter-process communication and a sample of its execution.  
* This class can be used when you want to link Python (image/point cloud processing) and C++ (robotics).  
* [Python](/py) is the sender and [C++](/cpp) / [C#](/cs) is the receiver.  
* The test is quite random. I sent a video, but did not display the image because it was too much trouble to build OpenCV.  
* For exclusion control, this repository uses a simple blocking process and memory pooling to be both safe and fast.  
* This is a little different from a FIFO.  
* If simultaneous read/write accesses occur, the later one will fail and return false. You can implement your own wait, timeout, etc.  
* A file named shared.pool is generated when either of the above is executed. The startup order can be either from the sender or receiver side, but the buffer capacity and pool size must be the same as promised.  
* If the process is started when the file already exists, the remnants of the previous process will remain. Since a flush function is provided to clear the cache, it is recommended that flush be inserted in the first process launched in each session as much as possible.  
* If you want to send multiple types of data or exchange data in both directions, create as many instances as you need.  
* Use sockets or pipes for non-heavy data.  

### Mini-experiment: Approximate processing time
* Full HD (1920x1080) video streaming  
* Python (sending: 2 ms) -> C# (receiving: 0.5 ms)  
* Encoding/decoding takes several tens of milliseconds, so it is more efficient to put the image buffer as it is.  
* TCP/UDP sockets have an upper limit of 65535 bytes for packet size, so it is necessary to split the packets. This means that it is out of the question as a large data transfer method because of the large number of round-trips.  
