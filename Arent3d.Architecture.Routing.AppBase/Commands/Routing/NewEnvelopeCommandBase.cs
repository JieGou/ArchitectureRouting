using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewEnvelopeCommandBase : IExternalCommand
  {

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var (originX, originY, _) = uiDocument.Selection.PickPoint( "Envelopeの配置場所を選択して下さい。" ) ;

        var result = document.Transaction(
          "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Pipe Spaces" ), _ =>
          {
            GenerateEnvelope( document, originX, originY, uiDocument.ActiveView.GenLevel ) ;

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

    private static void GenerateEnvelope( Document document, double originX, double originY, Level level )
    {
      var symbol = document.GetFamilySymbol( RoutingFamilyType.Envelope )! ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, 0 ), level, StructuralType.NonStructural ) ;
      instance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;

      //Find above level
      var levels = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ) ;
      var aboveLevel = levels.Last() ;
      if ( levels.Any() ) {
        for ( int i = 0 ; i < levels.Count() ; i++ ) {
          if ( levels.ElementAt( i ).Id == level.Id ) {
            aboveLevel = levels.ElementAt( i + 1 ) ;
            break ;
          }
        }
      }

      //Set Envelope Height
      var height = aboveLevel.Elevation - level.Elevation ;
      instance.LookupParameter( "高さ" ).Set( height ) ;
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