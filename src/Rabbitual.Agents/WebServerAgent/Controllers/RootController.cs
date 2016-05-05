using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using NJsonSchema;
using Rabbitual.Configuration;
using Rabbitual.Infrastructure;
using Rabbitual.Logging;

namespace Rabbitual.Agents.WebServerAgent.Controllers
{
    public class RootController : ApiController
    {
        private readonly IAgentConfiguration _cfg;
        private readonly IAgentPool _ar;
        private readonly IAgentService _s;
        private readonly IAgentLogRepository _l;
        private readonly IAgentConfiguration _configRepository;
        private readonly IJsonSerializer _serializer;

        public RootController(
            IAgentConfiguration cfg,
            IAgentPool ar,
            IAgentService s,
            IAgentLogRepository l,
            IAgentConfiguration configRepository,
            IJsonSerializer serializer)
        {
            _cfg = cfg;
            _ar = ar;
            _s = s;
            _l = l;
            _configRepository = configRepository;
            _serializer = serializer;
        }

        [HttpGet]
        [Route("agent/options/schema/{id}")]
        public HttpResponseMessage Schema(string id)
        {
            var cfg = _cfg.GetConfiguration().FirstOrDefault(x => x.Id == id);
            if (cfg == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var schema = JsonSchema4.FromType(cfg.Options.GetType());

            return this.FromRawJson(schema.ToJson(), "application/schema+json");
        }


        [Route("agent/options/update/{id}")]
        public HttpResponseMessage Update(string id)
        {
            var json = Request.Content.ReadAsStringAsync().Result;
            var cfg = _cfg.GetConfiguration().FirstOrDefault(x => x.Id == id);
            if (cfg == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var newOptions = _serializer.Deserialize(json, cfg.Options.GetType());
            cfg.Options = newOptions;
            _configRepository.PersistConfig(cfg.ToDto());

            return new HttpResponseMessage(HttpStatusCode.OK);
        }


        [HttpGet]
        [Route("agent/options/{id}")]
        public HttpResponseMessage Options(string id)
        {
            var cfg = _cfg.GetConfiguration().FirstOrDefault(x => x.Id==id);
            if (cfg == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return this.SweetJson(cfg.Options, bigAssPropertyNames: true, keepNulls:true);
        }

        [HttpGet]
        [Route("agent/message-log/{id}")]
        public HttpResponseMessage MessageLog(string id)
        {
            var al = _l.GetLog(id);

            return this.SweetJson(new
            {
                Incoming = al.GetIncoming(),
                Outgoing = al.GetOutGoing()
            });
        }

        [Route("agent/state/{id}")]
        public HttpResponseMessage Get(string id)
        {
            var agent = _ar.GetAgent(id);
            var state = _s.GetState(agent);

            return this.SweetJson(state);
        }

        [Route("config")]
        public HttpResponseMessage Get()
        {
            var root = Request.RequestUri.GetLeftPart(UriPartial.Authority);

            var o = _cfg
                .GetConfiguration()
                .GroupBy(x => x.ClrType)
                .SelectMany(g => g.OrderBy(x => x.Name))
                .Select(x =>
                {
                    var al = _l.GetLog(x.Id).GetSummary();
                    var attr = x.ClrType.GetCustomAttributes(typeof(IconAttribute), true).FirstOrDefault() as IconAttribute;

                    return new
                    {
                        Icon = attr==null ?  "hashtag": attr.FontAwesome,
                        OutgoingCount = al.OutgoingCount,
                        IncomingCount = al.IncomingCount,
                        LastCheck = al.LastCheck == null ? null : new DateTime?(al.LastCheck.Occured),
                        LastEventIn = al.LastEventIn == null ? null : new DateTime?(al.LastEventIn.Occured),
                        LastEventOut = al.LastEventOut == null ? null : new DateTime?(al.LastEventOut.Occured),
                        LastTaskIn = al.LastTaskIn == null ? null : new DateTime?(al.LastTaskIn.Occured),
                        LastTaskOut = al.LastTaskOut == null ? null : new DateTime?(al.LastTaskOut.Occured),
                        MessageLogUrl = $"{root}/agent/message-log/{x.Id}",
                        StateUrl = $"{root}/agent/state/{x.Id}",
                        OptionsSchemaUrl = $"{root}/agent/options/schema?id={x.Id}",
                        x.Id,
                        x.Name,
                        Sources = x.SourceIds,
                        Options = x.Options
                    };
                });

            return this.SweetJson(o);
        }

    }

}