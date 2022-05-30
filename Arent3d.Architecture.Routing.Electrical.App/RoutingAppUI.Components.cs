using Arent3d.Architecture.Routing.Electrical.App.Commands ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation;
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
        [SplitButton( "arent3d.architecture.routing.init.init", TitleKey = "Electrical.App.Panels.Routing.Initialize.Initialize" )]
        private static class InitSplitButton
        {
          [Button( typeof( InitializeCommand ), InitializeButton = true )]
          private static class InitializeCommandButton { }

          [Button( typeof( ShowRoutingViewsCommand ), OnlyInitialized = true )]
          private static class ShowRoutingViewsCommandButton { }

          [Button( typeof( Show3DViewsCommand ), OnlyInitialized = true )]
          private static class Show3DViewsCommandButton { }

          [Button(typeof(CnsSettingCommand), OnlyInitialized = true)]
          private static class CnsSettingCommandButton { }

          [Button( typeof( ShowCeedModelsCommand ), OnlyInitialized = true )]
          private static class ShowCeedModelsCommandButton { }
          
          [Button( typeof( ShowRegistrationOfBoardDataCommand ), OnlyInitialized = true )]
          private static class RegistrationOfBoardDataCommandButton { }

          [Button( typeof( ChangeFamilyGradeCommand ), OnlyInitialized = true )]
          private static class ChangeFamilyGradeCommandButton { }
          
          [Button( typeof( ShowHeightSettingCommand ), OnlyInitialized = true )]
          private static class ShowHeightSettingCommandButton { }
          
          [Button( typeof( ConfirmUnsetCommand ), OnlyInitialized = true )]
          private static class ConfirmUnsetCommandButton { }
          
          [Button( typeof( ConfirmNotConnectingCommand ), OnlyInitialized = true )]
          private static class ConfirmNotConnectingCommandButton { }
          
          [Button( typeof( SplitScheduleCommand ), OnlyInitialized = true )]
          private static class SplitScheduleCommandButton { }
         
          [Button( typeof( ShowFallMarkCommand ), OnlyInitialized = true )]
          private static class ShowFallMarkCommandButton { }
          
          [Button( typeof( ShowOpenEndPointMarkCommand ), OnlyInitialized = true )]
          private static class ShowOpenEndPointMarkCommandButton { }

          [Button( typeof( MergeSchedulesCommand ), OnlyInitialized = true )]
          private static class MergeSchedulesCommandButton { }

          [Button( typeof( SwitchEcoNormalModeCommand ), OnlyInitialized = true )]
          private static class SwitchEcoNormalModeCommandButton { }

          [Button( typeof( RegisterSymbolCommand ), OnlyInitialized = true )]
          private static class RegisterSymbolCommandButton { }
          
          [Button( typeof( SetupPrintCommand ), OnlyInitialized = true )]
          private static class SetupPrintCommandButton { }
        }

        [SplitButton( "arent3d.architecture.routing.init.pickup", TitleKey = "Electrical.App.Panels.Routing.Initialize.PickUp" )]
        private static class PickupSplitButton
        {
          [Button( typeof( ShowCeedDetailInformationCommand ), OnlyInitialized = true )]
          private static class ShowCeedDetailInformationCommandButton { }

          [Button(typeof(LoadCsvFilesCommand), OnlyInitialized = true)]
          private static class LoadCsvFilesCommandButton { }

          [Button( typeof( CreateDetailTableCommand ), OnlyInitialized = true )]
          private static class CreateDetailTableCommandButton { }

          [Button( typeof( ShowElectricSymbolsCommand ), OnlyInitialized = true )]
          private static class ShowElectricSymbolsCommandButton { }
          
          [Button( typeof( ShowDialogCreateTableByFloorCommand ), OnlyInitialized = true )]
          private static class ShowDialogCreateTableByFloorCommandButton { }

          [Button( typeof( ShowPickUpInformationCommand ), OnlyInitialized = true )]
          private static class ShowPickUpInformationCommandButton { }

          [Button(typeof(PickUpReportCommand), OnlyInitialized = true)]
          private static class PickUpReportCommandButton { }
        
          [Button( typeof( CreateDetailSymbolCommand ), OnlyInitialized = true )]
          private static class CreateDetailSymbolCommandButton { }
          
          [Button( typeof( LoadDrawingCommand ), OnlyInitialized = true )]
          private static class LoadDrawingCommandButton { }
          
          [Button( typeof( SingleBorderCommand ), OnlyInitialized = true )]
          private static class SingleBorderCommandButton { }
        
          [Button( typeof( DoubleBorderCommand ), OnlyInitialized = true )]
          private static class DoubleBorderCommandButton { }
          
          [Button( typeof( CircleAnnotationCommand ), OnlyInitialized = true )]
          private static class CircleAnnotationCommandButton { }
           
		      [Button( typeof( SymbolInformationCommand ), OnlyInitialized = true )]
          private static class SymbolInformationCommandButton { }
        }

        [SplitButton( "arent3d.architecture.routing.init.panels", TitleKey = "Electrical.App.Panels.Routing.Initialize.Panels" )]
        private static class PanelsSplitButton
        {
          [Button( typeof( ShowFrom_ToWindowCommand ), OnlyInitialized = true )]
          private static class ShowFrom_ToWindowCommandButton { }

          [Button( typeof( ShowFromToTreeCommand ), OnlyInitialized = true )]
          private static class ShowFromToTreeCommandButton { }
        }
      }

      [Panel("arent3d.architecture.routing.routing", TitleKey = "Electrical.App.Panels.Routing.Routing" )]
      private static class RoutingPanel
      {
        [SplitButton( "arent3d.architecture.routing.routing.routing", TitleKey = "Electrical.App.Panels.Routing.Routing.Routing" )]
        private static class RoutingSplitButton
        {
          [Button( typeof( PickRoutingCommand ), OnlyInitialized = true )]
          private static class PickRoutingCommandButton { }

          [Button( typeof( ReplaceFromToCommand ), OnlyInitialized = true )]
          private static class ReplaceFromToCommandButton { }

          [Button( typeof( PickAndReRouteCommand ), OnlyInitialized = true )]
          private static class PickAndReRouteCommandButton { }

          [Button( typeof( AllReRouteCommand ), OnlyInitialized = true )]
          private static class AllReRouteCommandButton { }

          [Button( typeof( AllReRouteByFloorCommand ), OnlyInitialized = true )]
          private static class AllReRouteByFloorCommandButton { }
          
          [Button( typeof( RoomPickRoutingCommand ), OnlyInitialized = true )]
          private static class RoomPickRoutingCommandButton { }
          
          [Button( typeof( PullBoxRoutingCommand ), OnlyInitialized = true )]
          private static class PullBoxRoutingCommandButton { }
        }
        
        [SplitButton( "arent3d.architecture.routing.routing.selection.range.routing", TitleKey = "Electrical.App.Panels.Routing.Routing.SelectionRangeRouting" )]
        private static class SelectionRangeRoutingSplitButton
        {
          [Button( typeof( SelectionRangeRouteCommand ), OnlyInitialized = true )]
          private static class SelectionRangeRouteCommandButton { }
          
          [Button( typeof( SelectionRangeRouteWithHeightAdjustmentCommand ), OnlyInitialized = true )]
          private static class SelectionRangeRouteWithHeightAdjustmentCommandButton { }
        
          [Button( typeof( RoomSelectionRangeRouteCommand ), OnlyInitialized = true )]
          private static class RoomSelectionRangeRouteCommandButton { }
        }

        [Button( typeof( InsertPassPointCommand ), OnlyInitialized = true )]
        private static class InsertPassPointCommandButton { }

        [SplitButton( "arent3d.architecture.routing.routing.erase", TitleKey = "Electrical.App.Panels.Routing.Routing.Erase" )]
        private static class EraseSplitButton
        {
          [Button( typeof( EraseSelectedRoutesCommand ), OnlyInitialized = true )]
          private static class EraseSelectedRoutesCommandButton { }

          [Button( typeof( EraseAllRoutesCommand ), OnlyInitialized = true )]
          private static class EraseAllRoutesCommandButton { }
        }
      }

      [Panel("arent3d.architecture.routing.connectors", TitleKey = "Electrical.App.Panels.Routing.Connectors")]
      private static class ConnectorsPanel
      {
        [SplitButton( "arent3d.architecture.routing.connectors.one.side", TitleKey = "Electrical.App.Commands.Routing.NewConnectorCommand" )]
        private static class NewOneSideConnectorButton
        {
          [Button(typeof(NewConnectorCommand), OnlyInitialized = true)]
          private static class NewConnectorCommandButton { }
        
          [Button(typeof(NewConnectorTypePowerCommand), OnlyInitialized = true)]
          private static class NewConnectorTypePowerCommandButton { }
          
          [Button(typeof(NewConnectorTypeSensorCommand), OnlyInitialized = true)]
          private static class NewConnectorTypeSensorCommandButton { }
        }
        [SplitButton( "arent3d.architecture.routing.connectors.two.side", TitleKey = "Electrical.App.Commands.Routing.Connectors.NewConnectorTwoSideCommand" )]
        private static class NewTwoSideConnectorButton
        {
          [Button(typeof(NewTwoSideConnectorCommand), OnlyInitialized = true)]
          private static class NewConnectorBsCommandButton { }
          
          [Button(typeof(NewTwoSideConnectorTypePassCommand), OnlyInitialized = true)]
          private static class NewTwoSideConnectorTypePassCommandButton { }
          
          [Button(typeof(NewTwoSideConnectorTypeSensorCommand), OnlyInitialized = true)]
          private static class NewTwoSideConnectorTypeSensorCommandButton { }
        }
        [SplitButton( "arent3d.architecture.routing.connectors.dumper", TitleKey = "Electrical.App.Panels.Routing.Connectors.Dumper" )]
        private static class EraseSplitButton
        {
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

        [Button( typeof( CreateRoomCommand ), OnlyInitialized = true )]
        private static class CreateRoomCommandButton { }
      }

      [Panel( "arent3d.architecture.routing.rackcommand", TitleKey = "Electrical.App.Panels.Routing.RackCommand" )]
      private static class RackCommandPanel
      {
        [Button( typeof( NewRackCommand ), OnlyInitialized = true )]
        private static class NewRackCommandButton { }

        [Button( typeof( NewRackFromToCommand ), OnlyInitialized = true )]
        private static class NewRackFromToCommandButton { }

        [Button( typeof( NewLimitRackCommand ), OnlyInitialized = true )]
        private static class NewLimitRackCommandButton { }

        [Button( typeof( EraseAllLimitRackCommand ), OnlyInitialized = true )]
        private static class EraseAllLimitRackCommandButton { }
        
        [Button( typeof( AdjustLeaderCommand ), OnlyInitialized = true )]
        private static class AdjustLeaderCommandButton { }
        
        [Button( typeof( ChangeWireSymbolUsingFilterCommand ), OnlyInitialized = true )]
        private static class ChangeWireSymbolUsingFilterCommandButton { }
        
        [Button( typeof( ChangeWireSymbolUsingDetailItemCommand ), OnlyInitialized = true )]
        private static class ChangeWireSymbolUsingDetailCommandButton { }
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
        
        [Button( typeof(AddHSymbolCommand), OnlyInitialized = true )]
        private static class AddHSymbolCommandButton { }
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

      [Panel( "arent3d.architecture.rc.debug", TitleKey = "App.Panels.Rc.Debug" )]
      private static class DebugPanel
      {
        [Button( typeof( UninitializeCommand ), OnlyInitialized = true )]
        private static class UnInitializeCommandButton
        {
        }
      }
    }
  }
}
