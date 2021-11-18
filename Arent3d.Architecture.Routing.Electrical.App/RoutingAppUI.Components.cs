using Arent3d.Architecture.Routing.Electrical.App.Commands ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Demo ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PassPoint ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Rack ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Shaft;
using Arent3d.Revit.UI.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  partial class RoutingAppUI
  {
    [Tab( "Electrical.App.Routing.TabName", VisibilityMode = TabVisibilityMode.NormalDocument )]
    private static class RoutingTab
    {
      [Panel( "arent3d.architecture.routing.init", TitleKey = "Electrical.App.Panels.Routing.Initialize" )]
      private static class InitPanel
      {
        [Button( typeof( InitializeCommand ), InitializeButton = true )]
        private static class InitializeCommandButton { }

        [Button( typeof( ShowRoutingViewsCommand ), OnlyInitialized = true )]
        private static class ShowRoutingViewsCommandButton { }
        
        [Button( typeof( Show3DViewsCommand ), OnlyInitialized = true )]
        private static class Show3DViewsCommandButton { }
        
        [Button(typeof(CnsSettingCommand), OnlyInitialized = true)]
        private static class CnsSettingCommandButton { }
        
        [Button(typeof(LoadCsvFilesCommand), OnlyInitialized = true)]
        private static class LoadCsvFilesCommandButton { }
      }

      [Panel("arent3d.architecture.routing.routing", TitleKey = "Electrical.App.Panels.Routing.Routing" )]
      private static class RoutingPanel
      {
        [Button( typeof( PickRoutingCommand ), OnlyInitialized = true )]
        private static class PickRoutingCommandButton { }

        [Button( typeof( PickAndReRouteCommand ), OnlyInitialized = true )]
        private static class PickAndReRouteCommandButton { }

        [Button( typeof( AllReRouteCommand ), OnlyInitialized = true )]
        private static class AllReRouteCommandButton { }

        [Button( typeof( InsertPassPointCommand ), OnlyInitialized = true )]
        private static class InsertPassPointCommandButton { }

        [Button( typeof( EraseSelectedRoutesCommand ), OnlyInitialized = true )]
        private static class EraseSelectedRoutesCommandButton { }

        [Button( typeof( EraseAllRoutesCommand ), OnlyInitialized = true )]
        private static class EraseAllRoutesCommandButton { }

        [Button( typeof( ReplaceFromToCommand ), OnlyInitialized = true )]
        private static class ReplaceFromToCommandButton { }

        [Button( typeof( ShowFrom_ToWindowCommand ), OnlyInitialized = true )]
        private static class ShowFrom_ToWindowCommandButton { }

        [Button( typeof( ShowFromToTreeCommand ), OnlyInitialized = true )]
        private static class ShowFromToTreeCommandButton { }
        
        [Button( typeof( SelectionRangeRouteCommand ), OnlyInitialized = true )]
        private static class SelectionRangeRouteCommandButton { }

        [Button( typeof( ShowHeightSettingCommand ), OnlyInitialized = true )]
        private static class ShowHeightSettingCommandButton { }

        [Button( typeof( NewRackCommand ), OnlyInitialized = true )]
        private static class NewRackCommandButton { }

        [Button( typeof( NewRackFromToCommand ), OnlyInitialized = true )]
        private static class NewRackFromToCommandButton { }

        [Button( typeof( NewLimitRackCommand ), OnlyInitialized = true )]
        private static class NewLimitRackCommandButton { }

        [Button( typeof( EraseAllLimitRackCommand ), OnlyInitialized = true )]
        private static class EraseAllLimitRackCommandButton { }
      }

      [Panel("arent3d.architecture.routing.connectors", TitleKey = "Electrical.App.Panels.Routing.Connectors")]
      private static class ConnectorsPanel
      {
        [Button(typeof(NewConnectorCommand), OnlyInitialized = true)]
        private static class NewConnectorCommandButton { }
        
        [Button(typeof(NewConnectorBsCommand), OnlyInitialized = true)]
        private static class NewConnectorBsCommandButton { }

        [Button(typeof(NewDamperActuatorCommand), OnlyInitialized = true)]
        private static class NewDamperActuatorCommandButton { }

        [Button(typeof(NewElectricTwoWayValveWithLogoCommand), OnlyInitialized = true)]
        private static class NewElectricTwoWayValveWithLogoCommandButton { }

        [Button(typeof(NewElectricTwoWayValveWithoutLogoCommand), OnlyInitialized = true)]
        private static class NewElectricTwoWayValveWithoutLogoCommandButton { }

        [Button(typeof(NewHumiditySensorForDuctWithLogoCommand), OnlyInitialized = true)]
        private static class NewHumiditySensorForDuctWithLogoCommandButton { }

        [Button(typeof(NewHumiditySensorForDuctWithoutLogoCommand), OnlyInitialized = true)]
        private static class NewHumiditySensorForDuctWithoutLogoCommandButton { }

        [Button(typeof(NewIndoorHumiditySensorWithLogoCommand), OnlyInitialized = true)]
        private static class NewIndoorHumiditySensorWithLogoCommandButton { }

        [Button(typeof(NewIndoorHumiditySensorWithoutLogoCommand), OnlyInitialized = true)]
        private static class NewIndoorHumiditySensorWithoutLogoCommandButton { }
      }

      [Panel( "arent3d.architecture.routing.rack", TitleKey = "Electrical.App.Panels.Routing.Racks" )]
      private static class RackPanel
      {
        [Button( typeof( ImportRacksCommand ), OnlyInitialized = true )]
        private static class ImportRacksCommandButton { }

        [Button( typeof( ExportRacksCommand ), OnlyInitialized = true )]
        private static class ExportRacksCommandButton { }

        [Button( typeof( EraseAllRacksCommand ), OnlyInitialized = true )]
        private static class EraseAllRacksCommandButton { }

        [Button( typeof( RackSpaceCommand ), OnlyInitialized = true )]
        private static class RackGuidCommandButton { }
      }

      [Panel( "arent3d.architecture.routing.envelope", TitleKey = "Electrical.App.Panels.Routing.Envelope" )]
      private static class EnvelopPanel
      {
        [Button(typeof(NewEnvelopeCommand), OnlyInitialized = true)]
        private static class NewEnvelopeCommandButton { }
        
        [Button(typeof(CeilingEnvelopeCommand), OnlyInitialized = true)]
        private static class CeilingEnvelopeCommandButton { }        
        
        [Button( typeof( OffsetSettingCommand ), OnlyInitialized = true )]
        private static class OffsetSettingCommandButton { }   
      }
      
      [Panel( "arent3d.architecture.routing.shaft", TitleKey = "Electrical.App.Panels.Routing.Shafts" )]
      private static class ShaftPanel
      {
        [Button( typeof( CreateShaftCommand ), OnlyInitialized = true )]
        private static class CreateShaftCommandButton { }

        [Button( typeof(CreateArentShaftCommand), OnlyInitialized = true )]
        private static class CreateArentShaftCommandButton { }
        
        [Button( typeof(CreateCylindricalShaftCommand), OnlyInitialized = true )]
        private static class CreateCylindricalShaftCommandButton { }
      }
      [Panel( "arent3d.architecture.routing.monitor", TitleKey = "Electrical.App.Panels.Routing.Monitor" )]
      private static class MonitorPanel
      {
        [Button( typeof( MonitorSelectionCommand ), AvailabilityType = typeof( Commands.Enabler.MonitorSelectionCommandEnabler ) )]
        private static class MonitorSelectionCommandButton { }
      }

      [Panel( "arent3d.architecture.routing.demo", TitleKey = "Electrical.App.Panels.Routing.Demo" )]
      private static class DemoPanel
      {
        [Button( typeof( Demo_DeleteAllRoutedElements ) )]
        private static class Demo_DeleteAllRoutedElementsCommandButton { }
      }
    }
  }
}
