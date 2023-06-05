/*using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Quaternion = TaleWorlds.Library.Quaternion;

namespace Trollito.Common.Troll
{
    public class TrollBehaviorOld : MissionLogic
    {
        struct ShockwaveEntity
        {
            public GameEntity Shockwave;
            public Vec3 PushVector;
            public float LifeTime;
        }

        private Dictionary<Agent, GameEntity> trolls = new Dictionary<Agent, GameEntity>();
        private List<ShockwaveEntity> shockwaves = new List<ShockwaveEntity>();
        private float flightHeight = 3f; // Default flight height

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.AddComponent(new TrollComponent(agent));
            return;
            if (agent.Name.ToLower().Contains("troll"))
            {
                Skeleton agentSkeleton = agent.AgentVisuals.GetSkeleton();
                MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame();
                GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, "bonk", false);
                // Get the wielded item
                EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                ItemObject wieldedItemObject = null;

                if (wieldedItemIndex != EquipmentIndex.None)
                {
                    wieldedItemObject = agent.Equipment[wieldedItemIndex].Item;
                }

                // Check if the wielded item is a weapon
                if (wieldedItemObject != null && wieldedItemObject.IsCraftedWeapon)
                {
                    // Get the weapon's bone index
                    sbyte weaponBoneIndex = agent.Monster.MainHandItemBoneIndex;

                    // Get the weapon's local position and rotation based on the bone transformation
                    Vec3 weaponLocalPosition = agentSkeleton.GetBoneEntitialFrameWithIndex(weaponBoneIndex).origin;
                    weaponLocalPosition *= 1.5f; // Scale if needed

                    Mat3 weaponLocalRotation = agentSkeleton.GetBoneEntitialFrameWithIndex(weaponBoneIndex).rotation;

                    // Transform the local position and rotation to global coordinates
                    Vec3 weaponGlobalPosition = agentGlobalFrame.TransformToParent(weaponLocalPosition);
                    Mat3 weaponGlobalRotation = agentGlobalFrame.rotation.TransformToParent(weaponLocalRotation);

                    // Update the gameEntity position and rotation to match the weapon
                    gameEntity.SetGlobalFrame(new MatrixFrame(weaponGlobalRotation, weaponGlobalPosition));
                }

                gameEntity.AddBodyFlags(BodyFlags.AgentOnly);

                InformationManager.DisplayMessage(new InformationMessage("Plane : " + gameEntity.GlobalPosition, Colors.Green));
                trolls.Add(agent, gameEntity);

                GameEntity weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(EquipmentIndex.Weapon0);
                InformationManager.DisplayMessage(new InformationMessage("WeaponPos : " + weaponEntityFromEquipmentSlot?.GlobalPosition, Colors.Green));
            }
        }

        public void RegisterFlyingAgent(Agent agent, Vec3 pushVector, float projectionDuration)
        {
            // Create the flying carpet entity and position it beneath the agent
            GameEntity flyingCarpet = SpawnFlyingCarpet(agent);

            // Calculate the rotation to align the carpet with the push vector
            Vec3 newPosition = agent.Position + pushVector * 0f;
            MatrixFrame frame = CalculateFrame(newPosition, pushVector);

            // Calculate the opposite direction of the push vector
            Vec3 oppositeDirection = -pushVector;

            // Calculate the new forward direction for the carpet plane
            Vec3 newForward = frame.rotation.f;
            newForward.RotateAboutAnArbitraryVector(oppositeDirection, MathF.PI);

            // Calculate the new right direction for the carpet plane
            Vec3 newRight = Vec3.CrossProduct(frame.rotation.f, newForward).NormalizedCopy();

            // Calculate the new up direction for the carpet plane
            Vec3 newUp = Vec3.CrossProduct(newForward, newRight).NormalizedCopy();

            // Create a new rotation matrix based on the new directions
            Mat3 newRotation = new Mat3(newRight, newUp, newForward);

            // Set the new rotation to the frame
            MatrixFrame rotatedFrame = new MatrixFrame(newRotation, frame.origin);

            // Set the frame (position and orientation) of the flying carpet entity
            flyingCarpet.SetGlobalFrame(rotatedFrame);

            ShockwaveEntity flyingCarpetStruct = new ShockwaveEntity()
            {
                Shockwave = flyingCarpet,
                PushVector = pushVector,
                LifeTime = projectionDuration
            };
            shockwaves.Add(flyingCarpetStruct);
        }

        public void UnregisterFlyingAgent(Agent agent)
        {
            // Remove the agent's flying carpet entity
            //if (flyingAgents.TryGetValue(agent, out FlyingCarpet flyingCarpet))
            //{
            //    flyingCarpet.Carpet.Remove(0);
            //    flyingAgents.Remove(agent);
            //}
        }

        public override void OnMissionTick(float dt)
        {
            // Iterate over the list
            for (int i = 0; i < shockwaves.Count; i++)
            {
                ShockwaveEntity flyingCarpet = shockwaves[i];

                if (flyingCarpet.LifeTime > 0f)
                {
                    GameEntity carpet = flyingCarpet.Shockwave;
                    Vec3 pushVector = flyingCarpet.PushVector;

                    // Calculate the new position based on the push vector and direction
                    Vec3 newPosition = carpet.GetFrame().origin + pushVector * (dt * 2);
                    carpet.SetLocalPosition(newPosition);


                    // Update the LifeTime directly in the struct
                    flyingCarpet.LifeTime -= dt;

                    // Update the struct in the list
                    shockwaves[i] = flyingCarpet;
                }
                else
                {
                    // Remove the carpet entity
                    flyingCarpet.Shockwave.Remove(0);
                    // Remove the flying carpet from the list
                    shockwaves.RemoveAt(i);
                    i--; // Decrement the index to account for the removed item
                }
            }

            // flying carpet way
            //foreach (var kvp in flyingCarpets)
            //{
            //    Agent agent = kvp.Key;
            //    GameEntity flyingCarpet = kvp.Value.Carpet;
            //    Vec3 pushVector = kvp.Value.PushVector;
            //    Vec3 pushDirection = kvp.Value.PushDirection;


            //    // Get the current position of the agent
            //    Vec3 currentPosition = agent.Position;

            //    // Get the ground height at the current position
            //    float groundHeight = Mission.Current.Scene.GetGroundHeightAtPosition(currentPosition);

            //    // Calculate the desired height of the flying carpet
            //    float desiredHeight = groundHeight + flightHeight;

            //    // Check if the carpet is below the desired height
            //    if (kvp.Value.LifeTime > 0)
            //    {
            //        // Calculate the new position by moving it up gradually
            //        float speed = 10f; // Adjust the speed as needed
            //        float targetHeight = MathF.Min(currentPosition.z + speed * dt, desiredHeight);

            //        // Update the position of the flying carpet with the new height
            //        flyingCarpet.SetLocalPosition(new Vec3(currentPosition.x, currentPosition.y, targetHeight));
            //        kvp.Value.LifeTime = dt;
            //    }
            //    else
            //    {

            //    }
            //    {
            //        UnregisterFlyingAgent(agent);
            //        // Update the position of the flying carpet to match the agent's position
            //        //flyingCarpet.SetLocalPosition(agent.Position);
            //    }
            //}


            return;



            //// flying carpet way
            //foreach (var kvp in flyingCarpets)
            //{
            //    Agent agent = kvp.Key;
            //    GameEntity flyingCarpet = kvp.Value.Carpet;

            //    // Get the current position of the agent
            //    Vec3 currentPosition = agent.Position;

            //    // Get the ground height at the current position
            //    float groundHeight = Mission.Current.Scene.GetGroundHeightAtPosition(currentPosition);

            //    // Calculate the desired height of the flying carpet
            //    float desiredHeight = groundHeight + flightHeight;

            //    // Check if the carpet is below the desired height
            //    if (currentPosition.z < desiredHeight)
            //    {
            //        // Calculate the new position by moving it up gradually
            //        float speed = 10f; // Adjust the speed as needed
            //        float targetHeight = MathF.Min(currentPosition.z + speed * dt, desiredHeight);

            //        // Update the position of the flying carpet with the new height
            //        flyingCarpet.SetLocalPosition(new Vec3(currentPosition.x, currentPosition.y, targetHeight));
            //    }
            //    else
            //    {
            //        UnregisterFlyingAgent(agent);
            //        // Update the position of the flying carpet to match the agent's position
            //        //flyingCarpet.SetLocalPosition(agent.Position);
            //    }
            //}


            return;
            // Get the player character agent
            //Agent playerAgent = Agent.Main;
            //if (playerAgent != null)
            //{
            //    // Check if the player is wielding an item
            //    //if (playerAgent.IsWieldingItem)
            //    //{
            //    //    // Get the wielded item position
            //    //    Vec3 wieldedItemPosition = playerAgent.WieldedItem.ItemEntity.GetGlobalFrame().origin;

            //    //    // Print the wielded item position to the game log
            //    //    InformationManager.DisplayMessage(new InformationMessage("Wielded Item Position: " + wieldedItemPosition.ToString()));
            //    //}
            //}

            foreach (var kvp in trolls)
            {
                Agent agent = kvp.Key;
                GameEntity gameEntity = kvp.Value;
                GameEntity weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(EquipmentIndex.Weapon0);
                Vec3 bbmin = new Vec3(0, 0, 0);
                Vec3 bbmax = new Vec3(0, 0, 0);
                weaponEntityFromEquipmentSlot?.GetPhysicsMinMax(true, out bbmin, out bbmax, false);
                InformationManager.DisplayMessage(new InformationMessage("WieldedWeaponFrame : " + agent.WieldedWeapon.Item.PrimaryWeapon.Frame.origin, Colors.Green));
                InformationManager.DisplayMessage(new InformationMessage("WeaponFrame : " + weaponEntityFromEquipmentSlot?.GetFrame().origin, Colors.Green));
                InformationManager.DisplayMessage(new InformationMessage("WeaponGFrame : " + weaponEntityFromEquipmentSlot?.GetGlobalFrame().origin, Colors.Green));
                InformationManager.DisplayMessage(new InformationMessage("PhysMinMax : " + bbmin + " | " + bbmax, Colors.Green));

                break;
                // Get the wielded item
                EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                ItemObject wieldedItemObject = null;

                if (wieldedItemIndex != EquipmentIndex.None)
                {
                    wieldedItemObject = agent.Equipment[wieldedItemIndex].Item;
                }

                // Check if the wielded item is a hammer
                if (wieldedItemObject != null && wieldedItemObject.IsCraftedWeapon)
                {
                    // Get the position of the hammer item
                    //Vec3 hammerPosition = agent.AgentVisuals.GetGlobalFrame().TransformToParent(agent.Equipment[wieldedItemIndex].GetGlobalFrame().origin);

                    // Set the position of the gameEntity to match the hammer position
                    //gameEntity.SetGlobalFrame(new MatrixFrame(Mat3.Identity, hammerPosition));
                }

                InformationManager.DisplayMessage(new InformationMessage("Hammer Position: " + gameEntity.GlobalPosition, Colors.Green));
            }

            return;
            // Somewhat working (very meh) physic impact with only one physic entity on the hammer
            foreach (var kvp in trolls)
            {
                Agent agent = kvp.Key;
                GameEntity gameEntity = kvp.Value;
                Skeleton agentSkeleton = agent.AgentVisuals.GetSkeleton();
                MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame();

                // Get the wielded item
                EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                ItemObject wieldedItemObject = null;

                if (wieldedItemIndex != EquipmentIndex.None)
                {
                    wieldedItemObject = agent.Equipment[wieldedItemIndex].Item;
                }

                // Check if the wielded item is a weapon
                if (wieldedItemObject != null && wieldedItemObject.IsCraftedWeapon)
                {
                    // Get the weapon's bone index
                    sbyte weaponBoneIndex = agent.Monster.MainHandItemBoneIndex;

                    // Get the weapon's local position and rotation based on the bone transformation
                    Vec3 weaponLocalPosition = agentSkeleton.GetBoneEntitialFrameWithIndex(weaponBoneIndex).origin;
                    // Scale if needed to adjust position
                    //weaponLocalPosition *= 1.2f;
                    weaponLocalPosition.y *= 1.4f;
                    weaponLocalPosition.x *= 1.4f;
                    weaponLocalPosition.z *= 1f;

                    Mat3 weaponLocalRotation = agentSkeleton.GetBoneEntitialFrameWithIndex(weaponBoneIndex).rotation;

                    // Transform the local position and rotation to global coordinates
                    Vec3 weaponGlobalPosition = agentGlobalFrame.TransformToParent(weaponLocalPosition);
                    Mat3 weaponGlobalRotation = agentGlobalFrame.rotation.TransformToParent(weaponLocalRotation);

                    // Apply adjustments to the weapon's position
                    //float positionAdjustment = 1f; // Adjust the position shift as needed
                    //weaponGlobalPosition.y += positionAdjustment; // Adjust the axis for vertical shift

                    // Scale if needed to adjust size
                    float scale = 0.4f;
                    weaponGlobalRotation.ApplyScaleLocal(scale);

                    // Update the gameEntity position and rotation to match the weapon
                    gameEntity.SetGlobalFrame(new MatrixFrame(weaponGlobalRotation, weaponGlobalPosition));
                }

                InformationManager.DisplayMessage(new InformationMessage("Weapon Position: " + gameEntity.GlobalPosition, Colors.Green));
                //InformationManager.DisplayMessage(new InformationMessage("Bone : " + mainHandLocalPosition, Colors.Green));
            }
            return;
        }

        private GameEntity SpawnFlyingCarpet(Agent agent)
        {
            //GameEntity gameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
            GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
            //gameEntity.SetAlpha(0f);
            //Material material = Material.GetDefaultMaterial();
            //gameEntity.SetMaterialForAllMeshes(material);

            Vec3 agentPosition = agent.Position;

            // Calculate the position of the plane beneath the agent
            Vec3 planePosition = new Vec3(agentPosition.x, agentPosition.y - flightHeight, agentPosition.z);

            // Set the local position of the flying carpet entity
            gameEntity.SetLocalPosition(planePosition);

            return gameEntity;
        }

        public static Vec3 RotatePerpendicular(Quaternion rotation, Vec3 vector)
        {
            // Create a pure quaternion from the vector
            Quaternion vectorQuat = new Quaternion(vector.X, vector.Y, vector.Z, 0f);

            // Compute the inverse of the rotation quaternion
            Quaternion conjugateRotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, rotation.W);

            // Multiply the quaternions manually
            Quaternion rotatedVectorQuat = MultiplyQuaternion(rotation, MultiplyQuaternion(vectorQuat, conjugateRotation));

            // Extract the rotated vector from the quaternion
            Vec3 rotatedVector = new Vec3(rotatedVectorQuat.X, rotatedVectorQuat.Y, rotatedVectorQuat.Z);

            return rotatedVector;
        }

        // Manually multiply two quaternions
        private static Quaternion MultiplyQuaternion(Quaternion a, Quaternion b)
        {
            float x = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
            float y = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
            float z = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;
            float w = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;

            return new Quaternion(x, y, z, w);
        }

        private MatrixFrame CalculateFrame(Vec3 position, Vec3 pushVector)
        {
            Vec3 forwardVector = pushVector;
            forwardVector.Normalize();

            Vec3 rightVector = Vec3.CrossProduct(Vec3.Side, forwardVector);
            rightVector.Normalize();

            Vec3 upVector = Vec3.CrossProduct(forwardVector, rightVector);
            upVector.Normalize();

            MatrixFrame frame = new MatrixFrame();
            frame.origin = position;
            frame.rotation.u = rightVector;
            frame.rotation.f = forwardVector;
            frame.rotation.s = upVector;

            return frame;
        }

        private Mesh CreatePlaneMesh(float width, float height)
        {
            Mesh mesh = Mesh.CreateMesh();

            mesh.ClearMesh();
            mesh.HintVerticesDynamic();
            mesh.HintIndicesDynamic();

            Vec3[] vertices = new Vec3[4];

            // Define the vertices of the plane in counter-clockwise order
            vertices[0] = new Vec3(-width / 2f, -height / 2f, 0f);
            vertices[1] = new Vec3(-width / 2f, height / 2f, 0f);
            vertices[2] = new Vec3(width / 2f, height / 2f, 0f);
            vertices[3] = new Vec3(width / 2f, -height / 2f, 0f);

            // Add the vertices to the mesh
            for (int i = 0; i < vertices.Length; i++)
            {
                mesh.AddFaceCorner(vertices[i], Vec3.Zero, Vec2.Zero, 0u, UIntPtr.Zero);
            }

            // Add the face to the mesh
            mesh.AddFace(0, 1, 2, UIntPtr.Zero);
            mesh.AddFace(0, 2, 3, UIntPtr.Zero);

            return mesh;
        }

        private Mat3 LookDirection(Vec3 lookDir, Vec3 up)
        {
            Vec3 forward = lookDir;
            forward.Normalize();
            Vec3 right = Vec3.CrossProduct(up, forward);
            right.Normalize();
            Vec3 newUp = Vec3.CrossProduct(forward, right);
            newUp.Normalize();
            return new Mat3(right, newUp, forward);
        }
    }
}
*/