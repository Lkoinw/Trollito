using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Trollito.Common.Models;
using Trollito.Common.Patch;
using Trollito.Common.Troll;

namespace Trollito.Common
{
    public class SubModule : MBSubModuleBase
    {
        public const string ModuleId = "Trollito";

        protected override void OnSubModuleLoad()
        {
            TrollPatch.Patch();
            Debug.Print("Trollito Common initialized.", 0, Debug.DebugColor.Green);
            InformationManager.DisplayMessage(new InformationMessage("Trollito Common initialized.", Colors.Green));
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            Mission.Current.AddMissionBehavior(new TrollBehavior());
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            gameStarter.AddModel(new TrollitoStatCalculateModel());
            gameStarter.AddModel(new TrollitoApplyDamageModel());
        }
    }
}