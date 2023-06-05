using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Trollito.Common
{
    public class CollisionBoxVisualizer : MissionLogic
    {
        private List<MatrixFrame> _collisionBoxFrames;
        private Dictionary<sbyte, uint> _boneColors = new Dictionary<sbyte, uint>();

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            _collisionBoxFrames = new List<MatrixFrame>();
            List<GameEntity> entities = new List<GameEntity>();
            Mission.Current.Scene.GetEntities(ref entities);
            // Iterate through all entities in the current scene
            foreach (GameEntity entity in entities)
            {
                // Check if the entity has a body with collision shape
                if (entity.HasPhysicsBody() && entity.GetBodyShape().CapsuleCount() > 0)
                {
                    _collisionBoxFrames.Add(entity.GetGlobalFrame());
                }
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            _collisionBoxFrames.Add(agent.Frame);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Visualize the collision spheres
            foreach (Agent agent in Mission.Current.Agents)
            {
                Skeleton skeleton = agent.AgentVisuals.GetSkeleton();

                for (sbyte i = 0; i < skeleton.GetBoneCount(); i++)
                {
                    CapsuleData capsuleData = new CapsuleData();
                    skeleton.GetBoneBody(i, ref capsuleData);

                    if (capsuleData.Radius > 0)
                    {
                        // Bone has collision
                        MatrixFrame boneFrame = skeleton.GetBoneEntitialFrame(i);
                        MatrixFrame agentFrame = agent.Frame;
                        MatrixFrame finalFrame = agentFrame * boneFrame;

                        if (!_boneColors.ContainsKey(i))
                        {
                            _boneColors[i] = GetRandomColor(i);
                        }
                        uint boneColor = _boneColors[i];

                        MBDebug.RenderDebugSphere(finalFrame.origin, capsuleData.Radius, boneColor);
                    }
                }
            }
        }

        private uint GetRandomColor(int seed)
        {
            Random random = new Random(seed);
            float r = (float)random.NextDouble();
            float g = (float)random.NextDouble();
            float b = (float)random.NextDouble();
            Color randomColor = new Color(r, g, b);
            return randomColor.ToUnsignedInteger();
        }
    }
}
