# ConsoleAppFrameworkSample

ConsoleAppFramework + Serilog  

```
Install-Package ConsoleAppFramework
Install-Package Serilog.Extensions.Logging
Install-Package Serilog.Extensions.Hosting
Install-Package Serilog.Settings.Configuration
Install-Package Serilog.Sinks.Console
Install-Package Serilog.Sinks.RollingFile
Install-Package Serilog.Enrichers.Environment
Install-Package Serilog.Enrichers.Process
Install-Package Serilog.Enrichers.Thread
```


## 背景
.NET core コンソールアプリを運用していますが  
いつのまにか機能を詰め込みすぎて、全体が把握できなくなり  
機能追加が困難な時期がやってきました。  
  
そこで、ConsoleAppFrameworkをつかってコードを整理しようと思います。  
機能ごとに別クラスにわけても、起動時引数で容易に制御ができるので便利です。  
  
ログなどは Microsoft.Extensions の仕組みを使うようです。  
残念ながら Microsoft.Extensions 関連は素人なので組み合わせ方法を探すのに苦労しました。  
ネットで情報漁って、なんとか形が見えてきたので、まとめておきます。  
  
## ConsoleAppFrameworkとは  
  
[ConsoleAppFramework - .NET Coreコンソールアプリ作成のためのマイクロフレームワーク（旧MicroBatchFramework)](http://neue.cc/2020/01/09_588.html "ConsoleAppFramework - .NET Coreコンソールアプリ作成のためのマイクロフレームワーク（旧MicroBatchFramework)")  
  
慣れてしまうと普通のコンソールアプリには戻れませんね。  
  
## 実装例  
まずは結論からということでProgram.csとConsoleAppBaseの実装です。  

[Program.cs](https://github.com/panda728/ConsoleAppFrameworkSample/blob/master/ConsoleAppFrameworkSampleApp/Program.cs)  
  
```c#
using System;  
using System.IO;  
using System.Threading.Tasks;  
using ConsoleAppFramework;  
using Microsoft.Extensions.Configuration;  
using Microsoft.Extensions.DependencyInjection;  
using Microsoft.Extensions.Hosting;  
using Serilog;  
  
namespace ConsoleAppFrameworkSampleApp {  
    public class Program {  
        static async Task Main(string[] args) {  
            Log.Logger = CreateLogger();  
            try {  
                await CreateHostBuilder(args).RunConsoleAppFrameworkAsync(args);  
            } catch (Exception ex) {  
                Log.Fatal(ex, "Host terminated unexpectedly");  
            } finally {  
                Log.CloseAndFlush();  
            }  
        }  
  
        private static IHostBuilder CreateHostBuilder(string[] args) =>  
            Host.CreateDefaultBuilder(args)  
                .UseSerilog()  
                .ConfigureServices((hostContext, services) => {  
                    services.Configure<Settings>(  
                        hostContext.Configuration.GetSection("Settings"));  
                });  
  
        private static ILogger CreateLogger() =>  
            new LoggerConfiguration()  
                .ReadFrom.Configuration(CreateBuilder().Build())  
                .Enrich.FromLogContext()  
                .WriteTo.Console()  
                .CreateLogger();  
          
        private static IConfigurationBuilder CreateBuilder() =>  
             new ConfigurationBuilder()  
                .SetBasePath(Directory.GetCurrentDirectory())  
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)  
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)  
                .AddEnvironmentVariables();  
    }  
}  
```  

各機能の実装は&nbsp;ConsoleAppBase&nbsp;を継承して実装します  
[Base1.cs](https://github.com/panda728/ConsoleAppFrameworkSample/blob/master/ConsoleAppFrameworkSampleApp/AppBase/Base1.cs)
```c#
    public class Base1 : ConsoleAppBase {  
        readonly ILogger logger;  
        readonly Settings settings;  
  
        public Base1(ILogger<Base1> logger, IOptions<Settings> settingsOp) {  
            this.logger = logger;  
            this.settings = settingsOp.Value;  
        }  
  
        public void Hello(  
       [Option("n", "name of send user.")]string name,  
       [Option("r", "repeat count.")]int repeat = 3) {  
            for (int i = 0; i < repeat; i++) {  
                this.logger.LogInformation("Hello My ConsoleApp Base1 from {name} {path}", name, this.settings.Path);  
            }  
        }  
    }  
```  
  
```console:exeを実行するときの引数で、実行するプログラムを指定します。  
ConsoleAppFrameworkSampleApp.exe Base1.Hello -t TEST  
```  
以上で全部です。  
以下で、各部を説明します。  
    
## Main部分  
mainでは、ロガーを作成してConsoleAppFrameworkを起動します。  
  
起動後は、ConsoleAppFrameworkが、引数に応じて適切なConsoleAppBaseクラスを実行してくれます。  
  
なおロガーの細かい設定は後述  
  
```c#  
static async Task Main(string[] args) {  
    Log.Logger = CreateLogger();  
    try {  
        await CreateHostBuilder(args).RunConsoleAppFrameworkAsync(args);  
    } catch (Exception ex) {  
        Log.Fatal(ex, "Host terminated unexpectedly");  
    } finally {  
        Log.CloseAndFlush();  
    }  
}  
```  
  
  
## ロガーの設定について  
ロガーの設定は appsettings.json にて行うスタイルにします。  
  
appsettings.jsonから設定を読み込む処理は Serilog.Extensions.Hosting のSampleの書き方を参考にしました。  
  
https://github.com/serilog/serilog-extensions-hosting/blob/dev/samples/SimpleServiceSample/Program.cs  
  
```c#  
private static ILogger CreateLogger() =>  
    new LoggerConfiguration()  
        .ReadFrom.Configuration(CreateBuilder().Build())  
        .Enrich.FromLogContext()  
        .WriteTo.Console()  
        .CreateLogger();  

private static IConfigurationBuilder CreateBuilder() =>  
     new ConfigurationBuilder()  
        .SetBasePath(Directory.GetCurrentDirectory())  
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)  
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)  
        .AddEnvironmentVariables();  
```  
  
余談ですが　appsettings.jsonについて。  
テスト環境で「appsettings.Develogment.json」  
本番環境で「appsettings.Production.json」  
を優先して読み込むとのこと  
  
## Microsoft.Extensions関連の設定について  
Host.CreateDefaultBuilderで設定している内容は  
・Serilogの登録  
・appsettings.jsonから設定情報を読み込む  
　（設定情報は以下のようなSettingクラスを用意するスタイルです）  
  
ここでservicesに設定したものは、後で作成するConsoleAppBaseのコンストラクタで受け取ることができます。  
機能が増えて共通的に使いたいものができたら、ここでいろいろ増やせばよさそうです。  
  
```c#  
private static IHostBuilder CreateHostBuilder(string[] args) =>  
    Host.CreateDefaultBuilder(args)  
        .UseSerilog()  
        .ConfigureServices((hostContext, services) => {  
            services.Configure<Settings>(  
                hostContext.Configuration.GetSection("Settings"));  
        });  
```  

設定用クラスの例（appsetting.jsonと連動させます。）

```c#
public class Settings {  
    public string Path { get; set; }  
    public string ConnectionString { get; set; }  
}  
```  
  
## ConsoleAppBaseの実装例  
上記のコードで準備が整いましたので  
実際の処理部分を記述します。  
  
ConsoleAppFrameworkのサンプルにあるHelloメソッド相当の機能に、ロガーと設定情報クラスを追加すると、以下の形になります。  
  
```c#  
public class Base1 : ConsoleAppBase {  
    readonly ILogger logger;  
    readonly Settings settings;  

    public Base1(ILogger<Base1> logger, IOptions<Settings> settingsOp) {  
        this.logger = logger;  
        this.settings = settingsOp.Value;  
    }  

    public void Hello(  
   [Option("n", "name of send user.")]string name,  
   [Option("r", "repeat count.")]int repeat = 3) {  
        for (int i = 0; i < repeat; i++) {  
            this.logger.LogInformation("Hello My ConsoleApp Base1 from {name} {path}", name, this.settings.Path);  
        }  
    }  
}  
```  
  
コンストラクタでロガーと設定情報クラスを受け取ることができますが  
この辺りはMicrosoft.Extensionsのお仕事  
  
DIがよくわからなかったのですが  
　「コンストラクタで書いたものが勝手にセットされてくる」  
と思ったら、便利さがわかったつもりになりましたw  
  
## appsetting.jsonの例  
設定クラスとSerilogの設定は以下の要領で  

[appsetting.json](https://github.com/panda728/ConsoleAppFrameworkSample/blob/master/ConsoleAppFrameworkSampleApp/appsettings.json)
```json  
{  
  "Settings": {  
    "Path": "c:\\temp",  
    "ConnectionString": "(connection string)"  
  },  
  "Serilog": {  
    "Using": [  
      "Serilog.Sinks.Console",  
      "Serilog.Sinks.RollingFile",  
    ],  
    "MinimumLevel": "Verbose",  
    "WriteTo": [  
      {  
        "Name": "RollingFile",  
        "Args": {  
          "pathFormat": "logs\\log-{Date}.txt",  
          "retainedFileCountLimit": "30"  
        }  
      },  
    ],  
    "Enrich": [ "WithMachineName", "WithProcessId", "WithThreadId" ],  
    "Destructure": [  
      {  
        "Name": "ToMaximumDepth",  
        "Args": { "maximumDestructuringDepth": 4 }  
      },  
      {  
        "Name": "ToMaximumStringLength",  
        "Args": { "maximumStringLength": 100 }  
      },  
      {  
        "Name": "ToMaximumCollectionCount",  
        "Args": { "maximumCollectionCount": 10 }  
      }  
    ]  
  }  
}  
```  
  
## 最後に  
引数で処理を分岐する部分はConsoleAppFrameworkが引き受けてくれますので、Program.csはシンプルに保てます。  
  
これで、これまで苦労していた、  
　・巨大クラスで見通しが悪く、影響範囲を考慮しながら慎重に機能追加  
　・いつの間にか似たような処理を複数作っていた  
といった時代とはおさらばできます。  
  
新機能を追加したい場合は、新規にConsoleAppBaseを追加すれば、  
従来の機能への影響を気にせず追加でき、共通機能はDIから受け取って使いまわせます。 
  
クラスが増えても、コンストラクタを見れば  
共通利用で使っているクラスが把握できるので安心です。
同じ処理のコードが複数クラスに書いてある事態は、DIで回避できるでしょう。   

###余談  
appsettings.jsonの実行時パス問題が発生した場合は以下参照  
  
How can I get my .NET Core 3 single file app to find the appsettings.json file?  
https://stackoverflow.com/questions/58307558/how-can-i-get-my-net-core-3-single-file-app-to-find-the-appsettings-json-file  
  
  
```C#  
config.SetBasePath(GetBasePath());  
config.AddJsonFile("appsettings.json", false);  
```  
  
  
```C#  
private string GetBasePath()  
{  
    using var processModule = Process.GetCurrentProcess().MainModule;  
    return Path.GetDirectoryName(processModule?.FileName);  
}  
```  
  
