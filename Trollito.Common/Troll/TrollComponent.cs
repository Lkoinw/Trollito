using TaleWorlds.MountAndBlade;
using Trollito.Common.Extensions;
using TaleWorlds.Library;
using Logger = Trollito.Common.Utilities.Logger;

namespace Trollito.Common.Troll
{
    public class TrollComponent : AgentComponent
    {
        public TrollComponent(Agent agent) : base(agent)
        {
        }

        public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
        {
            bool trollAttack = affectorAgent.Name.ToLower().Contains("troll");


            if (trollAttack)
            {
                if (Agent.IsMount)
                {
                    Logger.Log("Mount health before : " + Agent.Health);
                    Agent.DealDamage(500, new Vec3(0, 0, 0), null, false, true);
                    Logger.Log("Mount health after : " + Agent.Health);
                }
            }
        }
    }
}