using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;

namespace TU20Bot.Configuration.Controllers {
    public class CommitController : ServerController {
        [Route(HttpVerbs.Put, "/commit")]
        public void commitConfig() {
            Config.save(server.config);
        }
    }
}