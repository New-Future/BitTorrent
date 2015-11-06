BitTorrent for NKU
==========
简单轻量级的bt客户端，

调试说明，用VisualStudio 2015打开解决方案 Torrent.Client/BitTorrent.sln

### 架构
* 基础框架采用.net的MVVM(Model View ViewModel)框架,引用MvvmLight支持。程序GUI采用WPF构建。整个项目分为两部分Client和GUI。
* Client作为Torrent的客户端核心程序打包成Client.dll。
* GUI提供图形化界面，调用Client程序。


### 主要目录结构
```
├── Torrent.Client/        客户端核心项目目录
|   ├── BitTorrent.sln      项目解决方案管理文件(双击打开)
|   ├── Bencoding/                Becoding编码封装目录
|   │   ├── ...         
|   |   └── BencodingParser.cs    编码解析类
|   ├── Messages/          客户端直接详细类型封装目录
|   │   └─ ...         
|   ├── DownloadMode.cs           文件下载Model
|   ├── TorrentData.cs            Torrent数据对象类
|   ├── Global.cs                 全局配置
|   ├── TorrentMode.cs            Torrent的Mode对象类
|   ├── HashingMode.cs            哈希计算的Mode对象类
|   ├── SeedMode.cs               做种Model对象类
|   ├── TorrentTransfer.cs        Torrent数据传输对象类
|   ├── TrackerClient.cs          与Trackr服务器连接的对象
|   └── ...
|
└── Torrent.GUI/           图形化界面项目目录
    │── MainWindow.xaml         主窗口设计文件
    ├── MainViewModel.cs        主窗口ViewModel
    └── TransferViewModel.cs    控制模块ViewModel  
```
