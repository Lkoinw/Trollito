using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Trollito.Common.TestStuff
{
    public class TestBonk : MissionLogic
    {
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (affectorAgent.Name.ToLower().Contains("troll"))
            {
                // Calculate the direction of the force
                Vec3 forceDirection = affectedAgent.Position - affectorAgent.Position;
                forceDirection.Normalize();

                // Apply force to the affected agent
                float forceMultiplier = 1000000f; // Adjust this value to control the strength of the force
                SendFlying(affectedAgent, affectorAgent, forceDirection, forceMultiplier);
            }
        }

        public void SendFlying(Agent affectedAgent, Agent affectorAgent, Vec3 forceDirection, float forceMultiplier)
        {
            if (affectedAgent.State == AgentState.Active || affectedAgent.State == AgentState.Routed)
            {
                if (affectedAgent.IsFadingOut())
                    return;

                var blow = new Blow(affectorAgent.Index);
                blow.DamageType = DamageTypes.Blunt;
                blow.BoneIndex = affectedAgent.Monster.HeadLookDirectionBoneIndex;
                blow.Position = affectedAgent.GetChestGlobalPosition();
                blow.BaseMagnitude = forceMultiplier; // No additional damage
                blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                blow.Direction = forceDirection;
                blow.SwingDirection = forceDirection;
                blow.DamageCalculated = true;
                blow.AttackType = AgentAttackType.Kick;
                blow.BlowFlag = BlowFlags.KnockDown | BlowFlags.KnockBack | BlowFlags.NoSound;
                blow.VictimBodyPart = BoneBodyPartType.Chest;
                blow.StrikeType = StrikeType.Thrust;

                // Create AttackCollisionData
                AttackCollisionData attackCollisionDataForDebugPurpose =
                    AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                        false, false, false, true, false, false, false, false, false, false, false, false,
                        CombatCollisionResult.StrikeAgent,
                        -1, 1, 2,
                        blow.BoneIndex,
                        blow.VictimBodyPart,
                        affectorAgent.Monster.MainHandItemBoneIndex,
                        Agent.UsageDirection.AttackUp,
                        -1,
                        CombatHitResultFlags.NormalHit,
                        0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                        Vec3.Up, blow.Direction, blow.Position, Vec3.Zero, Vec3.Zero,
                        affectedAgent.Velocity, Vec3.Up);

                // Register the blow to knock down the agent
                affectedAgent.RegisterBlow(blow, attackCollisionDataForDebugPurpose);

                // Calculate the velocity to apply
                Vec3 velocity = forceDirection.NormalizedCopy() * forceMultiplier;

                // Apply the velocity to the agent
                affectedAgent.SetMaximumSpeedLimit(velocity.Length, true);
                affectedAgent.MovementInputVector = new Vec2(velocity.x, velocity.z);
            }
        }
    }
}
