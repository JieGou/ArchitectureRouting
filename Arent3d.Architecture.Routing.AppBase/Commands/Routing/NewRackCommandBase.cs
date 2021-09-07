using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.Csv ;
using Arent3d.Architecture.Routing.AppBase.Forms ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewRackCommandBase : IExternalCommand
  {
    private static readonly double DefaultThickness = 200.0 ;
    private static readonly double DefaultWidth = 200.0 ;
    private static readonly double DefaultHeight = 4000.0 ;
    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var pickFrom = PointOnRoutePicker.PickPointOnRoute( uiDocument, true,
          "Dialog.Commands.PassPoint.Insert.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;

        var pickTo = PointOnRoutePicker.PickPointOnRoute( uiDocument, true,
          "Dialog.Commands.PassPoint.Insert.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;

        if ( null == pickFrom.Position || null == pickTo.Position || null == pickFrom.Direction ||
             null == pickTo.Direction ) {
          return Result.Failed ;
        }

        var sv = new SetRackProperty() ;
        sv.UpdateParameters( DefaultHeight, DefaultThickness ) ;
        sv.ShowDialog() ;
        if ( true != sv.DialogResult ) {
          return Result.Failed ;
        }

        var result = document.Transaction(
          "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Rack" ), _ =>
          {
            var symbol = uiDocument.Document.GetFamilySymbol( RoutingFamilyType.RackGuide )! ;

            // TODO Calc Rack Direction base on Pipe Direction

            var instance =
              symbol.Instantiate(
                new XYZ( pickFrom.Position.X, pickFrom.Position.Y, sv.FixedHeight.MillimetersToRevitUnits() ),
                uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;
            var distance = pickFrom.Position.DistanceTo( pickTo.Position ) ;

            var currentLength = LengthParameterData.From( instance, "Arent-Length" ) ;

            instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;

            SetParameter( instance, "幅", DefaultWidth.MillimetersToRevitUnits() ) ;
            SetParameter( instance, "高さ", sv.FixedThickness.MillimetersToRevitUnits() ) ;
            SetParameter( instance, "奥行き", distance ) ;
            SetParameter( instance, "Arent-Offset", 0 ) ;

            SetParameter( instance, BuiltInParameter.INSTANCE_ELEVATION_PARAM, 0 ) ;
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

    private static void GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ,
      double sizeX, double sizeY, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbol( RoutingFamilyType.RackGuide )! ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }

    private static Level? GetUpperLevel( Level refRevel )
    {
      var minElevation = refRevel.Elevation + refRevel.Document.Application.ShortCurveTolerance ;
      return refRevel.Document.GetAllElements<Level>().Where( level => minElevation < level.Elevation )
        .MinItemOrDefault( level => level.Elevation ) ;
    }

    private static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }

    private static void SetParameter( FamilyInstance instance, BuiltInParameter parameter, double value )
    {
      instance.get_Parameter( parameter )?.Set( value ) ;
    }
  }
}