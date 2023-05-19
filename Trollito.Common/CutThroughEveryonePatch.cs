using HarmonyLib;
using System;
using System.Reflection;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace Trollito.Common
{
    /// <summary>
    /// Credits to Xorberax for original Cut Through Everyone mod
    /// </summary>
    public static class CutThroughEveryonePatch
    {
        private static readonly Harmony Harmony = new Harmony(nameof(CutThroughEveryonePatch));

        private static bool _patched;
        private static MethodInfo setPositionMethod;
        private static object scriptingInterface;
        private static FlyingCarpetBehavior flyingCarpetBehavior;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "DecideWeaponCollisionReaction"),
                    postfix: new HarmonyMethod(typeof(CutThroughEveryonePatch), nameof(DecideWeaponCollisionReactionPostfix))
                );

                Harmony.Patch(
                    AccessTools.Method(typeof(Mission), "MeleeHitCallback"),
                    postfix: new HarmonyMethod(typeof(CutThroughEveryonePatch), nameof(MeleeHitCallbackPostfix))
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
            bool flag = attacker.Name.ToLower().Contains("troll");
            if (flag)
            {
                colReaction = MeleeCollisionReaction.SlicedThrough;
            }
        }

        public static void MeleeHitCallbackPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent victim, GameEntity realHitEntity, ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction, CrushThroughState crushThroughState, Vec3 blowDir, Vec3 swingDir, ref HitParticleResultData hitParticleResultData, bool crushedThroughWithoutAgentCollision)
        {
            int num = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
            bool flag = num >= 1 && attacker.Name.ToLower().Contains("troll");
            if (flag)
            {
                float num2 = (float)collisionData.InflictedDamage / (float)num;
                float pushDistance = 12f * num2; // Adjust the push distance as needed
                float projectionDuration = 0.6f * num2; // Adjust the duration of the projection effect

                // Calculate the push direction based on the blow direction
                Vec3 pushDirection = swingDir;

                Vec3 pushVector = pushDirection * pushDistance + Vec3.Up * 100f;

                float flightHeight = 10f; // 10 meters
                float duration = 10f; // 10 seconds

                FlyingCarpetBehavior fcb = Mission.Current.GetMissionBehavior<FlyingCarpetBehavior>();
                fcb.RegisterFlyingAgent(victim, flightHeight);

                //PerformProjection(victim, pushVector, projectionDuration);

                // Remaining momentum
                inOutMomentumRemaining = num2 * 0.8f;
            }
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

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
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
    }
}
