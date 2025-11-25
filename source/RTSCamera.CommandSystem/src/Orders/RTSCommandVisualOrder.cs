using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders
{
    public enum SelectTargetMode
    {
        None,
        Advance,
        LookAtDirection,
        LookAtEnemy,
        Count
    }
    public abstract class RTSCommandVisualOrder : VisualOrder
    {
        protected bool QueueCommand = false;
        protected bool IsSelectTargetForMouseClickingKeyDown = false;
        public static bool IsFromClicking = false;

        public static SelectTargetMode OrderToSelectTarget = SelectTargetMode.None;
        protected RTSCommandVisualOrder(string stringId) : base(stringId)
        {
        }

        protected bool OnBeforeExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
        {
            var selectedFormations = orderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            QueueCommand = Utilities.Utility.ShouldQueueCommand();
            if (!QueueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(selectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(selectedFormations));
            }
            IsSelectTargetForMouseClickingKeyDown = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectTargetForCommand).IsKeyDownInOrder();
            if (!IsSelectTargetForMouseClickingKeyDown)
            {
                OrderToSelectTarget = SelectTargetMode.None;
            }
            return QueueCommand;
        }
    }
}
