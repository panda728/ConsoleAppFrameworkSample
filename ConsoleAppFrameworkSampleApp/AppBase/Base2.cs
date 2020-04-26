using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleAppFrameworkSampleApp.AppBase {
    public class Base2 : ConsoleAppBase {
        readonly ILogger logger;
        readonly Settings settings;

        public Base2(ILogger<Base2> logger, IOptions<Settings> settingsOp) {
            this.logger = logger;
            this.settings = settingsOp.Value;
        }

        public void Hello(
       [Option("n", "name of send user.")]string name,
       [Option("r", "repeat count.")]int repeat = 3) {
            for (int i = 0; i < repeat; i++) {
                this.logger.LogWarning("Hello My ConsoleApp Base2 from {name} {connectionstring}", name, this.settings.ConnectionString);
            }
        }
    }
}
