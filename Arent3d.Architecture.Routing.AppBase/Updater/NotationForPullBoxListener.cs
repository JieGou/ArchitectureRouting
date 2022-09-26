using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
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

        if ( data.GetModifiedElementIds().Count < 1 ) return ;

        var selectionElementIds = new List<ElementId>() ;
        foreach ( var modifiedElementId in data.GetModifiedElementIds() ) {
          var pullBoxElement = document.GetElement( modifiedElementId ) ;
          if ( pullBoxElement is not FamilyInstance pullBox ) continue ;
          var positionOfPullBox = ( pullBox.Location as LocationPoint )?.Point ;

          if ( positionOfPullBox == null ) continue ;

          var level = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).SingleOrDefault( l => l.Id == pullBoxElement.LevelId ) ;
          if ( level == null ) continue ;

          var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
          var textNoteOfPullBoxUniqueId = storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.SingleOrDefault( t => t.PullBoxUniqueId == pullBox.UniqueId )?.TextNoteUniqueId ;
          if ( string.IsNullOrEmpty( textNoteOfPullBoxUniqueId ) ) continue ;

          var viewPlans = new FilteredElementCollector( document ).OfClass( typeof( ViewPlan ) ).Where( v => v is ViewPlan { GenLevel: { } } viewPlan && viewPlan.GenLevel.Id == level.Id ).Cast<ViewPlan>().EnumerateAll() ;
          if ( ! viewPlans.Any() ) continue ;

          var textNoteOfPullBox = document.GetAllElements<TextNote>().FirstOrDefault( t => textNoteOfPullBoxUniqueId == t.UniqueId && viewPlans.Any( v => v.Id == t.OwnerViewId ) ) ;
          if ( textNoteOfPullBox != null ) {
            var viewPlan = viewPlans.SingleOrDefault( v => v.Id == textNoteOfPullBox.OwnerViewId ) ;
            if ( viewPlan == null ) continue ;

            var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
            var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).OfType<Conduit>().EnumerateAll() ;
            var routesRelatedPullBox = PullBoxRouteManager.GetRoutesRelatedPullBoxByNearestEndPoints( routes, allConduits, pullBox ) ;
            var pullBoxLocation = ( pullBox.Location as LocationPoint )?.Point! ;
            var conduitsRelatedPullBox = PullBoxRouteManager.GetConduitsRelatedPullBox( allConduits, routesRelatedPullBox, pullBoxLocation ) ;
            var conduitDirections = PullBoxRouteManager.GetConduitDirectionsRelatedPullBox( conduitsRelatedPullBox ) ;

            var scale = ImportDwgMappingModel.GetMagnificationOfView( viewPlan.Scale ) ;
            var baseLengthOfLine = scale / 100d ;
            var positionOfTextNoteForPullBox = PullBoxRouteManager.GetPositionOfPullBox( textNoteOfPullBox, positionOfPullBox, conduitDirections, viewPlan.Scale, baseLengthOfLine ) ;
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