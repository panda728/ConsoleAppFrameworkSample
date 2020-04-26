using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleAppFrameworkSampleApp.AppBase {
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
}
