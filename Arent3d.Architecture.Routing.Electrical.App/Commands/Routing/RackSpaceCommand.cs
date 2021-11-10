using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  public class RackSpacePickRange
  {
    public XYZ Point1 { get ; }
    public XYZ Point2 { get ; }

    public RackSpacePickRange( XYZ p1, XYZ p2 )
    {
      Point1 = p1 ;
      Point2 = p2 ;
    }
  }

  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.RackSpaceCommand", DefaultString = "Rack Space" )]
  [Image( "resources/PickFrom-To.png" )]
  public class RackSpaceCommand : RoutingExternalAppCommandBase<RackSpacePickRange>
  {
    protected override string GetTransactionName() => "TransactionName.Commands.Routing.RackSpace".GetAppStringByKeyOrDefault(" ") ;

    protected override OperationResult<RackSpacePickRange> OperateUI( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiApp = commandData.Application ;
      var uiDocument = uiApp.ActiveUIDocument ;
      var selection = uiDocument.Selection ;

      var firstPoint = selection.PickPoint( "Pick the first point" ) ;

      using var rect = new RectangleExternal( uiApp ) ;
      rect.DrawingServer.BasePoint = firstPoint ;
      rect.DrawExternal() ;

      XYZ secondPoint ;
      do {
        secondPoint = selection.PickPoint( "Pick the second point" ) ;
      } while ( secondPoint.DistanceTo( firstPoint ) <= 0.1 ) ;

      return new RackSpacePickRange( firstPoint, secondPoint ) ;
    }

    protected override Result Execute( Document document, TransactionWrapper transaction, RackSpacePickRange range )
    {
      var (x1, y1, z1) = range.Point1 ;
      var (x2, y2, _) = range.Point2 ;
      
      var heightSetting = document.GetHeightSettingStorable() ;
      var levels = heightSetting.Levels.OrderBy( x => x.Elevation ).ToList() ;
      var level = levels.LastOrDefault( x => x.Elevation <= z1 ) ?? levels.First() ?? Level.Create( document, 0.0 ) ;
      var levelSetting = heightSetting[ level ] ;

      var positionMax = ( levelSetting.Elevation + levelSetting.HeightOfLevel ).MillimetersToRevitUnits() ;
      var positionMin = positionMax - 100.0.MillimetersToRevitUnits() ;

      var position = new XYZ( ( x1 + x2 ) / 2, ( y1 + y2 ) / 2, positionMin ) ;
      var xWidth = Math.Abs( x1 - x2 ) ;
      var yWidth = Math.Abs( y1 - y2 ) ;
      var zWidth = positionMax - positionMin ;

      var familyInstance = document.AddRackSpace( position, level ) ;
      if ( xWidth < yWidth ) {
        ElementTransformUtils.RotateElement( document, familyInstance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), Math.PI / 2 ) ;
        SetSize( familyInstance, yWidth, xWidth, zWidth ) ;
      }
      else {
        SetSize( familyInstance, xWidth, yWidth, zWidth ) ;
      }

      return Result.Succeeded ;
    }

    private static void SetSize( FamilyInstance familyInstance, double xWidth, double yWidth, double zWidth )
    {
      var document = familyInstance.Document ;

      SetValue( familyInstance, "Revit.Property.Builtin.Width".GetDocumentStringByKeyOrDefault( document, null ), xWidth ) ;
      SetValue( familyInstance, "Revit.Property.Builtin.Length".GetDocumentStringByKeyOrDefault( document, null ), yWidth ) ;
      SetValue( familyInstance, "Revit.Property.Builtin.Height".GetDocumentStringByKeyOrDefault( document, null ), zWidth ) ;
      SetValue( familyInstance, "Arent-Offset" , 0 ) ;
    }

    private static void SetValue( FamilyInstance familyInstance, string parameterName, double value )
    {
      if ( familyInstance.LookupParameter( parameterName ) is not { } parameter ) return ;
      if ( StorageType.Double != parameter.StorageType ) return ;

      parameter.Set( value ) ;
    }
  }
}