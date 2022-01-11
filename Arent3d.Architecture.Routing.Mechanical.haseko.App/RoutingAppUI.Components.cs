using Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Initialization ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.PassPoint ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Rack ;
using Arent3d.Revit.UI.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App
{
  partial class RoutingAppUI
  {
    [Tab( "Mechanical.haseko.App.Routing.TabName", VisibilityMode = TabVisibilityMode.NormalDocument )]
    private static class RoutingTab
    {
      [Panel( "arent3d.architecture.routing.init", TitleKey = "Mechanical.haseko.App.Panels.Routing.Initialize" )]
      private static class InitPanel
      {
        [Button( typeof( InitializeCommand ), InitializeButton = true )]
        private static class InitializeCommandButton { }

        // [Button( typeof( ShowRoutingViewsCommand ), OnlyInitialized = true )]
        // private static class ShowRoutingViewsCommandButton { }
      }

      // [Panel("arent3d.architecture.routing.routing", TitleKey = "Mechanical.haseko.App.Panels.Routing.Routing" )]
      // private static class RoutingPanel
      // {
      //   [Button( typeof( PickRoutingCommand ), OnlyInitialized = true )]
      //   private static class PickRoutingCommandButton { }
      //   
      //   [Button( typeof( SimplePickRoutingCommand ), OnlyInitialized = true )]
      //   private static class SimplePickRoutingCommandButton { }
      //
      //   [Button( typeof( PickAndReRouteCommand ), OnlyInitialized = true )]
      //   private static class PickAndReRouteCommandButton { }
      //
      //   [Button( typeof( AllReRouteCommand ), OnlyInitialized = true )]
      //   private static class AllReRouteCommandButton { }
      //
      //   [Button( typeof( InsertPassPointCommand ), OnlyInitialized = true )]
      //   private static class InsertPassPointCommandButton { }
      //
      //   [Button( typeof( EraseSelectedRoutesCommand ), OnlyInitialized = true )]
      //   private static class EraseSelectedRoutesCommandButton { }
      //
      //   [Button( typeof( EraseAllRoutesCommand ), OnlyInitialized = true )]
      //   private static class EraseAllRoutesCommandButton { }
      //
      //   [Button( typeof( ReplaceFromToCommand ), OnlyInitialized = true )]
      //   private static class ReplaceFromToCommandButton { }
      //
      //   [Button( typeof( ShowFrom_ToWindowCommand ), OnlyInitialized = true )]
      //   private static class ShowFrom_ToWindowCommandButton { }
      //
      //   [Button( typeof( ShowFromToTreeCommand ), OnlyInitialized = true )]
      //   private static class ShowFromToTreeCommandButton { }
      // }

      // [Panel( "arent3d.architecture.routing.rack", TitleKey = "Mechanical.haseko.App.Panels.Routing.Racks" )]
      // private static class RackPanel
      // {
      //   [Button( typeof( ImportRacksCommand ), OnlyInitialized = true )]
      //   private static class ImportRacksCommandButton { }
      //
      //   [Button( typeof( ExportRacksCommand ), OnlyInitialized = true )]
      //   private static class ExportRacksCommandButton { }
      //
      //   [Button( typeof( EraseAllRacksCommand ), OnlyInitialized = true )]
      //   private static class EraseAllRacksCommandButton { }
      //
      //   [Button( typeof( RackGuideCommand ), OnlyInitialized = true )]
      //   private static class RackGuidCommandButton { }
      //   
      //   [Button( typeof( CreateFASUAndVAVAutomaticallyCommand ), OnlyInitialized = true )]
      //   private static class CreateFASUAndVAVAutomaticallyCommandButton { }
      //
      //   [Button( typeof( AutoRoutingVavCommand ), OnlyInitialized = true )]
      //   private static class AutoRoutingVavCommandButton { }
      //   
      //   [Button( typeof( AdjustDuctSizeCommand ), OnlyInitialized = true )]
      //   private static class AdjustDuctSizeCommandButton { }        
      // }
      //
      // [Panel( "arent3d.architecture.routing.monitor", TitleKey = "Mechanical.haseko.App.Panels.Routing.Monitor" )]
      // private static class MonitorPanel
      // {
      //   [Button( typeof( MonitorSelectionCommand ), AvailabilityType = typeof( Commands.Enabler.MonitorSelectionCommandEnabler ) )]
      //   private static class MonitorSelectionCommandButton { }
      // }
      //
      // [Panel( "arent3d.architecture.rc.debug", TitleKey = "App.Panels.Rc.Debug" )]
      // private static class DebugPanel
      // {
      //
      //   [Button( typeof( UninitializeCommand ), OnlyInitialized = true )]
      //   private static class UnInitializeCommandButton
      //   {
      //   }
      // }
    }
  }
}