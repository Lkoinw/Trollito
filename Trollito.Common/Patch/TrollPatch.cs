using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using Trollito.Common.Extensions;
using static TaleWorlds.MountAndBlade.Mission;
using Logger = Trollito.Common.Utilities.Logger;

namespace Trollito.Common.Patch
{
    /// <summary>
    /// Combat related patches for the troll.
    /// Credits to Xorberax for original Cut Through Everyone mod.
    /// </summary>
    public static class TrollPatch
    {
        private static readonly Harmony Harmony = new Harmony(nameof(TrollPatch));

        private static bool _patched;
        private static Dictionary<Agent, float> lastProjectionTimes = new Dictionary<Agent, float>();
        private const float MinProjectionDelay = 0.20f; // Minimum delay between consecutive projections (in seconds)
        private static ActionIndexCache fallbackAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise");
        const float gravityMagnitude = 9.8f; // Adjust the gravity magnitude as needed
        const float carpetWidth = 1.5f; // Adjust the width of the flying carpet as needed
        const float planeHeightOffset = 0.5f; // Adjust the height of the flying carpet as needed
        const float planeVectorOffset = -0.5f; // Adjust the offset to spawn the carpet in front of the agent as needed
        
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "DecideWeaponCollisionReaction"),
                    postfix: new HarmonyMethod(typeof(TrollPatch), nameof(DecideWeaponCollisionReactionPostfix))
                );

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "MeleeHitCallback"),
                    postfix: new HarmonyMethod(typeof(TrollPatch), nameof(MeleeHitCallbackPostfix))
                );

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "GetAttackCollisionResults"),
                    postfix: new HarmonyMethod(typeof(TrollPatch), nameof(GetAttackCollisionResultsPostfix))
                );

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "HandleMissileCollisionReaction"),
                    prefix: new HarmonyMethod(typeof(TrollPatch), nameof(HandleMissileCollisionReactionPrefix))
                );

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "MissileHitCallback"),
                    prefix: new HarmonyMethod(typeof(TrollPatch), nameof(MissileHitCallbackPrefix))
                );

                return true;
            }
            catch (Exception e)
            {
                Debug.Print("Error in TrollPatch: " + e.ToString(), 0, Debug.DebugColor.Red);
                return false;
            }
        }

        // Make arrow bounce
        public static void MissileHitCallbackPrefix(ref int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, Agent attacker, Agent victim, GameEntity hitEntity)
        {
            if (victim != null && victim.Name.ToLower().Contains("troll"))
            {
                bool backAttack = IsBackAttack(attacker, victim);
                bool attackBounced = false;
                List<BoneBodyPartType> strongParts = new List<BoneBodyPartType>() {
                    BoneBodyPartType.Head,
                    BoneBodyPartType.ShoulderLeft,
                    BoneBodyPartType.ShoulderRight,
                    BoneBodyPartType.ArmLeft,
                    BoneBodyPartType.ArmRight,
                    BoneBodyPartType.Legs
                };

                if(backAttack || strongParts.Contains(collisionData.VictimHitBodyPart))
                {
                    if(!(MBRandom.RandomInt(1, 10) == 1))
                    {
                        attackBounced = true;
                    }
                }

                if (attackBounced)
                {
                    int physicsMaterialIndex = PhysicsMaterial.GetFromName("stone").Index;
                    collisionData = SetAttackCollisionData(collisionData, false, false, false, true, physicsMaterialIndex, true, CombatCollisionResult.None);

                    Current.MakeSoundOnlyOnRelatedPeer(CombatSoundContainer.SoundCodeMissionCombatThrowingStoneMed, collisionData.CollisionGlobalPosition, attacker.Index);

                    SoundEventParameter soundEventParameter = new SoundEventParameter("Force", 1f);
                    Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeDefault, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index, ref soundEventParameter);
                } 
            }
        }

        // Make arrow bounce
        public static void HandleMissileCollisionReactionPrefix(Mission __instance, ref int missileIndex, ref MissileCollisionReaction collisionReaction, ref MatrixFrame attachLocalFrame, ref bool isAttachedFrameLocal, ref Agent attackerAgent, ref Agent attachedAgent, ref bool attachedToShield, ref sbyte attachedBoneIndex, ref MissionObject attachedMissionObject, ref Vec3 bounceBackVelocity, ref Vec3 bounceBackAngularVelocity, ref int forcedSpawnIndex)
        {
            //Vec3 zero = Vec3.Zero;
            //Vec3 zero2 = Vec3.Zero;
            //Missile missile = __instance._missiles[missileIndex];
            //missile.CalculateBounceBackVelocity(missileAngularVelocity, collisionData, out zero, out zero2);
            // Determine victim look direction
            //if (attachedAgent != null)
            //{
            //    Vec3 victimLookDirection = attachedAgent.GetMovementDirection().ToVec3();

            //    // Calculate the vector from the victim to the attacker
            //    Vec3 victimToAttacker = (attackerAgent.Position - attachedAgent.Position).NormalizedCopy();

            //    float angleInRadians = (float)Math.Atan2(victimLookDirection.x * victimToAttacker.y - victimLookDirection.y * victimToAttacker.x, victimLookDirection.x * victimToAttacker.x + victimLookDirection.y * victimToAttacker.y);
            //    float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));

            //    string position = string.Empty;

            //    float stanceOffset = attachedAgent.GetIsLeftStance() ? 25f : -25f;

            //    bool backAttack = false;

            //    if (angleInDegrees < -(90 - stanceOffset) && angleInDegrees > -(180 - stanceOffset)
            //        || angleInDegrees > (180 + stanceOffset))
            //    {
            //        position = "Back Right";
            //        backAttack = true;
            //    }
            //    else if (angleInDegrees < (0 + stanceOffset))
            //    {
            //        position = "Front Right";
            //    }
            //    else if (angleInDegrees < (90 + stanceOffset))
            //    {
            //        position = "Front Left";
            //    }
            //    else if (angleInDegrees > (90 + stanceOffset) || angleInDegrees < -(180 - stanceOffset))
            //    {
            //        position = "Back Left";
            //        backAttack = true;
            //    }

            //    if (backAttack)
            //    {
            //        collisionReaction = MissileCollisionReaction.BecomeInvisible;
            //        forcedSpawnIndex = -1;
            //        isAttachedFrameLocal = false;
            //        attachedAgent = null;
            //    }
            //}        
        }        

        public static void GetAttackCollisionResultsPostfix(ref CombatLogData __result, Agent attackerAgent, Agent victimAgent, GameEntity hitObject, float momentumRemaining, in MissionWeapon attackerWeapon, bool crushedThrough, bool cancelDamage, bool crushedThroughWithoutAgentCollision, ref AttackCollisionData attackCollisionData, ref WeaponComponentData shieldOnBack, ref CombatLogData combatLog)
        {
            if (victimAgent != null && victimAgent.Name.ToLower().Contains("troll"))
            {
                bool backAttack = IsBackAttack(attackerAgent, victimAgent);
                bool criticalHit = false;
                List<BoneBodyPartType> strongParts = new List<BoneBodyPartType>() {
                    BoneBodyPartType.Head,
                    BoneBodyPartType.ShoulderLeft,
                    BoneBodyPartType.ShoulderRight,
                    BoneBodyPartType.ArmLeft,
                    BoneBodyPartType.ArmRight,
                    BoneBodyPartType.Legs
                };

                if (backAttack || strongParts.Contains(attackCollisionData.VictimHitBodyPart))
                {
                    if ((MBRandom.RandomInt(1, 10) == 1))
                    {
                        criticalHit = true;
                    }
                }

                if (backAttack)
                {
                    if(criticalHit)
                    {
                        Logger.Log("Critical damage to the back");
                        attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 0.25f);
                    } else
                    {
                        Logger.Log("Couldn't pierce troll back...");
                        attackCollisionData.InflictedDamage = 0;
                    }                    
                }
                else
                {
                    switch (attackCollisionData.VictimHitBodyPart)
                    {
                        case BoneBodyPartType.Head:
                            if (criticalHit)
                            {
                                Logger.Log("Critical damage to the head");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 5f);
                            }
                            else
                            {
                                Logger.Log("Low damage to the head");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 0.1f);
                            }                            
                            break;
                        case BoneBodyPartType.Neck:
                            Logger.Log("Critical damage to the neck");
                            attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 3f);
                            break;
                        case BoneBodyPartType.ShoulderLeft:
                        case BoneBodyPartType.ShoulderRight:
                            if (criticalHit)
                            {
                                Logger.Log("Critical damage to the shoulder");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
                            }
                            else
                            {
                                Logger.Log("Low damage to the shoulder");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 0.1f);
                            }
                            break;
                        case BoneBodyPartType.ArmLeft:
                        case BoneBodyPartType.ArmRight:
                            if (criticalHit)
                            {
                                Logger.Log("Critical damage to the arm");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
                            }
                            else
                            {
                                Logger.Log("Low damage to the arm");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 0.4f);
                            }
                            break;
                        case BoneBodyPartType.Legs:
                            if (criticalHit)
                            {
                                Logger.Log("Critical damage to the leg");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
                            }
                            else
                            {
                                Logger.Log("Low damage to the leg");
                                attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 0.1f);
                            }
                            break;
                        default:
                            Logger.Log("Critical damage to the body");
                            attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
                            break;
                    }
                }
            }
        }

        public static float ToDegrees(float radians)
        {
            return radians * (180f / MathF.PI);
        }

        public static void DecideWeaponCollisionReactionPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent defender, ref MeleeCollisionReaction colReaction)
        {
            bool attackerIsTroll = attacker.Name.ToLower().Contains("troll");
            if (attackerIsTroll)
            {
                if (!collisionData.AttackBlockedWithShield)
                {
                    colReaction = MeleeCollisionReaction.SlicedThrough;
                }
                if (collisionData.CollisionResult == CombatCollisionResult.Parried)
                {
                    //collisionData = SetAttackCollisionResult(collisionData, CombatCollisionResult.StrikeAgent);
                    colReaction = MeleeCollisionReaction.SlicedThrough;
                    Logger.Log(defender?.Name + " parried.");
                }
            }

            bool defenderIsTroll = defender?.Name.ToLower().Contains("troll") ?? false;
            if (defenderIsTroll)
            {
                // Check if attacks comes from back etc. to make simulate troll defenses

                // Calculate the angle between the attack direction and the troll's forward direction
                Vec3 weaponBlowDirNormalized = collisionData.WeaponBlowDir.NormalizedCopy();
                Vec3 victimAgentMovementVelocityNormalized = defender.LookDirection.NormalizedCopy();

                float angle = MathF.Acos(Vec3.DotProduct(weaponBlowDirNormalized, victimAgentMovementVelocityNormalized));

                // Define a threshold angle to determine if the attack has hit the troll's back
                float backAngleThreshold = MathF.PI / 2; // Adjust this threshold as needed

                // Check if the attack has hit the troll's back based on the angle
                bool backAttack = angle > backAngleThreshold;

                if (backAttack)
                {
                    // Handle attack hitting the troll's back
                    colReaction = MeleeCollisionReaction.Bounced;
                    //Logger.Log("Back attack -> bounced", color: ConsoleColor.DarkMagenta);                    
                }
                else
                {
                    // Handle attack hitting other body parts of the troll
                    colReaction = MeleeCollisionReaction.Stuck;
                    //Logger.Log("Front attack -> stuck", color: ConsoleColor.Magenta);
                }
            }
        }

        public static void MeleeHitCallbackPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent victim, GameEntity realHitEntity, ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction, CrushThroughState crushThroughState, Vec3 blowDir, Vec3 swingDir, ref HitParticleResultData hitParticleResultData, bool crushedThroughWithoutAgentCollision)
        {
            int num = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
            bool flag = num >= 1 && attacker.Name.ToLower().Contains("troll");
            if (flag)
            {
                float num2 = 1f;//(float)collisionData.InflictedDamage / num;
                float pushDistance = 2f * num2; // Adjust the push distance as needed
                float projectionDuration = 0.4f * num2; // Adjust the duration of the projection effect

                // Find a way to prevent mounts going to the moon...
                //if (victim.IsMount)
                //{
                //    pushDistance = 2f;
                //    projectionDuration = 1f;
                //}

                // Calculate the push direction based on the blow direction
                Vec3 pushDirection = swingDir;
                Vec3 pushVector = pushDirection * pushDistance + Vec3.Up * 1f;

                if (CanProject(attacker))
                {
                    ProjectVictim(victim, pushVector, projectionDuration);

                    // Update the last projection time for the attacker
                    UpdateLastProjectionTime(attacker);
                }

                // Remaining momentum
                inOutMomentumRemaining = num2 * 0.8f;
            }
        }

        public static bool CanProject(Agent attacker)
        {
            float currentTime = Mission.Current.CurrentTime;

            if (lastProjectionTimes.TryGetValue(attacker, out float lastProjectionTime))
            {
                // Check if enough time has passed since the last projection
                if (currentTime - lastProjectionTime < 0.05f/*MinProjectionDelay*/)
                    return false;
            }

            return true;
        }

        public static void UpdateLastProjectionTime(Agent attacker)
        {
            float currentTime = Mission.Current.CurrentTime;

            if (lastProjectionTimes.ContainsKey(attacker))
                lastProjectionTimes[attacker] = currentTime;
            else
                lastProjectionTimes.Add(attacker, currentTime);
        }

        /// <summary>
        /// Project a victim on a vector by simulating a plane entity physically pushing it. 
        /// Agents hit on the victim traject will also suffect a blow.
        /// </summary>
        public static async Task ProjectVictim(Agent victim, Vec3 projectionVector, float projectionDuration)
        {
            float startTime = Mission.Current.CurrentTime;

            // List of all agents already hit by the projecting plane or the victim
            List<Agent> collidedAgents = new List<Agent>{ victim };

            Vec3 normalizedProjVector = projectionVector.NormalizedCopy();
            GameEntity projectingPlane = GameEntity.Instantiate(Mission.Current.Scene, "troll_hit_plane", false);
            projectingPlane.BodyFlag |= BodyFlags.Barrier;

            // Calculate plane original position, based on victim position and offsets defined
            Vec3 originalPosition = victim.Position + Vec3.Up * 0f/*planeHeightOffset*/ + normalizedProjVector * planeVectorOffset;
            // Calculate the rotation to align the carpet with the push vector
            MatrixFrame originalFrame = CalculateAlignedFrame(originalPosition, projectionVector);
            originalFrame.Scale(new Vec3(1.5f, 1.5f, 1.5f));

            // Set the frame (position and orientation) of the projecting plane
            projectingPlane.SetGlobalFrame(originalFrame);

            while (Mission.Current.CurrentTime - startTime < projectionDuration)
            {
                List<Agent> collidingAgents = Mission.Current.GetNearbyAgents(
                    victim.Position.AsVec2,
                    1f,
                    new MBList<Agent>()
                ).FindAll(agent => !collidedAgents.Contains(agent) && victim.Position.Z - agent.Position.Z < 2f);

                // Add agents hit to collided list to ignore them next time
                collidedAgents.AddRange(collidingAgents);

                // Do a blow to every colliding agents
                foreach (Agent agent in collidingAgents)
                {
                    agent.DealDamage(10, victim.Position, victim, true, true);
                }

                float elapsedSeconds = Mission.Current.CurrentTime - startTime;
                float t = elapsedSeconds / projectionDuration;
                float lerpT = SmoothStep(0f, 1f, t);
                float currentDistance = projectionVector.Length * lerpT;

                // Calculate the new position without gravity
                Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

                // Apply gravity to the newPosition
                float gravityOffset = 0f;// 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
                newPosition += -Vec3.Up * gravityOffset;

                // Move the projecting plane entity to the new position
                projectingPlane.SetLocalPosition(newPosition + Vec3.Up * planeHeightOffset);

                // Add a delay to refresh around 50 times per second
                await Task.Delay(20); 
            }

            Logger.Log("Projection ended after hitting " + collidedAgents.Count + " agents. Original pos : " + originalPosition + " | Final pos : " + projectingPlane.GlobalPosition, color: ConsoleColor.Cyan);

            // Remove the plane entity
            projectingPlane.Remove(0);
        }

        /*public static async Task FlyCarpet6(Agent victim, Vec3 projectionVector, float duration)
        {
            int i = 0;
            
            //ActionIndexCache horseFallbackAction = ActionIndexCache.Create("act_horse_fall_backwards");
            //ActionIndexCache fallbackAction = ActionIndexCache.Create("act_strike_fall_back_back_rise");
            //ActionIndexCache fallbackLeftAction = ActionIndexCache.Create("act_strike_fall_back_back_rise_left_stance");
            //ActionIndexCache fallbackHAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise");
            //ActionIndexCache fallbackHLeftAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise_left_stance");

            float startTime = Mission.Current.CurrentTime;
            //Vec3 agentPosition = victim.Position;
            float distance = projectionVector.Length;

            // Calculate the gravity force
            Vec3 gravity = -Vec3.Up * gravityMagnitude;

            // Create the flying carpet entity
            GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
            //flyingCarpet.BodyFlag |= BodyFlags.Barrier3D; Prevent flying to the moon but less satisfying physic :(
            // Calculate the position of the plane in front of the agent
            Vec3 originalPosition = agentPosition + Vec3.Up * planeHeightOffset + projectionVector.NormalizedCopy() * planeVectorOffset;
            InformationManager.DisplayMessage(new InformationMessage("AgentPos = " + agentPosition, Colors.Green));
            InformationManager.DisplayMessage(new InformationMessage("carpetPos = " + originalPosition, Colors.Green));

            // Set the local position of the flying carpet entity
            //flyingCarpet.SetLocalPosition(originalPosition);

            // Calculate the rotation to align the carpet with the push vector
            MatrixFrame frame = CalculateFrame(originalPosition, projectionVector);

            // Set the frame (position and orientation) of the flying carpet entity
            flyingCarpet.SetGlobalFrame(frame);

            Vec3 normalizedProjVector = projectionVector.NormalizedCopy();
            List<Agent> collidedAgents = new List<Agent>();
            while (Mission.Current.CurrentTime - startTime < duration)
            {
                float elapsedTime = Mission.Current.CurrentTime - startTime;
                float percentageDuration = elapsedTime / duration;
                Vec2 projectedDirection = projectionVector.AsVec2;

                List<Agent> collidingAgents = Mission.Current.GetNearbyAgents(
                    flyingCarpet.GetGlobalFrame().origin.AsVec2,
                    1f,
                    new MBList<Agent>()
                ).FindAll(agent => !collidedAgents.Contains(agent) && flyingCarpet.GetGlobalFrame().origin.Z - agent.Position.Z < 2f);

                collidedAgents.AddRange(collidingAgents);

                foreach (Agent agent in collidingAgents)
                {
                    // Get the agent's forward direction
                    Vec3 agentForward = agent.GetMovementDirection().ToVec3();

                    // Calculate the vector from the agent to the flying carpet
                    Vec3 agentToCarpet = flyingCarpet.GetGlobalFrame().origin - agent.Position;

                    // Calculate the angle between the agent's forward direction and the vector to the carpet
                    float angle = Vec3.AngleBetweenTwoVectors(agentForward, agentToCarpet);

                    // Convert the angle to degrees
                    float angleDegrees = angle * (180f / MathF.PI);

                    // Calculate the cross product of the agent's forward direction and the vector to the carpet
                    Vec3 crossProduct = Vec3.CrossProduct(agentForward, agentToCarpet);

                    // Check if the flying carpet is coming from the right or left side of the agent
                    bool fromRight = crossProduct.y > 0f;

                    // Check the strength of the flying carpet
                    bool isStrong = percentageDuration < 0.50f;
                    Logger.Log(angle + " || " + angleDegrees, LogLevel.Information, ConsoleColor.Red);
                    if (percentageDuration > 0.75f)
                    {

                    }
                    else if (agent.HasMount)
                    {
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, horseFallbackAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (fromRight && !isStrong)
                    {
                        Logger.Log("Weak hit coming from right");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (!fromRight && !isStrong)
                    {
                        Logger.Log("Weak hit coming from left");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackLeftAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (fromRight && isStrong)
                    {
                        Logger.Log("Strong hit coming from right");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackHAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (!fromRight && isStrong)
                    {
                        Logger.Log("Strong hit coming from left");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackHLeftAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                }

                float elapsedSeconds = Mission.Current.CurrentTime - startTime;
                float t = elapsedSeconds / duration;
                float lerpT = SmoothStep(0f, 1f, t);
                float currentDistance = distance * lerpT;

                // Calculate the new position without gravity
                Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

                // Apply gravity to the newPosition
                float gravityOffset = 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
                newPosition += -Vec3.Up * gravityOffset;

                // Move the flying carpet entity to the new position
                flyingCarpet.SetLocalPosition(newPosition + Vec3.Up * planeHeightOffset);

                i++;

                await Task.Yield();
            }

            InformationManager.DisplayMessage(new InformationMessage("Carpet updated " + i + " times. Original pos : " + originalPosition + " | Final pos : " + flyingCarpet.GlobalPosition, Colors.Green));

            // Remove the carpet entity
            flyingCarpet.Remove(0);
        }*/

        /// <summary>
        /// Returns a matrix frame with vectors aligned to the given push vector.
        /// </summary>
        private static MatrixFrame CalculateAlignedFrame(Vec3 position, Vec3 pushVector)
        {
            // We want the upward vector to be aligned on push vector
            Vec3 upVector = pushVector;
            upVector.Normalize();

            Vec3 forwardVector = Vec3.Forward;

            // Calculate the right vector perpendicular to the up vector and forward vector
            Vec3 rightVector = Vec3.CrossProduct(upVector, forwardVector);
            rightVector.Normalize();

            // Recalculate the forwardVector vector to ensure it is perpendicular to the up and right vectors
            forwardVector = Vec3.CrossProduct(rightVector, upVector);
            forwardVector.Normalize();

            MatrixFrame frame = new MatrixFrame();
            frame.origin = position;
            frame.rotation.u = upVector;
            frame.rotation.f = forwardVector;
            frame.rotation.s = rightVector;

            return frame;
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        public static async Task PerformProjection(Agent victim, Vec3 projectionVector, float duration)
        {
            float startTime = Mission.Current.CurrentTime;
            Vec3 originalPosition = victim.Position;
            //Vec3 targetPosition = originalPosition + projectionVector;
            float distance = projectionVector.Length;

            while (Mission.Current.CurrentTime - startTime < duration)
            {
                float t = (Mission.Current.CurrentTime - startTime) / duration;
                float lerpT = SmoothStep(0f, 1f, t);
                float currentDistance = distance * lerpT;
                Vec3 newPosition = originalPosition + projectionVector.NormalizedCopy() * currentDistance;

                victim.TeleportToPosition(newPosition);

                await Task.Yield();
            }
        }

        private static bool IsBackAttack(Agent attacker, Agent victim)
        {
            Vec3 victimLookDirection = victim.GetMovementDirection().ToVec3();

            // Calculate the vector from the victim to the attacker
            Vec3 victimToAttacker = (attacker.Position - victim.Position).NormalizedCopy();

            float angleInRadians = (float)Math.Atan2(victimLookDirection.x * victimToAttacker.y - victimLookDirection.y * victimToAttacker.x, victimLookDirection.x * victimToAttacker.x + victimLookDirection.y * victimToAttacker.y);
            float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));

            string position = string.Empty;

            float stanceOffset = victim.GetIsLeftStance() ? 25f : -25f;

            bool backAttack = false;

            if (angleInDegrees < -(90 - stanceOffset) && angleInDegrees > -(180 - stanceOffset)
                || angleInDegrees > (180 + stanceOffset))
            {
                position = "Back Right";
                backAttack = true;
            }
            else if (angleInDegrees < (0 + stanceOffset) && angleInDegrees > -(90 - stanceOffset))
            {
                position = "Front Right";
            }
            else if (angleInDegrees < (90 + stanceOffset) && angleInDegrees > (0 + stanceOffset))
            {
                position = "Front Left";
            }
            else if (angleInDegrees > (90 + stanceOffset) || angleInDegrees < -(180 - stanceOffset))
            {
                position = "Back Left";
                backAttack = true;
            }
            else
            {
                position = "error ? back left ?";
                backAttack = true;
            }

            //Logger.Log(position);
            //Logger.Log("angleInRadians: " + angleInRadians.ToString());
            //Logger.Log("angleInDegrees: " + angleInDegrees.ToString());
            return backAttack;
        }

        public static AttackCollisionData SetAttackCollisionData(AttackCollisionData data, bool attackBlockedWithShield, bool collidedWithShieldOnBack, bool missileBlockedWithWeapon, bool missileHasPhysics, int physicsMaterialIndex, bool isColliderAgent, CombatCollisionResult collisionResult)
        {
            return AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                attackBlockedWithShield,
                data.CorrectSideShieldBlock,
                data.IsAlternativeAttack,
                isColliderAgent,
                collidedWithShieldOnBack,
                data.IsMissile,
                missileBlockedWithWeapon,
                missileHasPhysics,
                data.EntityExists,
                data.ThrustTipHit,
                data.MissileGoneUnderWater,
                data.MissileGoneOutOfBorder,
                collisionResult,
                data.AffectorWeaponSlotOrMissileIndex,
                data.StrikeType,
                data.DamageType,
                data.CollisionBoneIndex,
                data.VictimHitBodyPart,
                data.AttackBoneIndex,
                data.AttackDirection,
                physicsMaterialIndex,
                data.CollisionHitResultFlags,
                data.AttackProgress,
                data.CollisionDistanceOnWeapon,
                data.AttackerStunPeriod,
                data.DefenderStunPeriod,
                data.MissileTotalDamage,
                data.MissileStartingBaseSpeed,
                data.ChargeVelocity,
                data.FallSpeed,
                data.WeaponRotUp,
                data.WeaponBlowDir,
                data.CollisionGlobalPosition,
                data.MissileVelocity,
                data.MissileStartingPosition,
                data.VictimAgentCurVelocity,
                data.CollisionGlobalNormal
            );
        }
    }
}
