using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Trollito.Common.Utilities;
using Logger = Trollito.Common.Utilities.Logger;

namespace Trollito.Common.Extensions
{
    public static class AgentExtensions
    {
        /// <summary>
        /// Apply damage to an agent. 
        /// </summary>
        /// <param name="agent">The agent that will be damaged</param>
        /// <param name="damageAmount">How much damage the agent will receive.</param>
        /// <param name="damager">The agent who is applying the damage</param>
        /// <param name="doBlow">A mask that controls whether the unit receives a blow or direct health manipulation</param>
        public static void DealDamage(this Agent agent, int damageAmount, Vec3 impactPosition, Agent damager = null, bool doBlow = true, bool hasShockWave = false)
        {
            //if (!GameNetwork.IsServer)
            //{
            //    return;
            //}
            if (agent == null || !agent.IsActive() || agent.Health < 1)
            {
                Logger.Log("ApplyDamage: attempted to apply damage to a null or dead agent.", LogLevel.Warning);
                return;
            }
            try
            {
                // Registering a blow causes the agent to react/stagger. Manipulate health directly if the damage won't kill the agent.
                if (agent.State == AgentState.Active || agent.State == AgentState.Routed)
                {
                    if (!doBlow && agent.Health > damageAmount)
                    {
                        agent.Health -= damageAmount;
                        return;
                    }

                    if (agent.IsFadingOut())
                        return;

                    var damagerAgent = damager != null ? damager : agent;

                    var blow = new Blow(damagerAgent.Index);
                    blow.DamageType = DamageTypes.Blunt;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.Position = agent.GetChestGlobalPosition();
                    blow.BaseMagnitude = damageAmount;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.InflictedDamage = damageAmount;
                    var direction = agent.Position == impactPosition ? agent.LookDirection : agent.Position - impactPosition;
                    direction.Normalize();
                    blow.Direction = direction;
                    blow.SwingDirection = direction;
                    blow.DamageCalculated = true;
                    blow.AttackType = AgentAttackType.Kick;
                    blow.BlowFlag = BlowFlags.NoSound;
                    blow.VictimBodyPart = BoneBodyPartType.Chest;
                    blow.StrikeType = StrikeType.Thrust;
                    if (hasShockWave)
                    {
                        if (agent.HasMount) blow.BlowFlag |= BlowFlags.CanDismount;
                        else blow.BlowFlag |= BlowFlags.KnockDown;
                    }

                    if (agent.Health <= damageAmount && !doBlow)
                    {
                        agent.Die(blow);
                        return;
                    }
                    sbyte mainHandItemBoneIndex = damagerAgent.Monster.MainHandItemBoneIndex;
                    AttackCollisionData attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                        false,
                        false,
                        false,
                        true,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        CombatCollisionResult.StrikeAgent,
                        -1,
                        1,
                        2,
                        blow.BoneIndex,
                        blow.VictimBodyPart,
                        mainHandItemBoneIndex,
                        Agent.UsageDirection.AttackUp,
                        -1,
                        CombatHitResultFlags.NormalHit,
                        0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                        Vec3.Up,
                        blow.Direction,
                        blow.Position,
                        Vec3.Zero,
                        Vec3.Zero,
                        agent.Velocity,
                        Vec3.Up);
                    agent.RegisterBlow(blow, attackCollisionData);
                }
            }
            catch (Exception e)
            {
                Logger.Log("ApplyDamage: attempted to damage agent, but: " + e.Message, LogLevel.Error);
            }
        }
    }
}
