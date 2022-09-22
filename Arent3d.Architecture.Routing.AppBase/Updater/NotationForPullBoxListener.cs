using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  public class NotationForPullBoxListener : IDocumentUpdateListener
  {
    public Guid Guid { get ; } = new("E34A65F7-9F93-42CA-B194-23111FD81107") ;
    public string Name => "Update Position Of Notation For Pull Box" ;
    public string Description => "Update position of notation for pull box when having any changes" ;
    public bool IsDocumentSpan => false ;
    public bool IsOptional => false ;
    public ChangePriority ChangePriority => ChangePriority.MEPFixtures ;
    public DocumentUpdateListenType ListenType => DocumentUpdateListenType.Geometry ;

    public bool CanListen( Document document ) => true ;

    public IEnumerable<ParameterProxy> GetListeningParameters( Document? document ) => throw new NotSupportedException() ;

    public ElementFilter GetElementFilter( Document? document )
    {
      var categoryFilter = new ElementCategoryFilter( BuiltInCategory.OST_ElectricalFixtures ) ;
      if ( document == null ) return categoryFilter ;

      var sharedParameterElement = new FilteredElementCollector( document ).OfClass( typeof( SharedParameterElement ) ).OfType<SharedParameterElement>().Single( x => x.Name == ElectricalRoutingElementParameter.ConnectorType.GetFamilyName() ) ;
      var parameterFilter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateEqualsRule( sharedParameterElement.Id, ConnectorFamilyType.PullBox.GetFamilyName(), true ) ) ;
      return new LogicalAndFilter( categoryFilter, parameterFilter ) ;
    }

    public void Execute( UpdaterData data )
    {
      try {
        var document = data.GetDocument() ;
        var uiDocument = new UIDocument( document ) ;

        if ( document.ActiveView is not ViewPlan ) return ;
        if ( data.GetModifiedElementIds().Count < 1 ) return ;

        var scale = ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
        var baseLengthOfLine = scale / 100d ;
        var level = document.ActiveView.GenLevel ;
        var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;

        var selectionElementIds = new List<ElementId>() ;
        foreach ( var modifiedElementId in data.GetModifiedElementIds() ) {
          var pullBoxElement = document.GetElement( modifiedElementId ) ;
          if ( pullBoxElement is not FamilyInstance pullBox ) continue ;
          var positionOfPullBox = ( pullBox.Location as LocationPoint )?.Point ;

          if ( positionOfPullBox == null ) continue ;

          var textNoteOfPullBoxUniqueId = storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.SingleOrDefault( t => t.PullBoxUniqueId == pullBox.UniqueId )?.TextNoteUniqueId ;
          if ( string.IsNullOrEmpty( textNoteOfPullBoxUniqueId ) ) continue ;

          var textNoteOfPullBox = document.GetAllElements<TextNote>().FirstOrDefault( t => textNoteOfPullBoxUniqueId == t.UniqueId ) ;
          if ( textNoteOfPullBox != null ) {
            var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
            var routesRelatedPullBox = PullBoxRouteManager.GetRoutesRelatedPullBoxByNearestEndPoints( document, pullBox, routes ) ;
            var pullBoxLocation = ( pullBox.Location as LocationPoint )?.Point! ;
            var conduitsRelatedPullBox = PullBoxRouteManager.GetConduitsRelatedPullBox( document, pullBoxLocation, routesRelatedPullBox ) ;
            var positionOfTextNoteForPullBox = PullBoxRouteManager.GetPositionOfPullBox( document, textNoteOfPullBox, positionOfPullBox, conduitsRelatedPullBox, PullBoxRouteManager.HeightDistanceBetweenPullAndNotation, baseLengthOfLine ) ;
            textNoteOfPullBox.Coord = positionOfTextNoteForPullBox ;
          }
          selectionElementIds.Add( modifiedElementId ) ;
          selectionElementIds.Add( textNoteOfPullBox!.Id ) ;
        }

        if ( selectionElementIds.Any() )
          uiDocument.Selection.SetElementIds( selectionElementIds ) ;
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
      }
    }
  }
}