using System.Linq ;
using System.Threading ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class OffsetSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // Get data of offset setting from snoop DB
      OffsetSettingStorable settingStorable = document.GetOffsetSettingStorable() ;

      var viewModel = new ViewModel.OffsetSettingViewModel( settingStorable ) ;
      var dialog = new OffsetSettingDialog( viewModel ) ;
      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        return document.Transaction( "TransactionName.Commands.Routing.OffsetSetting".GetAppStringByKeyOrDefault( "Offset Setting" ), _ =>
        {
          var newStorage = viewModel.SettingStorable ;
          var tokenSource = new CancellationTokenSource() ;
          using var progress = ProgressBar.ShowWithNewThread( tokenSource ) ;
          progress.Message = "Offset Setting..." ;

          using ( var p = progress?.Reserve( 0.5 ) ) {
            ApplySetting( uiDocument, newStorage, p ) ;
          }

          using ( progress?.Reserve( 0.5 ) ) {
            SaveSetting( settingStorable ) ;
          }

          return Result.Succeeded ;
        } ) ;
      }

      return Result.Cancelled ;
    }

    private static void ApplySetting( UIDocument uiDocument, OffsetSettingStorable settingStorable, IProgressData? progressData = null )
    {
      var document = uiDocument.Document ;

      // Get all envelop
      var envelops = document.GetAllFamilyInstances( RoutingFamilyType.Envelope ) ;
      var familyInstances = envelops as FamilyInstance[] ?? envelops.ToArray() ;
      foreach ( var envelop in familyInstances ) {
        // Create new envelop
        GenerateEnvelope( document, envelop, uiDocument.ActiveView.GenLevel, settingStorable.OffsetSettingsData.Offset ) ;
      }
    }

    private static void SaveSetting( StorableBase newSettings )
    {
      newSettings.Save() ;
    }

    private static void GenerateEnvelope( Document document, Element envelope, Level level, double offset )
    {
      // Get parent position
      double originX = 0 ;
      double originY = 0 ;
      double originZ = 0 ;
      if ( envelope?.Location is LocationPoint location ) {
        originX = location.Point.X ;
        originY = location.Point.Y ;
        var zOffset = level != null ? offset.MetersToRevitUnits() - level.Elevation : location.Point.Z - offset.MetersToRevitUnits() ;
        originZ = location.Point.Z - zOffset ;
      }

      // Create new envelope
      var symbol = document.GetFamilySymbol( RoutingFamilyType.Envelope )! ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level!, StructuralType.NonStructural ) ;

      // Change envelope size
      instance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
      var backSize = envelope == null ? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Length".GetDocumentStringByKeyOrDefault( document, "奥行き" ) ).AsDouble() + 2 * offset.MetersToRevitUnits() ;
      var widthSize = envelope == null ? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Width".GetDocumentStringByKeyOrDefault( document, "幅" ) ).AsDouble() + 2 * offset.MetersToRevitUnits() ;
      var height = envelope == null ? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Height".GetDocumentStringByKeyOrDefault( document, "高さ" ) ).AsDouble() + 2 * offset.MetersToRevitUnits() ;
      instance.LookupParameter( "奥行き" ).Set( backSize ) ;
      instance.LookupParameter( "幅" ).Set( widthSize ) ;
      instance.LookupParameter( "高さ" ).Set( height ) ;

      // Change transparency value for all view
      var ogs = new OverrideGraphicSettings() ;
      ogs.SetSurfaceTransparency( 100 ) ;
      var allView = document.GetAllElements<View>() ;
      foreach ( var view in allView ) {
        try {
          view.SetElementOverrides( instance.Id, ogs ) ;
        }
        catch {
          // Todo catch handle
        }
      }
    }
  }
}