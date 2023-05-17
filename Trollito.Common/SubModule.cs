using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Trollito.Common
{
    public class SubModule : MBSubModuleBase
    {
        public const string ModuleId = "Trollito";

        protected override void OnSubModuleLoad()
        {
            CutThroughEveryonePatch.Patch();
            Debug.Print("Trollito Common initialized.", 0, Debug.DebugColor.Green);
            InformationManager.DisplayMessage(new InformationMessage("Trollito Common initialized.", Colors.Green));
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            //Mission.Current.AddMissionBehavior(new CollisionBoxVisualizer());
            //Mission.Current.AddMissionBehavior(new TestBonk());
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            gameStarter.AddModel(new TestAgentStatCalculateModel());
            gameStarter.AddModel(new TestAgentApplyDamageModel());
        }
    }
}