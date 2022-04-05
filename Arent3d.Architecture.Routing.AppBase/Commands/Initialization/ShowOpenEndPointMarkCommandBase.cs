using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowOpenEndPointMarkCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction(
          "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Not Connect" ), _ =>
          {
            if ( ! HideOpenEndPointMarks( document ) )
              CreateOpenEndPointMarkForNotConnectedConnector( document ) ;

            return Result.Succeeded ;
          } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void CreateOpenEndPointMarkForNotConnectedConnector( Document document )
    {
      var notConnectedConduitPosition = new List<XYZ>() ;

      // check conduits
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      foreach ( var conduit in conduits ) {
        if (CheckConnectedConduit( allConnectors, conduit )) continue;
        var from = conduit.GetRoutingConnectors( true ).FirstOrDefault() ;
        var to = conduit.GetRoutingConnectors( false ).FirstOrDefault() ;
        if ( to is { IsConnected: false } &&
             ! notConnectedConduitPosition.Any( item => item != null && Equal( item, to.Origin ) ) )
          notConnectedConduitPosition.Add( to.Origin ) ;

        if ( from is { IsConnected: false } &&
             ! notConnectedConduitPosition.Any( item => item != null && Equal( item, from.Origin ) ) )
          notConnectedConduitPosition.Add( from.Origin ) ;
      }

      // check conduitFittings
      var conduitFittings = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) )
        .OfCategory( BuiltInCategory.OST_ConduitFitting ).AsEnumerable().OfType<FamilyInstance>() ;
      foreach ( var conduit in conduitFittings ) {
        if (CheckConnectedConduit( allConnectors, conduit )) continue;
        var from = conduit.GetRoutingConnectors( true ).FirstOrDefault() ;
        var to = conduit.GetRoutingConnectors( false ).FirstOrDefault() ;
        if ( to is { IsConnected: false } &&
             ! notConnectedConduitPosition.Any( item => item != null && Equal( item, to.Origin ) ) )
          notConnectedConduitPosition.Add( to.Origin ) ;

        if ( from is { IsConnected: false } &&
             ! notConnectedConduitPosition.Any( item => item != null && Equal( item, from.Origin ) ) )
          notConnectedConduitPosition.Add( from.Origin ) ;
      }

      if ( ! notConnectedConduitPosition.Any() ) return ;
      var fallMarkSymbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ).FirstOrDefault() ??
                           throw new InvalidOperationException() ;
      foreach ( var conduitOrigin in notConnectedConduitPosition )
        fallMarkSymbol.Instantiate( conduitOrigin, StructuralType.NonStructural ) ;
    }

    private static bool Equal( XYZ a, XYZ b )
    {
      return Math.Abs( a.X - b.X ) <= GeometryUtil.Tolerance && Math.Abs( a.Y - b.Y ) <= GeometryUtil.Tolerance &&
             Math.Abs( a.Z - b.Z ) <= GeometryUtil.Tolerance ;
    }

    private static bool CheckConnectedConduit( IReadOnlyCollection<Element> allConnectors, Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ).FirstOrDefault() ;
      var fromEndPointKey = fromEndPoint?.Key ;
      if ( fromEndPointKey != null ) {
        var fromElementUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromElementUniqueId ) &&
             allConnectors.All( c => c.UniqueId != fromElementUniqueId ) )
          return false ;
      }

      var toEndPoint = conduit.GetNearestEndPoints( false ).FirstOrDefault() ;
      var toEndPointKey = toEndPoint?.Key ;
      if ( toEndPointKey != null ) {
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( toElementUniqueId ) && allConnectors.All( c => c.UniqueId != toElementUniqueId ) )
          return false ;
      }

      return true ;
    }

    private static bool HideOpenEndPointMarks( Document document )
    {
      var fallMarkSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ) ??
                            throw new InvalidOperationException() ;
      var fallMarkIds = document.GetAllFamilyInstances( fallMarkSymbols ).Select( item => item.Id ).ToList() ;
      if ( fallMarkIds.Count == 0 ) return false ;
      document.Delete( fallMarkIds ) ;
      return true ;
    }
  }
}