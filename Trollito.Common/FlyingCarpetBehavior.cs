using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Trollito.Common
{
    public class FlyingCarpetBehavior : MissionLogic
    {
        private Dictionary<Agent, GameEntity> flyingAgents = new Dictionary<Agent, GameEntity>();
        private float flightHeight = 3f; // Default flight height

        public void RegisterFlyingAgent(Agent agent, float flightHeight)
        {
            // Add the agent and its associated flying carpet to the dictionary
            if(!flyingAgents.ContainsKey(agent))
            {
                // Create the flying carpet entity and position it beneath the agent
                GameEntity flyingCarpet = SpawnFlyingCarpet(agent);
                //flyingCarpet.SetLocalPosition(agent.Position - new Vec3(0f, flightHeight, 0f));
                flyingCarpet.SetLocalPosition(agent.Position);
                flyingAgents.Add(agent, flyingCarpet);
            }            
        }

        public void UnregisterFlyingAgent(Agent agent)
        {
            // Remove the agent's flying carpet entity
            if (flyingAgents.TryGetValue(agent, out GameEntity flyingCarpet))
            {
                flyingCarpet.Remove(0);
                flyingAgents.Remove(agent);
            }
        }

        public override void OnMissionTick(float dt)
        {
            foreach (var kvp in flyingAgents)
            {
                Agent agent = kvp.Key;
                GameEntity flyingCarpet = kvp.Value;

                // Get the current position of the agent
                Vec3 currentPosition = agent.Position;

                // Get the ground height at the current position
                float groundHeight = Mission.Current.Scene.GetGroundHeightAtPosition(currentPosition);

                // Calculate the desired height of the flying carpet
                float desiredHeight = groundHeight + flightHeight;

                // Check if the carpet is below the desired height
                if (currentPosition.z < desiredHeight)
                {
                    // Calculate the new position by moving it up gradually
                    float speed = 10f; // Adjust the speed as needed
                    float targetHeight = MathF.Min(currentPosition.z + speed * dt, desiredHeight);

                    // Update the position of the flying carpet with the new height
                    flyingCarpet.SetLocalPosition(new Vec3(currentPosition.x, currentPosition.y, targetHeight));
                }
                else
                {
                    UnregisterFlyingAgent(agent);
                    // Update the position of the flying carpet to match the agent's position
                    //flyingCarpet.SetLocalPosition(agent.Position);
                }
            }
        }

        private GameEntity SpawnFlyingCarpet(Agent agent)
        {
            //GameEntity gameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
            GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, "editor_plane", false);            
            gameEntity.SetAlpha(0f);
            //Material material = Material.GetDefaultMaterial();
            //gameEntity.SetMaterialForAllMeshes(material);

            Vec3 agentPosition = agent.Position;

            // Calculate the position of the plane beneath the agent
            Vec3 planePosition = new Vec3(agentPosition.x, agentPosition.y - flightHeight, agentPosition.z);

            // Set the local position of the flying carpet entity
            gameEntity.SetLocalPosition(planePosition);

            return gameEntity;
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
    }
}
