using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
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
    private const string FallMarkTextNoteTypeName = "1.5mm_ConditionText" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction(
          "TransactionName.Commands.Routing.ShowFallMark".GetAppStringByKeyOrDefault( "Show Fall Mark" ), _ =>
          {
            var fallMarkInstanceIds = GetExistedFallMarkInstancesIds( document ) ;
            if ( fallMarkInstanceIds.Count > 0 ) {
              var fallMarkTextNoteInstanceIds =
                GetExistedFallMarkTextNoteInstancesIds( document, fallMarkInstanceIds ) ;
              document.Delete( fallMarkInstanceIds ) ; // remove marks are displaying
              document.Delete( fallMarkTextNoteInstanceIds ) ;
            }
            else
              CreateFallMarkForConduitWithVerticalDirection( document ) ;
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
        if ( ! conduitWithZDirection.Any( item => ((item.Location as LocationCurve )!.Curve as Line )!.Origin.IsAlmostEqualTo( conduitLine.Origin )))
          conduitWithZDirection.Add( conduit ) ;
      }

      if ( ! conduitWithZDirection.Any() ) return ;
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ).FirstOrDefault() ??
                   throw new InvalidOperationException() ;
      symbol.TryGetProperty( "Lenght", out double lenghtMark ) ;

      var detailTableModels = conduitWithZDirection.GetDetailTableModelsFromConduits( document ) ;
      
      foreach ( var conduit in conduitWithZDirection ) {
        GenerateFallMarks( document, symbol, conduit,detailTableModels ) ;
      }
    }

    private static void GenerateFallMarks( Document document, FamilySymbol symbol, Conduit conduit, IEnumerable<DetailTableModel> detailTableModels )
    {
      var level = conduit.ReferenceLevel ;
      var height = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() + VerticalOffset ;
      var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
      var conduitLine = ( conduitPosition.Curve as Line ) ! ;

      var fallMarkPoint = new XYZ( conduitLine.Origin.X, conduitLine.Origin.Y, height ) ;
      var fallMarkInstance = symbol.Instantiate( fallMarkPoint, level, StructuralType.NonStructural ) ;

      var routeName = conduit.GetRouteName() ;
      
      var changePlumbingInformationStorable = document.GetChangePlumbingInformationStorable() ;

      var existingPlumbingInfo =
        changePlumbingInformationStorable.ChangePlumbingInformationModelData.FirstOrDefault( x =>
          x.ConduitId == conduit.UniqueId ) ;
      
      

      var detaiTableModel = detailTableModels.FirstOrDefault( dtm => dtm.RouteName == routeName ) ;

      var fallMarkNoteString = existingPlumbingInfo !=null && !string.IsNullOrEmpty( existingPlumbingInfo.PlumbingSize) ? 
                               $"{existingPlumbingInfo.PlumbingType}{existingPlumbingInfo.PlumbingSize.Replace( "mm","" )}":
                               $"{detaiTableModel?.PlumbingType}{detaiTableModel?.PlumbingSize.Replace( "mm","" )}" ;

      if ( string.IsNullOrEmpty( fallMarkNoteString ) ) return ;
      
      var fallMarkTextNote = CreateFallMarkNote( document, fallMarkInstance, fallMarkNoteString,fallMarkPoint ) ;
      var fallMarkGroupIds = new[] { fallMarkInstance.Id, fallMarkTextNote.Id } ;
      var group = document.Create.NewGroup(fallMarkGroupIds);
      group.GroupType.Name= fallMarkInstance.UniqueId ;
      
    }
    private static List<ElementId> GetExistedFallMarkInstancesIds( Document document )
    {
      var fallMarkSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ) ??
                            throw new InvalidOperationException() ;
      return document.GetAllFamilyInstances( fallMarkSymbols ).Select( item => item.Id ).ToList();

    }

    private List<ElementId> GetExistedFallMarkTextNoteInstancesIds(Document document , IEnumerable<ElementId> existedFallMarkInstancesIds )
    {
      var fallMarkTextNoteInstanceIds = new HashSet<ElementId>() ;

      foreach ( var groupId in from fallMarkSymbolId in existedFallMarkInstancesIds
               select document.GetElement( fallMarkSymbolId )
               into fallMarkSymbolElement
               select fallMarkSymbolElement.GroupId
               into groupId
               where ! groupId.Equals( ElementId.InvalidElementId )
               select groupId ) {
        if ( document.GetElement( groupId ) is not Group parentGroup ) continue ;
        var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
        foreach ( var group in attachedGroup ) {
          var textNoteIds = group.GetMemberIds() ;
          fallMarkTextNoteInstanceIds.AddRange( textNoteIds ) ;
          group.UngroupMembers() ;
        }

        parentGroup.UngroupMembers() ;
      }

      return fallMarkTextNoteInstanceIds.ToList();
    }

    private static TextNote CreateFallMarkNote(Document document, FamilyInstance fallMarkInstance, string fallMarkNote,XYZ fallMarkPoint)
    {
      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var fallMarkTextNoteType = GetTextNoteTypeForFallMarkNote( document ) ;
      TextNoteOptions opts = new( fallMarkTextNoteType.Id ) { HorizontalAlignment = HorizontalTextAlignment.Right } ;
      var txtPosition = new XYZ( fallMarkPoint.X + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification, fallMarkPoint.Y, fallMarkPoint.Z ) ;

      return TextNote.Create( document, document.ActiveView.Id, txtPosition,fallMarkNote ,opts) ;
    }

    private static TextNoteType GetTextNoteTypeForFallMarkNote(Document doc)
    {
      var fallMarktTextNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( FallMarkTextNoteTypeName, tt.Name ) ) ;
      
      if ( fallMarktTextNoteType != null ) return fallMarktTextNoteType ;
      
      var defaultTextNoteId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      var defaultTextNoteType = doc.GetElement( defaultTextNoteId ) as TextNoteType ;

      if ( defaultTextNoteType == null ) {
        throw new NullReferenceException( "can not find default text note type!" ) ;
      }
      
      var elementType = defaultTextNoteType.Duplicate( FallMarkTextNoteTypeName ) ;
      fallMarktTextNoteType = elementType as TextNoteType ;
      fallMarktTextNoteType?.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
      fallMarktTextNoteType?.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 0 ) ;

      return fallMarktTextNoteType as TextNoteType ?? throw new InvalidOperationException();
    }
    
  }
}