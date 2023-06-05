/*using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using Trollito.Common.Utilities;
using Logger = Trollito.Common.Utilities.Logger;

namespace Trollito.Common.Patch
{
    /// <summary>
    /// Credits to Xorberax for original Cut Through Everyone mod
    /// </summary>
    public static class TrollPatchOld
    {
        private static readonly Harmony Harmony = new Harmony(nameof(TrollPatchOld));

        private static bool _patched;
        private static Dictionary<Agent, float> lastProjectionTimes = new Dictionary<Agent, float>();
        private const float MinProjectionDelay = 0.20f; // Minimum delay between consecutive projections (in seconds)
        private static ActionIndexCache fallbackAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise");

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "DecideWeaponCollisionReaction"),
                    postfix: new HarmonyMethod(typeof(TrollPatchOld), nameof(DecideWeaponCollisionReactionPostfix))
                );

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "MeleeHitCallback"),
                    postfix: new HarmonyMethod(typeof(TrollPatchOld), nameof(MeleeHitCallbackPostfix))
                );

                return true;
            }
            catch (Exception e)
            {
                Debug.Print("Error in CutThroughEveryonePatch: " + e.ToString(), 0, Debug.DebugColor.Red);
                return false;
            }
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
                    collisionData = SetAttackCollisionResult(collisionData, CombatCollisionResult.StrikeAgent);
                    colReaction = MeleeCollisionReaction.SlicedThrough;
                    Logger.Log("No don't you dare");
                }
                //collisionData. = CombatCollisionResult.StrikeAgent;
                //collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, collisionData.IsAlternativeAttack, collisionData.IsColliderAgent, false, collisionData.IsMissile, false, collisionData.MissileHasPhysics, collisionData.EntityExists, collisionData.ThrustTipHit, collisionData.MissileGoneUnderWater, collisionData.MissileGoneOutOfBorder, CombatCollisionResult.StrikeAgent, collisionData.AffectorWeaponSlotOrMissileIndex, collisionData.StrikeType, collisionData.DamageType, collisionData.CollisionBoneIndex, collisionData.VictimHitBodyPart, collisionData.AttackBoneIndex, collisionData.AttackDirection, collisionData.PhysicsMaterialIndex, collisionData.CollisionHitResultFlags, collisionData.AttackProgress, collisionData.CollisionDistanceOnWeapon, collisionData.AttackerStunPeriod, collisionData.DefenderStunPeriod, collisionData.MissileTotalDamage, collisionData.MissileStartingBaseSpeed, collisionData.ChargeVelocity, collisionData.FallSpeed, collisionData.WeaponRotUp, collisionData.WeaponBlowDir, collisionData.CollisionGlobalPosition, collisionData.MissileVelocity, collisionData.MissileStartingPosition, collisionData.VictimAgentCurVelocity, Vec3.Up);
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
                    InformationManager.DisplayMessage(new InformationMessage("Attack bounced", Colors.Green));
                }
                else
                {
                    // Handle attack hitting other body parts of the troll
                    colReaction = MeleeCollisionReaction.Stuck;
                    InformationManager.DisplayMessage(new InformationMessage("Attack stuck", Colors.Green));
                }
                // colReaction = MeleeCollisionReaction.SlicedThrough;
                // colReaction = MeleeCollisionReaction.Bounced;
                // colReaction = MeleeCollisionReaction.Stuck;
            }
        }

        public static void MeleeHitCallbackPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent victim, GameEntity realHitEntity, ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction, CrushThroughState crushThroughState, Vec3 blowDir, Vec3 swingDir, ref HitParticleResultData hitParticleResultData, bool crushedThroughWithoutAgentCollision)
        {
            int num = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
            bool flag = num >= 1 && attacker.Name.ToLower().Contains("troll");
            if (flag)
            {
                float num2 = (float)collisionData.InflictedDamage / num;
                float pushDistance = 6f * num2; // Adjust the push distance as needed
                float projectionDuration = 0.7f * num2; // Adjust the duration of the projection effect

                // Find a way to prevent mounts going to the moon...
                if (victim.IsMount)
                {
                    pushDistance = 2f;
                    projectionDuration = 1f;
                }

                // Calculate the push direction based on the blow direction
                Vec3 pushDirection = swingDir;
                Vec3 pushVector = pushDirection * pushDistance + Vec3.Up * 1f;

                // Check if a projection was already performed recently for the same attacker
                bool canFlyCarpet = CanPerformFlyCarpet(attacker);

                if (canFlyCarpet)
                {
                    FlyCarpet6(victim, pushVector, projectionDuration);

                    // Update the last projection time for the attacker
                    UpdateLastProjectionTime(attacker);
                }

                // Remaining momentum
                inOutMomentumRemaining = num2 * 0.8f;
            }
        }

        public static bool CanPerformFlyCarpet(Agent attacker)
        {
            float currentTime = Mission.Current.CurrentTime;

            //if (lastProjectionTimes.TryGetValue(attacker, out float lastProjectionTime))
            //{
            //    // Check if enough time has passed since the last projection
            //    if (currentTime - lastProjectionTime < MinProjectionDelay)
            //        return false;
            //}

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

        public static async Task FlyCarpet6(Agent victim, Vec3 projectionVector, float duration)
        {
            int i = 0;
            const float gravityMagnitude = 9.8f; // Adjust the gravity magnitude as needed
            const float carpetWidth = 1.5f; // Adjust the width of the flying carpet as needed
            const float carpetHeight = 0.5f; // Adjust the height of the flying carpet as needed
            const float carpetOffset = -0.5f; // Adjust the offset to spawn the carpet in front of the agent as needed
            ActionIndexCache horseFallbackAction = ActionIndexCache.Create("act_horse_fall_backwards");
            ActionIndexCache fallbackAction = ActionIndexCache.Create("act_strike_fall_back_back_rise");
            ActionIndexCache fallbackLeftAction = ActionIndexCache.Create("act_strike_fall_back_back_rise_left_stance");
            ActionIndexCache fallbackHAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise");
            ActionIndexCache fallbackHLeftAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise_left_stance");

            float startTime = Mission.Current.CurrentTime;
            Vec3 agentPosition = victim.Position;
            float distance = projectionVector.Length;

            // Calculate the gravity force
            Vec3 gravity = -Vec3.Up * gravityMagnitude;

            // Create the flying carpet entity
            GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
            //flyingCarpet.BodyFlag |= BodyFlags.Barrier3D;
            // Calculate the position of the plane in front of the agent
            Vec3 originalPosition = agentPosition + Vec3.Up * carpetHeight + projectionVector.NormalizedCopy() * carpetOffset;
            InformationManager.DisplayMessage(new InformationMessage("AgentPos = " + agentPosition, Colors.Green));
            InformationManager.DisplayMessage(new InformationMessage("carpetPos = " + originalPosition, Colors.Green));

            // Set the local position of the flying carpet entity
            flyingCarpet.SetLocalPosition(originalPosition);

            // Calculate the rotation to align the carpet with the push vector
            MatrixFrame frame = CalculateFrame(originalPosition, projectionVector);

            //// Calculate the new forward direction for the carpet plane
            //Vec3 newForward = frame.rotation.f;
            //newForward = Vec3.CrossProduct(-projectionVector, frame.rotation.f).NormalizedCopy();

            //// Calculate the new right direction for the carpet plane
            //Vec3 newRight = Vec3.CrossProduct(newForward, -projectionVector).NormalizedCopy();

            //// Calculate the new up direction for the carpet plane
            //Vec3 newUp = Vec3.CrossProduct(newRight, newForward).NormalizedCopy();

            //// Create a new rotation matrix based on the new directions
            //Mat3 newRotation = new Mat3(newRight, newUp, newForward);

            //// Set the new rotation to the frame
            //MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

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
                flyingCarpet.SetLocalPosition(newPosition + Vec3.Up * carpetHeight);

                i++;

                await Task.Yield();
            }

            InformationManager.DisplayMessage(new InformationMessage("Carpet updated " + i + " times. Original pos : " + originalPosition + " | Final pos : " + flyingCarpet.GlobalPosition, Colors.Green));

            // Remove the carpet entity
            flyingCarpet.Remove(0);
        }

        //public static async Task FlyCarpet5(Agent victim, Vec3 projectionVector, float duration)
        //{
        //    const float gravityMagnitude = 9.8f; // Adjust the gravity magnitude as needed
        //    const float carpetWidth = 1.5f; // Adjust the width of the flying carpet as needed

        //    float startTime = Mission.Current.CurrentTime;
        //    Vec3 originalPosition = victim.Position;
        //    float distance = projectionVector.Length;

        //    // Calculate the gravity force
        //    Vec3 gravity = -Vec3.Up * gravityMagnitude;

        //    // Create the flying carpet entity and position it beneath the agent
        //    GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
        //    Vec3 agentPosition = victim.Position;

        //    // Calculate the position of the plane beneath the agent
        //    Vec3 planePosition = new Vec3(agentPosition.X, agentPosition.Y - carpetWidth * 0.5f, agentPosition.Z);

        //    // Set the local position of the flying carpet entity
        //    flyingCarpet.SetLocalPosition(planePosition);

        //    // Calculate the rotation to align the carpet with the push vector
        //    Vec3 initialPosition = victim.Position + projectionVector * 0f;
        //    MatrixFrame frame = CalculateFrame(initialPosition, projectionVector);

        //    // Calculate the opposite direction of the push vector
        //    Vec3 oppositeDirection = -projectionVector;

        //    // Calculate the new forward direction for the carpet plane
        //    Vec3 newForward = frame.rotation.f;
        //    newForward.RotateAboutAnArbitraryVector(oppositeDirection, MathF.PI);

        //    // Calculate the new right direction for the carpet plane
        //    Vec3 newRight = Vec3.CrossProduct(frame.rotation.f, newForward).NormalizedCopy();

        //    // Calculate the new up direction for the carpet plane
        //    Vec3 newUp = Vec3.CrossProduct(newForward, newRight).NormalizedCopy();

        //    // Create a new rotation matrix based on the new directions
        //    Mat3 newRotation = new Mat3(newRight, newUp, newForward);

        //    // Set the new rotation to the frame
        //    MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

        //    // Set the frame (position and orientation) of the flying carpet entity
        //    flyingCarpet.SetGlobalFrame(rotatedFrame);

        //    Vec3 normalizedProjVector = projectionVector.NormalizedCopy();

        //    while (Mission.Current.CurrentTime - startTime < duration)
        //    {
        //        float elapsedSeconds = Mission.Current.CurrentTime - startTime;
        //        float t = elapsedSeconds / duration;
        //        float lerpT = SmoothStep(0f, 1f, t);
        //        float currentDistance = distance * lerpT;

        //        // Calculate the new position without gravity
        //        Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

        //        // Apply gravity to the newPosition
        //        float gravityOffset = 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
        //        newPosition += -Vec3.Up * gravityOffset;

        //        // Move the flying carpet entity to the new position
        //        flyingCarpet.SetLocalPosition(newPosition);

        //        await Task.Yield();
        //    }

        //    // Remove the carpet entity
        //    flyingCarpet.Remove(0);
        //}

        //public static async Task FlyCarpet4(Agent victim, Vec3 projectionVector, float duration)
        //{
        //    const float gravityMagnitude = 9.8f; // Adjust the gravity magnitude as needed

        //    float startTime = Mission.Current.CurrentTime;
        //    Vec3 originalPosition = victim.Position;
        //    float distance = projectionVector.Length;

        //    // Calculate the gravity force
        //    Vec3 gravity = -Vec3.Up * gravityMagnitude;

        //    // Create the flying carpet entity and position it beneath the agent
        //    GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
        //    Vec3 agentPosition = victim.Position;

        //    // Calculate the position of the plane beneath the agent
        //    Vec3 planePosition = new Vec3(agentPosition.X, agentPosition.Y - 0.5f, agentPosition.Z);

        //    // Set the local position of the flying carpet entity
        //    flyingCarpet.SetLocalPosition(planePosition);

        //    // Calculate the rotation to align the carpet with the push vector
        //    Vec3 initialPosition = victim.Position + projectionVector * 0f;
        //    MatrixFrame frame = CalculateFrame(initialPosition, projectionVector);

        //    // Calculate the opposite direction of the push vector
        //    Vec3 oppositeDirection = -projectionVector;

        //    // Calculate the new forward direction for the carpet plane
        //    Vec3 newForward = frame.rotation.f;
        //    newForward.RotateAboutAnArbitraryVector(oppositeDirection, MathF.PI);

        //    // Calculate the new right direction for the carpet plane
        //    Vec3 newRight = Vec3.CrossProduct(frame.rotation.f, newForward).NormalizedCopy();

        //    // Calculate the new up direction for the carpet plane
        //    Vec3 newUp = Vec3.CrossProduct(newForward, newRight).NormalizedCopy();

        //    // Create a new rotation matrix based on the new directions
        //    Mat3 newRotation = new Mat3(newRight, newUp, newForward);

        //    // Set the new rotation to the frame
        //    MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

        //    // Set the frame (position and orientation) of the flying carpet entity
        //    flyingCarpet.SetGlobalFrame(rotatedFrame);

        //    Vec3 normalizedProjVector = projectionVector.NormalizedCopy();

        //    while (Mission.Current.CurrentTime - startTime < duration)
        //    {
        //        float elapsedSeconds = Mission.Current.CurrentTime - startTime;
        //        float t = elapsedSeconds / duration;
        //        float lerpT = SmoothStep(0f, 1f, t);
        //        float currentDistance = distance * lerpT;

        //        // Calculate the new position without gravity
        //        Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

        //        // Apply gravity to the newPosition
        //        float gravityOffset = 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
        //        newPosition += -Vec3.Up * gravityOffset;

        //        // Move the flying carpet entity to the new position
        //        flyingCarpet.SetLocalPosition(newPosition);

        //        await Task.Yield();
        //    }

        //    // Remove the carpet entity
        //    flyingCarpet.Remove(0);
        //}

        //public static async Task FlyCarpet3(Agent victim, Vec3 projectionVector, float duration)
        //{
        //    int i = 0;
        //    const float gravityMagnitude = 9.8f; // Adjust the gravity magnitude as needed

        //    float startTime = Mission.Current.CurrentTime;
        //    Vec3 originalPosition = victim.Position;
        //    float distance = projectionVector.Length;

        //    // Calculate the gravity force
        //    Vec3 gravity = -Vec3.Up * gravityMagnitude;

        //    // Create the flying carpet entity and position it beneath the agent
        //    GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
        //    Vec3 agentPosition = victim.Position;

        //    // Calculate the position of the plane beneath the agent
        //    Vec3 planePosition = new Vec3(agentPosition.X, agentPosition.Y - 0.5f, agentPosition.Z);

        //    // Set the local position of the flying carpet entity
        //    flyingCarpet.SetLocalPosition(planePosition);

        //    // Calculate the rotation to align the carpet with the push vector
        //    Vec3 initialPosition = victim.Position + projectionVector * 0f;
        //    MatrixFrame frame = CalculateFrame(initialPosition, projectionVector);

        //    // Calculate the opposite direction of the push vector
        //    Vec3 oppositeDirection = -projectionVector;

        //    // Calculate the new forward direction for the carpet plane
        //    Vec3 newForward = frame.rotation.f;
        //    newForward.RotateAboutAnArbitraryVector(oppositeDirection, MathF.PI);

        //    // Calculate the new right direction for the carpet plane
        //    Vec3 newRight = Vec3.CrossProduct(frame.rotation.f, newForward).NormalizedCopy();

        //    // Calculate the new up direction for the carpet plane
        //    Vec3 newUp = Vec3.CrossProduct(newForward, newRight).NormalizedCopy();

        //    // Create a new rotation matrix based on the new directions
        //    Mat3 newRotation = new Mat3(newRight, newUp, newForward);

        //    // Set the new rotation to the frame
        //    MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

        //    // Set the frame (position and orientation) of the flying carpet entity
        //    flyingCarpet.SetGlobalFrame(rotatedFrame);

        //    Vec3 normalizedProjVector = projectionVector.NormalizedCopy();

        //    while (Mission.Current.CurrentTime - startTime < duration)
        //    {
        //        float elapsedSeconds = Mission.Current.CurrentTime - startTime;
        //        float t = elapsedSeconds / duration;
        //        float lerpT = SmoothStep(0f, 1f, t);
        //        float currentDistance = distance * lerpT;

        //        // Calculate the new position without gravity
        //        Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

        //        // Apply gravity to the newPosition
        //        float gravityOffset = 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
        //        newPosition += -Vec3.Up * gravityOffset;

        //        // Move the flying carpet entity to the new position
        //        flyingCarpet.SetLocalPosition(newPosition);
        //        i++;
        //        await Task.Yield();
        //    }

        //    InformationManager.DisplayMessage(new InformationMessage("Carpet updated " + i + " times. Original pos : " + originalPosition + " | Final pos : " + flyingCarpet.GlobalPosition, Colors.Green));

        //    // Remove the carpet entity
        //    flyingCarpet.Remove(0);
        //}

        //public static async Task FlyCarpet2(Agent victim, Vec3 projectionVector, float duration)
        //{
        //    int i = 0;
        //    const float gravityMagnitude = 9.8f; // Adjust the gravity magnitude as needed

        //    float startTime = Mission.Current.CurrentTime;
        //    Vec3 originalPosition = victim.Position;
        //    float distance = projectionVector.Length;

        //    // Calculate the gravity force
        //    Vec3 gravity = -Vec3.Up * gravityMagnitude;

        //    // Create the flying carpet entity and position it beneath the agent
        //    GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
        //    Vec3 agentPosition = victim.Position;

        //    // Calculate the position of the plane beneath the agent
        //    Vec3 planePosition = new Vec3(agentPosition.X, agentPosition.Y - 0.5f, agentPosition.Z);

        //    // Set the local position of the flying carpet entity
        //    flyingCarpet.SetLocalPosition(planePosition);

        //    // Calculate the rotation to align the carpet with the push vector
        //    Vec3 initialPosition = victim.Position + projectionVector * 0f;
        //    MatrixFrame frame = CalculateFrame(initialPosition, projectionVector);

        //    // Calculate the opposite direction of the push vector
        //    Vec3 oppositeDirection = -projectionVector;

        //    // Calculate the new forward direction for the carpet plane
        //    Vec3 newForward = frame.rotation.f;
        //    newForward.RotateAboutAnArbitraryVector(oppositeDirection, MathF.PI);

        //    // Calculate the new right direction for the carpet plane
        //    Vec3 newRight = Vec3.CrossProduct(frame.rotation.f, newForward).NormalizedCopy();

        //    // Calculate the new up direction for the carpet plane
        //    Vec3 newUp = Vec3.CrossProduct(newForward, newRight).NormalizedCopy();

        //    // Create a new rotation matrix based on the new directions
        //    Mat3 newRotation = new Mat3(newRight, newUp, newForward);

        //    // Set the new rotation to the frame
        //    MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

        //    // Set the frame (position and orientation) of the flying carpet entity
        //    flyingCarpet.SetGlobalFrame(rotatedFrame);

        //    Vec3 normalizedProjVector = projectionVector.NormalizedCopy();

        //    while (Mission.Current.CurrentTime - startTime < duration)
        //    {
        //        float elapsedSeconds = Mission.Current.CurrentTime - startTime;
        //        float t = elapsedSeconds / duration;
        //        float lerpT = SmoothStep(0f, 1f, t);
        //        float currentDistance = distance * lerpT;

        //        // Calculate the new position without gravity
        //        Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

        //        // Apply gravity to the newPosition
        //        float gravityOffset = 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
        //        newPosition += -Vec3.Up * gravityOffset;

        //        // Set the new position of the flying carpet entity
        //        flyingCarpet.SetLocalPosition(newPosition);

        //        // Update the victim's position to match the flying carpet
        //        victim.TeleportToPosition(newPosition);
        //        i++;
        //        await Task.Yield();
        //    }

        //    InformationManager.DisplayMessage(new InformationMessage("Carpet updated " + i + " times. Original pos : " + originalPosition + " | Final pos : " + flyingCarpet.GlobalPosition, Colors.Green));
        //    // Remove the carpet entity
        //    flyingCarpet.Remove(0);
        //}

        //public static async Task FlyCarpet(Agent victim, Vec3 projectionVector, float duration)
        //{
        //    int i = 0;
        //    float startTime = Mission.Current.CurrentTime;
        //    Vec3 originalPosition = victim.Position;
        //    //Vec3 targetPosition = originalPosition + projectionVector;
        //    float distance = projectionVector.Length;

        //    // Create the flying carpet entity and position it beneath the agent
        //    GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
        //    Vec3 agentPosition = victim.Position;

        //    // Calculate the position of the plane beneath the agent
        //    Vec3 planePosition = new Vec3(agentPosition.x, agentPosition.y - 0.5f, agentPosition.z);

        //    // Set the local position of the flying carpet entity
        //    flyingCarpet.SetLocalPosition(planePosition);

        //    // Calculate the rotation to align the carpet with the push vector
        //    Vec3 initialPosition = victim.Position + projectionVector * 0f;
        //    MatrixFrame frame = CalculateFrame(initialPosition, projectionVector);

        //    // Calculate the opposite direction of the push vector
        //    Vec3 oppositeDirection = -projectionVector;

        //    // Calculate the new forward direction for the carpet plane
        //    Vec3 newForward = frame.rotation.f;
        //    newForward.RotateAboutAnArbitraryVector(oppositeDirection, MathF.PI);

        //    // Calculate the new right direction for the carpet plane
        //    Vec3 newRight = Vec3.CrossProduct(frame.rotation.f, newForward).NormalizedCopy();

        //    // Calculate the new up direction for the carpet plane
        //    Vec3 newUp = Vec3.CrossProduct(newForward, newRight).NormalizedCopy();

        //    // Create a new rotation matrix based on the new directions
        //    Mat3 newRotation = new Mat3(newRight, newUp, newForward);

        //    // Set the new rotation to the frame
        //    MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

        //    // Set the frame (position and orientation) of the flying carpet entity
        //    flyingCarpet.SetGlobalFrame(rotatedFrame);

        //    Vec3 normalizedProjVector = projectionVector.NormalizedCopy();

        //    while (Mission.Current.CurrentTime - startTime < duration)
        //    {
        //        float t = (Mission.Current.CurrentTime - startTime) / duration;
        //        float lerpT = SmoothStep(0f, 1f, t);
        //        float currentDistance = distance * lerpT;
        //        //Vec3 newPosition = originalPosition + projectionVector.NormalizedCopy() * currentDistance;

        //        //Vec3 newPosition = flyingCarpet.GetFrame().origin + normalizedProjVector * currentDistance;
        //        Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;
        //        flyingCarpet.SetLocalPosition(newPosition);
        //        i++;

        //        await Task.Yield();
        //    }

        //    InformationManager.DisplayMessage(new InformationMessage("Carpet updated " + i + " times. Original pos : " + originalPosition + " | Final pos : " + flyingCarpet.GlobalPosition, Colors.Green));
        //    // Remove the carpet entity
        //    flyingCarpet.Remove(0);
        //}

        private static MatrixFrame CalculateFrame(Vec3 position, Vec3 pushVector)
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

            // Ensure the victim is at the final projected position
            //victim.TeleportToPosition(targetPosition);
        }

        //public static void EnableFlying(Agent agent, float flightHeight)
        //{
        //    // Create the flying carpet behaviour if it doesn't exist
        //    if (flyingCarpetBehavior == null)
        //    {
        //        flyingCarpetBehavior = new FlyingCarpetBehavior();
        //        Mission.Current.AddMissionBehavior(flyingCarpetBehavior);
        //    }

        //    // Register the agent with the flying carpet behaviour
        //    flyingCarpetBehavior.RegisterFlyingAgent(agent, flightHeight);
        //}

        //public static void DisableFlying(Agent agent)
        //{
        //    // Unregister the agent from the flying carpet behaviour
        //    flyingCarpetBehavior?.UnregisterFlyingAgent(agent);
        //}

        public static AttackCollisionData SetAttackCollisionResult(AttackCollisionData data, CombatCollisionResult collisionResult)
        {
            return AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                false,//data.AttackBlockedWithShield,
                false,//data.CorrectSideShieldBlock,
                data.IsAlternativeAttack,
                data.IsColliderAgent,
                false,//data.CollidedWithShieldOnBack,
                data.IsMissile,
                data.MissileBlockedWithWeapon,
                data.MissileHasPhysics,
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
                data.PhysicsMaterialIndex,
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
*/