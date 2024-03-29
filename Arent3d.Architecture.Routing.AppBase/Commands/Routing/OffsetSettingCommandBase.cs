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
          using var progress = ProgressBar.ShowWithNewThread( commandData.Application ) ;
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
        if ( string.IsNullOrEmpty( envelop.GetParentEnvelopeId() ) ) {
          var childrenEnvelope = familyInstances.FirstOrDefault( f => f.GetParentEnvelopeId() == envelop.UniqueId ) ;
          // Create new envelop
          GenerateEnvelope( document, envelop, uiDocument.ActiveView.GenLevel, settingStorable.OffsetSettingsData.Offset.MillimetersToRevitUnits(), childrenEnvelope ) ;
        }
      }
    }

    private static void SaveSetting( StorableBase newSettings )
    {
      newSettings.Save() ;
    }

    private static void GenerateEnvelope( Document document, Element envelope, Level level, double offset, Element? childrenEnvelop )
    {
      // Get parent position
      double originX = 0 ;
      double originY = 0 ;
      double originZ = 0 ;
      if ( envelope?.Location is LocationPoint location ) {
        originX = location.Point.X ;
        originY = location.Point.Y ;
        var zOffset = level != null ? offset + level.Elevation : offset ;
        originZ = location.Point.Z - zOffset ;
      }

      var backSize = envelope == null ? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Length".GetDocumentStringByKeyOrDefault( document, "奥行き" ) ).AsDouble() + 2 * offset ;
      var widthSize = envelope == null ? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Width".GetDocumentStringByKeyOrDefault( document, "幅" ) ).AsDouble() + 2 * offset ;
      var height = envelope == null ? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Height".GetDocumentStringByKeyOrDefault( document, "高さ" ) ).AsDouble() + 2 * offset ;
      var parentEnvelopeId = envelope == null ? string.Empty : envelope.UniqueId ;

      // Create new envelope
      if ( childrenEnvelop == null ) {
        var symbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new System.InvalidOperationException() ;
        var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level!, StructuralType.NonStructural ) ;

        // Change envelope size
        instance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
        instance.LookupParameter( "奥行き" ).Set( backSize ) ;
        instance.LookupParameter( "幅" ).Set( widthSize ) ;
        instance.LookupParameter( "高さ" ).Set( height ) ;
        instance.LookupParameter( "Parent Envelope Id" ).Set( parentEnvelopeId ) ;

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
      // Correct children envelope
      else {
        // Change envelope size
        childrenEnvelop.LookupParameter( "奥行き" ).Set( backSize ) ;
        childrenEnvelop.LookupParameter( "幅" ).Set( widthSize ) ;
        childrenEnvelop.LookupParameter( "高さ" ).Set( height ) ;
      }
    }
  }
}