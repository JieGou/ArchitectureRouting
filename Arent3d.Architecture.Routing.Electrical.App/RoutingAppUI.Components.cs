using Arent3d.Architecture.Routing.Electrical.App.Commands ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Demo ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PassPoint ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Rack ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Shaft ;
using Arent3d.Revit.UI.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  partial class RoutingAppUI
  {
    [Tab( "Electrical.App.Routing.TabName", VisibilityMode = TabVisibilityMode.NormalDocument )]
    private static class RoutingTab
    {
      [Panel( "arent3d.architecture.routing.initialize", TitleKey = "Electrical.App.Panels.Routing.Initialize" )]
      private static class InitPanel
      {
        [SplitButton( "arent3d.architecture.routing.initialize.initialize", TitleKey = "Electrical.App.Panels.Routing.Initialize.Initialize" )]
        private static class InitSplitButton
        {
          [Button( typeof( InitializeCommand ), InitializeButton = true )]
          private static class InitializeCommandButton
          {
          }

          [Button( typeof( ShowRoutingViewsCommand ), OnlyInitialized = true )]
          private static class ShowRoutingViewsCommandButton
          {
          }
        }

        [SplitButton( "arent3d.architecture.routing.initialize.panels", TitleKey = "Electrical.App.Panels.Routing.Initialize.Panels" )]
        private static class PanelsSplitButton
        {
          [Button( typeof( ShowFrom_ToWindowCommand ), OnlyInitialized = true )]
          private static class ShowFrom_ToWindowCommandButton
          {
          }

          [Button( typeof( ShowFromToTreeCommand ), OnlyInitialized = true )]
          private static class ShowFromToTreeCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.settings", TitleKey = "Electrical.App.Panels.Routing.Settings" )]
      private static class Settings
      {
        [SplitButton( "arent3d.architecture.routing.settings.all", TitleKey = "Electrical.App.Panels.Routing.Settings.All" )]
        private static class AllSettings
        {
          [Button( typeof( LoadCsvFilesCommand ), OnlyInitialized = true )]
          private static class LoadCsvFilesCommandButton
          {
          }

          [Button( typeof( RegisterSymbolCommand ), OnlyInitialized = true )]
          private static class RegisterSymbolCommandButton
          {
          }

          [Button( typeof( CnsSettingCommand ), OnlyInitialized = true )]
          private static class CnsSettingCommandButton
          {
          }

          [Button( typeof( DefaultSettingCommand ), OnlyInitialized = true )]
          private static class DefaultSettingCommandButton
          {
          }

          [Button( typeof( Show3DViewsCommand ), OnlyInitialized = true )]
          private static class Show3DViewsCommandButton
          {
          }

          [Button( typeof( ChangeFamilyGradeCommand ), OnlyInitialized = true )]
          private static class ChangeFamilyGradeCommandButton
          {
          }

          [Button( typeof( SetupPrintCommand ), OnlyInitialized = true )]
          private static class SetupPrintCommandButton
          {
          }

          [Button( typeof( SwitchEcoNormalModeCommand ), OnlyInitialized = true )]
          private static class SwitchEcoNormalModeCommandButton
          {
          }

          [Button( typeof( LoadDrawingCommand ), OnlyInitialized = true )]
          private static class LoadDrawingCommandButton
          {
          }

          [Button( typeof( ShowHeightSettingCommand ), OnlyInitialized = true )]
          private static class ShowHeightSettingCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.confirmation", TitleKey = "Electrical.App.Panels.Routing.Confirmation" )]
      private static class ConfirmationButton
      {
        [SplitButton( "arent3d.architecture.routing.confirmation.all", TitleKey = "Electrical.App.Panels.Routing.Confirmation.All" )]
        private static class AllConfirmationButton
        {
          [Button( typeof( ConfirmUnsetCommand ), OnlyInitialized = true )]
          private static class ConfirmUnsetCommandButton
          {
          }

          [Button( typeof( ConfirmNotConnectingCommand ), OnlyInitialized = true )]
          private static class ConfirmNotConnectingCommandButton
          {
          }

          [Button( typeof( ShowFallMarkCommand ), OnlyInitialized = true )]
          private static class ShowFallMarkCommandButton
          {
          }

          [Button( typeof( ShowOpenEndPointMarkCommand ), OnlyInitialized = true )]
          private static class ShowOpenEndPointMarkCommandButton
          {
          }

          [Button( typeof( ShowCeedDetailInformationCommand ), OnlyInitialized = true )]
          private static class ShowCeedDetailInformationCommandButton
          {
          }

          [Button( typeof( ElectricalSymbolAggregationCommand ), OnlyInitialized = true )]
          private static class ElectricalSymbolAggregationCommandButton
          {
          }

          [Button( typeof( ElectricalSymbolAggregationAllCommand ), OnlyInitialized = true )]
          private static class ElectricalSymbolAggregationAllCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.drawing", TitleKey = "Electrical.App.Panels.Routing.Drawing" )]
      private static class Drawing
      {
        [SplitButton( "arent3d.architecture.routing.drawing.symbols", TitleKey = "Electrical.App.Panels.Routing.Drawing.Symbols" )]
        private static class Symbols
        {
          [Button( typeof( CreateDetailSymbolCommand ), OnlyInitialized = true )]
          private static class CreateDetailSymbolCommandButton
          {
          }

          [Button( typeof( SymbolInformationCommand ), OnlyInitialized = true )]
          private static class SymbolInformationCommandButton
          {
          }

          [Button( typeof( ShowCeedModelsCommand ), OnlyInitialized = true )]
          private static class ShowCeedModelsCommandButton
          {
          }

          [Button( typeof( ShowRegistrationOfBoardDataCommand ), OnlyInitialized = true )]
          private static class RegistrationOfBoardDataCommandButton
          {
          }

          [Button( typeof( SingleBorderCommand ), OnlyInitialized = true )]
          private static class SingleBorderCommandButton
          {
          }

          [Button( typeof( DoubleBorderCommand ), OnlyInitialized = true )]
          private static class DoubleBorderCommandButton
          {
          }

          [Button( typeof( CircleAnnotationCommand ), OnlyInitialized = true )]
          private static class CircleAnnotationCommandButton
          {
          }

          [Button( typeof( ExportDwgCommand ), OnlyInitialized = true )]
          private static class ExportDWGCommandButton
          {
          }
        }

        [SplitButton( "arent3d.architecture.routing.drawing.rack", TitleKey = "Electrical.App.Panels.Routing.Drawing.Rack" )]
        private static class Rack
        {
          [Button( typeof( NewLimitRackCommand ), OnlyInitialized = true )]
          private static class NewLimitRackCommandButton
          {
          }
          
          [Button( typeof( NewLimitRackCircleCommand ), OnlyInitialized = true )]
          private static class NewLimitRackCircleCommandButton
          {
          }
          
          

          [Button( typeof( NewRackCommand ), OnlyInitialized = true )]
          private static class NewRackCommandButton
          {
          }

          [Button( typeof( NewRackFromToCommand ), OnlyInitialized = true )]
          private static class NewRackFromToCommandButton
          {
          }

          [Button( typeof( EraseAllLimitRackCommand ), OnlyInitialized = true )]
          private static class EraseAllLimitRackCommandButton
          {
          }
          
          [Button( typeof( EraseSelectedLimitRacksCommand ), OnlyInitialized = true )]
          private static class EraseSelectedLimitRacksCommandButton
          {
          }
          
          [Button( typeof( RackSpaceCommand ), OnlyInitialized = true )]
          private static class RackGuidCommandButton
          {
          }

          [Button( typeof( AdjustLeaderCommand ), OnlyInitialized = true )]
          private static class AdjustLeaderCommandButton
          {
          }
        }

        [SplitButton( "arent3d.architecture.routing.drawing.shafts", TitleKey = "Electrical.App.Panels.Routing.Drawing.Shafts" )]
        private static class ShaftPanel
        {
          [Button( typeof( CreateShaftCommand ), OnlyInitialized = true )]
          private static class CreateShaftCommandButton
          {
          }

          [Button( typeof( CreateArentShaftCommand ), OnlyInitialized = true )]
          private static class CreateArentShaftCommandButton
          {
          }

          [Button( typeof( CreateCylindricalShaftCommand ), OnlyInitialized = true )]
          private static class CreateCylindricalShaftCommandButton
          {
          }

          [Button( typeof( AddHSymbolCommand ), OnlyInitialized = true )]
          private static class AddHSymbolCommandButton
          {
          }
        }

        [SplitButton( "arent3d.architecture.routing.drawing.envelope", TitleKey = "Electrical.App.Panels.Routing.Drawing.Envelope" )]
        private static class EnvelopPanel
        {
          [Button( typeof( NewEnvelopeCommand ), OnlyInitialized = true )]
          private static class NewEnvelopeCommandButton
          {
          }

          [Button( typeof( CeilingEnvelopeCommand ), OnlyInitialized = true )]
          private static class CeilingEnvelopeCommandButton
          {
          }

          [Button( typeof( OffsetSettingCommand ), OnlyInitialized = true )]
          private static class OffsetSettingCommandButton
          {
          }

          [Button( typeof( CreateRoomCommand ), OnlyInitialized = true )]
          private static class CreateRoomCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.table", TitleKey = "Electrical.App.Panels.Routing.Table" )]
      private static class TablePanel
      {
        [SplitButton( "arent3d.architecture.routing.table.all", TitleKey = "Electrical.App.Panels.Routing.Table.All" )]
        private static class AllTabels
        {
          [Button( typeof( ShowPickUpInformationCommand ), OnlyInitialized = true )]
          private static class ShowPickUpInformationCommandButton
          {
          }

          [Button( typeof( PickUpReportCommand ), OnlyInitialized = true )]
          private static class PickUpReportCommandButton
          {
          }

          [Button( typeof( CreateDetailTableCommand ), OnlyInitialized = true )]
          private static class CreateDetailTableCommandButton
          {
          }

          [Button( typeof( ShowElectricSymbolsCommand ), OnlyInitialized = true )]
          private static class ShowElectricSymbolsCommandButton
          {
          }

          [Button( typeof( ShowDialogCreateTableByFloorCommand ), OnlyInitialized = true )]
          private static class ShowDialogCreateTableByFloorCommandButton
          {
          }

          [Button( typeof( SplitScheduleCommand ), OnlyInitialized = true )]
          private static class SplitScheduleCommandButton
          {
          }

          [Button( typeof( MergeSchedulesCommand ), OnlyInitialized = true )]
          private static class MergeSchedulesCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.routing", TitleKey = "Electrical.App.Panels.Routing.Routing" )]
      private static class RoutingPanel
      {
        [SplitButton( "arent3d.architecture.routing.routing.routing", TitleKey = "Electrical.App.Panels.Routing.Routing.Routing" )]
        private static class RoutingSplitButton
        {
          [Button( typeof( PickRoutingCommand ), OnlyInitialized = true )]
          private static class PickRoutingCommandButton
          {
          }

          [Button( typeof( ReplaceFromToCommand ), OnlyInitialized = true )]
          private static class ReplaceFromToCommandButton
          {
          }

          [Button( typeof( PickAndReRouteCommand ), OnlyInitialized = true )]
          private static class PickAndReRouteCommandButton
          {
          }

          [Button( typeof( AllReRouteCommand ), OnlyInitialized = true )]
          private static class AllReRouteCommandButton
          {
          }

          [Button( typeof( AllReRouteByFloorCommand ), OnlyInitialized = true )]
          private static class AllReRouteByFloorCommandButton
          {
          }

          [Button( typeof( RoomPickRoutingCommand ), OnlyInitialized = true )]
          private static class RoomPickRoutingCommandButton
          {
          }

          [Button( typeof( LeakRoutingCommand ), OnlyInitialized = true )]
          private static class LeakRoutingCommandCommandButton
          {
          }

          [Button( typeof( PullBoxRoutingCommand ), OnlyInitialized = true )]
          private static class PullBoxRoutingCommandButton
          {
          }
          
          [Button( typeof( PressureGuidingTubeCommand ), OnlyInitialized = true )]
          private static class PressureGuidingTubeButton { }
        }

        [SplitButton( "arent3d.architecture.routing.routing.selectionRangeRouting", TitleKey = "Electrical.App.Panels.Routing.Routing.SelectionRangeRouting" )]
        private static class SelectionRangeRoutingSplitButton
        {
          [Button( typeof( SelectionRangeRouteCommand ), OnlyInitialized = true )]
          private static class SelectionRangeRouteCommandButton
          {
          }

          [Button( typeof( SelectionRangeRouteWithHeightAdjustmentCommand ), OnlyInitialized = true )]
          private static class SelectionRangeRouteWithHeightAdjustmentCommandButton
          {
          }

          [Button( typeof( RoomSelectionRangeRouteCommand ), OnlyInitialized = true )]
          private static class RoomSelectionRangeRouteCommandButton
          {
          }
        }

        [Button( typeof( InsertPassPointCommand ), OnlyInitialized = true )]
        private static class InsertPassPointCommandButton
        {
        }

        [SplitButton( "arent3d.architecture.routing.routing.erase", TitleKey = "Electrical.App.Panels.Routing.Routing.Erase" )]
        private static class EraseSplitButton
        {
          [Button( typeof( EraseSelectedRoutesCommand ), OnlyInitialized = true )]
          private static class EraseSelectedRoutesCommandButton
          {
          }

          [Button( typeof( EraseAllRoutesCommand ), OnlyInitialized = true )]
          private static class EraseAllRoutesCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.connectors", TitleKey = "Electrical.App.Panels.Routing.Connectors" )]
      private static class ConnectorsPanel
      {
        [SplitButton( "arent3d.architecture.routing.connectorsOneSide", TitleKey = "Electrical.App.Panels.Routing.Connectors.ConnectorsOneSide" )]
        private static class NewOneSideConnectorButton
        {
          [Button( typeof( NewConnectorCommand ), OnlyInitialized = true )]
          private static class NewConnectorCommandButton
          {
          }

          [Button( typeof( NewConnectorTypePowerCommand ), OnlyInitialized = true )]
          private static class NewConnectorTypePowerCommandButton
          {
          }

          [Button( typeof( NewConnectorTypeSensorCommand ), OnlyInitialized = true )]
          private static class NewConnectorTypeSensorCommandButton
          {
          }
        }

        [SplitButton( "arent3d.architecture.routing.connectorsTwoSide", TitleKey = "Electrical.App.Panels.Routing.Connectors.ConnectorsTwoSide" )]
        private static class NewTwoSideConnectorButton
        {
          [Button( typeof( NewTwoSideConnectorCommand ), OnlyInitialized = true )]
          private static class NewConnectorBsCommandButton
          {
          }

          [Button( typeof( NewTwoSideConnectorTypePassCommand ), OnlyInitialized = true )]
          private static class NewTwoSideConnectorTypePassCommandButton
          {
          }

          [Button( typeof( NewTwoSideConnectorTypeSensorCommand ), OnlyInitialized = true )]
          private static class NewTwoSideConnectorTypeSensorCommandButton
          {
          }
        }

        [SplitButton( "arent3d.architecture.routing.connectors.dumper", TitleKey = "Electrical.App.Panels.Routing.Connectors.Dumper" )]
        private static class EraseSplitButton
        {
          [Button( typeof( NewDamperActuatorCommand ), OnlyInitialized = true )]
          private static class NewDamperActuatorCommandButton
          {
          }

          [Button( typeof( NewElectricTwoWayValveWithLogoCommand ), OnlyInitialized = true )]
          private static class NewElectricTwoWayValveWithLogoCommandButton
          {
          }

          [Button( typeof( NewElectricTwoWayValveWithoutLogoCommand ), OnlyInitialized = true )]
          private static class NewElectricTwoWayValveWithoutLogoCommandButton
          {
          }

          [Button( typeof( NewHumiditySensorForDuctWithLogoCommand ), OnlyInitialized = true )]
          private static class NewHumiditySensorForDuctWithLogoCommandButton
          {
          }

          [Button( typeof( NewHumiditySensorForDuctWithoutLogoCommand ), OnlyInitialized = true )]
          private static class NewHumiditySensorForDuctWithoutLogoCommandButton
          {
          }

          [Button( typeof( NewIndoorHumiditySensorWithLogoCommand ), OnlyInitialized = true )]
          private static class NewIndoorHumiditySensorWithLogoCommandButton
          {
          }

          [Button( typeof( NewIndoorHumiditySensorWithoutLogoCommand ), OnlyInitialized = true )]
          private static class NewIndoorHumiditySensorWithoutLogoCommandButton
          {
          }
        }
      }

      [Panel( "arent3d.architecture.routing.racks", TitleKey = "Electrical.App.Panels.Routing.Racks" )]
      private static class RackPanel
      {
        [Button( typeof( ImportRacksCommand ), OnlyInitialized = true )]
        private static class ImportRacksCommandButton
        {
        }

        [Button( typeof( ExportRacksCommand ), OnlyInitialized = true )]
        private static class ExportRacksCommandButton
        {
        }

        [Button( typeof( EraseAllRacksCommand ), OnlyInitialized = true )]
        private static class EraseAllRacksCommandButton
        {
        }
      }


      [Panel( "arent3d.architecture.routing.lineCommand", TitleKey = "Electrical.App.Panels.Routing.LineCommand" )]
      private static class LineCommandPanel
      {
        [Button( typeof( ChangeWireSymbolUsingFilterCommand ), OnlyInitialized = true )]
        private static class ChangeWireSymbolUsingFilterCommandButton
        {
        }

        [Button( typeof( ChangeWireSymbolUsingDetailItemCommand ), OnlyInitialized = true )]
        private static class ChangeWireSymbolUsingDetailCommandButton
        {
        }
      }

      [Panel( "arent3d.architecture.routing.monitor", TitleKey = "Electrical.App.Panels.Routing.Monitor" )]
      private static class MonitorPanel
      {
        [Button( typeof( MonitorSelectionCommand ), AvailabilityType = typeof( Commands.Enabler.MonitorSelectionCommandEnabler ) )]
        private static class MonitorSelectionCommandButton
        {
        }
      }

      [Panel( "arent3d.architecture.routing.demo", TitleKey = "Electrical.App.Panels.Routing.Demo" )]
      private static class DemoPanel
      {
        [Button( typeof( DemoDeleteAllRoutedElements ) )]
        private static class DemoDeleteAllRoutedElementsCommandButton { }
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