using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowFallMarkCommandBase : IExternalCommand
  {
    private const double VerticalOffset = 0.1 ;
    private const string FallMarkTextNoteTypeName = "1.5mm_FallMarkText" ;
    private const string DefaultParentPlumbingType = "E" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction(
          "TransactionName.Commands.Routing.ShowFallMark".GetAppStringByKeyOrDefault( "Show Fall Mark" ), _ =>
          {
            var fallMarkInstances = GetExistedFallMarkInstances( document ) ;
            var fallMarkInstanceIds = fallMarkInstances.Select( instance => instance.Id ).ToList() ;
            if ( fallMarkInstanceIds.Any() ) {
              var fallMarkTextNoteInstanceIds = GetExistedFallMarkTextNoteInstancesIds( document, fallMarkInstances ) ;
              document.Delete( fallMarkInstanceIds ) ; // remove marks are displaying
              document.Delete( fallMarkTextNoteInstanceIds ) ;
            }
            else {
              CreateFallMarkForConduitWithVerticalDirection( document ) ;
            }

            return Result.Succeeded ;
          } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void CreateFallMarkForConduitWithVerticalDirection( Document document )
    {
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var conduitWithZDirection = new List<Conduit>() ;
      foreach ( var conduit in conduits ) {
        var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitPosition.Curve as Line ) ! ;
        var conduitDirection = conduitLine.Direction ;
        if ( conduitDirection.Z is not (1.0 or -1.0) ) continue ;
        if ( ! conduitWithZDirection.Any( item =>
              ( ( item.Location as LocationCurve )!.Curve as Line )!.Origin.IsAlmostEqualTo( conduitLine.Origin ) ) )
          conduitWithZDirection.Add( conduit ) ;
      }

      if ( ! conduitWithZDirection.Any() ) return ;
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ).FirstOrDefault() ??
                   throw new InvalidOperationException() ;

      var detailTableModels = conduitWithZDirection.GetDetailTableItemsFromConduits( document ).ToList() ;

      foreach ( var conduit in conduitWithZDirection ) {
        GenerateFallMarks( document, symbol, conduit, detailTableModels ) ;
      }
    }

    private static void GenerateFallMarks( Document document, FamilySymbol symbol, Conduit conduit,
      IEnumerable<DetailTableItemModel> detailTableItemModels )
    {
      var level = conduit.ReferenceLevel ;
      var height = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() +
                   VerticalOffset ;
      var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
      var conduitLine = ( conduitPosition.Curve as Line ) ! ;

      var fallMarkPoint = new XYZ( conduitLine.Origin.X, conduitLine.Origin.Y, height ) ;
      var fallMarkInstance = symbol.Instantiate( fallMarkPoint, level, StructuralType.NonStructural ) ;

      var routeName = conduit.GetRouteName() ;

      var changePlumbingInformationStorable = document.GetChangePlumbingInformationStorable() ;

      var existingPlumbingInfo =
        changePlumbingInformationStorable.ChangePlumbingInformationModelData.FirstOrDefault( x =>
          x.ConduitId == conduit.UniqueId ) ;


      var detailTableItemModel = detailTableItemModels.FirstOrDefault( dtm => dtm.RouteName == routeName ) ;

      var fallMarkNoteString =
        existingPlumbingInfo != null && ! string.IsNullOrEmpty( existingPlumbingInfo.PlumbingSize )
          ? $"{existingPlumbingInfo.PlumbingType}"
          : $"{detailTableItemModel?.PlumbingType}" ;

      if ( string.IsNullOrEmpty( fallMarkNoteString ) ) 
        fallMarkNoteString = DefaultParentPlumbingType ;

      var fallMarkTextNote = CreateFallMarkNote( document, fallMarkNoteString, fallMarkPoint ) ;
      fallMarkInstance.GetParameter( "FallMarkTextNoteId" )?.Set( fallMarkTextNote.UniqueId ) ;
    }

    private static List<FamilyInstance> GetExistedFallMarkInstances( Document document )
    {
      var fallMarkSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ) ??
                            throw new InvalidOperationException() ;
      return document.GetAllFamilyInstances( fallMarkSymbols ).ToList() ;
    }

    private static List<ElementId> GetExistedFallMarkTextNoteInstancesIds( Document document,
      IEnumerable<Element> existedFallMarkInstancesIds )
    {
      var fallMarkTextNoteInstanceIds = new HashSet<ElementId>() ;

      foreach ( var existedFallMarkInstancesId in existedFallMarkInstancesIds ) {
        var fallMarkTextNoteId =
          existedFallMarkInstancesId.GetParameter( "FallMarkTextNoteId" )?.AsValueString() ;
        var existedFallMarkTextNote = document.GetElement( fallMarkTextNoteId ) ;
        if ( existedFallMarkTextNote != null ) {
          fallMarkTextNoteInstanceIds.Add( existedFallMarkTextNote.Id ) ;
        }
      }

      return fallMarkTextNoteInstanceIds.ToList() ;
    }

    private static TextNote CreateFallMarkNote( Document document, string fallMarkNote, XYZ fallMarkPoint )
    {
      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var fallMarkTextNoteType = GetTextNoteTypeForFallMarkNote( document ) ;
      TextNoteOptions opts = new(fallMarkTextNoteType.Id) { HorizontalAlignment = HorizontalTextAlignment.Left } ;
      var txtPosition =
        new XYZ( fallMarkPoint.X + .6 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification,
          fallMarkPoint.Y - 0.2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification,
          fallMarkPoint.Z ) ;

      var textNote = TextNote.Create( document, document.ActiveView.Id, txtPosition, fallMarkNote, opts ) ;

      return textNote ;
    }

    private static TextNoteType GetTextNoteTypeForFallMarkNote( Document doc )
    {
      var fallMarkTextNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) )
        .WhereElementIsElementType().Cast<TextNoteType>()
        .FirstOrDefault( tt => Equals( FallMarkTextNoteTypeName, tt.Name ) ) ;

      if ( fallMarkTextNoteType != null ) return fallMarkTextNoteType ;

      var defaultTextNoteId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      var defaultTextNoteType = doc.GetElement( defaultTextNoteId ) as TextNoteType ;

      if ( defaultTextNoteType == null ) {
        throw new NullReferenceException( "can not find default text note type!" ) ;
      }

      var elementType = defaultTextNoteType.Duplicate( FallMarkTextNoteTypeName ) ;
      fallMarkTextNoteType = elementType as TextNoteType ;
      fallMarkTextNoteType?.GetParameter( BuiltInParameter.TEXT_SIZE )?.Set( 1.5.MillimetersToRevitUnits() ) ;
      fallMarkTextNoteType?.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
      fallMarkTextNoteType?.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 1 ) ;
      fallMarkTextNoteType?.get_Parameter( BuiltInParameter.LINE_COLOR )
        .Set( ParamUtils.ToColorParameterValue( 255, 128, 64 ) ) ;

      return fallMarkTextNoteType ?? throw new InvalidOperationException() ;
    }

    public static void RemoveDisplayingFallMark(Document document)
    {
      var fallMarkInstances = GetExistedFallMarkInstances( document ) ;
      var fallMarkInstanceIds = fallMarkInstances.Select( instance => instance.Id ).ToList() ;
      if ( fallMarkInstanceIds.Any() ) {
        var fallMarkTextNoteInstanceIds = GetExistedFallMarkTextNoteInstancesIds( document, fallMarkInstances ) ;
        document.Delete( fallMarkInstanceIds ) ; // remove marks are displaying
        document.Delete( fallMarkTextNoteInstanceIds ) ;
      }
    }
  }
}