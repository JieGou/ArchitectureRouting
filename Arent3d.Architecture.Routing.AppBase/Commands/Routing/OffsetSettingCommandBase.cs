using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
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
      //Call Open UI dialog
      var property = ShowDialog( document ) ;
      if ( true != property?.DialogResult ) return Result.Succeeded ;
      var value = property.OffsetNumeric.Value ;      
      try {
        var result = document.Transaction(
          "TransactionName.Commands.Routing.OffsetSetting".GetAppStringByKeyOrDefault( "Offset Setting" ), _ =>
          {
            // get all envelop
            var envelops = document.GetAllFamilyInstances( RoutingFamilyType.Envelope ) ;
            var familyInstances = envelops as FamilyInstance[] ?? envelops.ToArray() ;            
            foreach ( var envelop in familyInstances ) {
                GenerateEnvelope( document, envelop, uiDocument.ActiveView.GenLevel ) ;
            }

            return Result.Succeeded ;
          } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }      
    }

    /// <summary>
    ///   Show dialog Offset Setting
    /// </summary>
    private static OffsetSetting ShowDialog( Document document )
    {
      var sv = new OffsetSetting( document ) ;
      sv.ShowDialog() ;
      return sv ;
    }
    
    public static void GenerateEnvelope( Document document, FamilyInstance envelope, Level level, bool isCeiling = false )
    {
      double originX = 0 ;
      double originY = 0 ;
      double originZ = 0 ;
      if ( envelope?.Location is LocationPoint location ) {
        originX = location.Point.X ;
        originY = location.Point.Y ;
        originZ = location.Point.Z ;
      }
      
      var symbol = document.GetFamilySymbol( RoutingFamilyType.Envelope )! ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      instance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;
      const double offSet = 1000 ;
      var backSize = envelope == null? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Length".GetDocumentStringByKeyOrDefault( document, "奥行き" ) ).AsDouble() + offSet.MillimetersToRevitUnits() ;
      var widthSize = envelope == null? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Width".GetDocumentStringByKeyOrDefault( document, "幅" ) ).AsDouble() + offSet.MillimetersToRevitUnits() ;
      var height = envelope == null? 0 : envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Height".GetDocumentStringByKeyOrDefault( document, "高さ" ) ).AsDouble() + offSet.MillimetersToRevitUnits();
      instance.LookupParameter( "奥行き" ).Set( backSize ) ;
      instance.LookupParameter( "幅" ).Set( widthSize ) ;
      instance.LookupParameter( "高さ" ).Set( height ) ;

      var ogs = new OverrideGraphicSettings() ;
      ogs.SetSurfaceTransparency( 100 ) ;
      document.ActiveView.SetElementOverrides( instance.Id, ogs ) ;   
    }    
  }
}