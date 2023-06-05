using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Trollito.Common.Troll
{
    public class TrollBehavior : MissionLogic
    {
        struct ShockwaveEntity
        {
            public GameEntity Shockwave;
            public Vec3 PushVector;
            public float LifeTime;
        }

        private Dictionary<Agent, GameEntity> trolls = new Dictionary<Agent, GameEntity>();
        private List<ShockwaveEntity> shockwaves = new List<ShockwaveEntity>();

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.AddComponent(new TrollComponent(agent));
        }
    }
}
