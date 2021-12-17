using System ;
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
    private const double EnvelopHeightPlus = 1000;
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

    public static void GenerateEnvelope( Document document, double originX, double originY, Level? level, bool isCeiling = false )
    {
      var levels = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).ToList() ;
      if ( false == levels.Any() ) return ;
      level ??= levels.First() ;

      var heightOfLevel = document.GetHeightSettingStorable()[ level ].HeightOfLevel.MillimetersToRevitUnits() ;
      var symbol = document.GetFamilySymbols( RoutingFamilyType.Envelope ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var instance = isCeiling ? symbol.Instantiate( new XYZ( originX, originY, heightOfLevel ), level, StructuralType.NonStructural ) : symbol.Instantiate( new XYZ( originX, originY, 0 ), level, StructuralType.NonStructural ) ;
      instance.LookupParameter( "Arent-Offset" ).Set( 0.0 ) ;

      //Find above level
      var aboveLevel = levels.Last() ;
      for ( int i = 0 ; i < levels.Count - 1 ; i++ ) {
        if ( levels[ i ].Id == level.Id ) {
          aboveLevel = levels[ i + 1 ] ;
          break ;
        }
      }

      //Set Envelope Height
      double height ;
      if ( isCeiling )
        height = level.Id == aboveLevel.Id ? EnvelopHeightPlus.MillimetersToRevitUnits() : aboveLevel.Elevation - ( heightOfLevel + level.Elevation ) ;
      else
        height = level.Id == aboveLevel.Id ? ( document.GetHeightSettingStorable()[ level ].HeightOfLevel + EnvelopHeightPlus ).MillimetersToRevitUnits() : aboveLevel.Elevation - level.Elevation ;
      instance.LookupParameter( "高さ" ).Set( height ) ;
    }

    private static Level? GetUpperLevel( Level refRevel )
    {
      var minElevation = refRevel.Elevation + refRevel.Document.Application.ShortCurveTolerance ;
      return refRevel.Document.GetAllElements<Level>().Where( level => minElevation < level.Elevation ).MinBy( level => level.Elevation ) ;
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