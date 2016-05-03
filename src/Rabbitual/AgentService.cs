using System;
using Rabbitual.Infrastructure;
using Rabbitual.Logging;

namespace Rabbitual
{
    public interface IAgentService
    {
        object GetState(IAgentWrapper a);
        object GetPersistedState(Type agent, string id);
    }


    public class AgentService : IAgentService
    {
        private readonly IObjectDb _db;
        private readonly ILogger _logger;

        public AgentService(
            IObjectDb db,
            ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public object GetPersistedState(Type agent, string id)
        {
            if (!agent.IsOfType(typeof(IStatefulAgent<>)))
                return null;

            var service = new AgentStateRepository(id, _db, _logger);
            return StateHelper.GetPersistedStateUsingMagic(service, agent);
        }

        public object GetState(IAgentWrapper a)
        {
            if (a == null || !a.HasState())
                return null;

            return a.GetState();
        }
    }
}