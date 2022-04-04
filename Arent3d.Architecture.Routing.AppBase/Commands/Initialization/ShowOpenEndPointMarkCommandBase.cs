using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
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
      Document document = commandData.Application.ActiveUIDocument.Document ;
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

    private void CreateOpenEndPointMarkForNotConnectedConnector( Document document )
    {
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var notConnectedConduitPosition = new List<XYZ>() ;
      foreach ( var conduit in conduits ) {
        var isConnectedConduit = CheckConnectedConduit( allConnectors, conduit ) ;
        if ( isConnectedConduit ) continue ;
        var t = conduit.GetConnectorManager() ;
        var relatedConnectorOrigins = conduit.GetConnectors().Select( item => item.Origin ).ToList() ;
        foreach ( var connectorOrigin in relatedConnectorOrigins )
          if ( !allConnectors.Select( item => (item.Location as LocationPoint)?.Point ).Any( item =>
                item != null && (Math.Abs( item.X - connectorOrigin.X ) <= GeometryUtil.Tolerance &&
                                 Math.Abs( item.Y - connectorOrigin.Y ) <= GeometryUtil.Tolerance &&
                                 Math.Abs( item.Z - connectorOrigin.Z ) <= GeometryUtil.Tolerance) ) )
            notConnectedConduitPosition.Add( connectorOrigin ) ;
      }

      if ( ! notConnectedConduitPosition.Any() ) return ;
      var fallMarkSymbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ).FirstOrDefault() ??
                           throw new InvalidOperationException() ;
      foreach ( var conduitOrigin in notConnectedConduitPosition ) {
        fallMarkSymbol.Instantiate( conduitOrigin, StructuralType.NonStructural ) ;
      }
    }

    private bool CheckConnectedConduit( IReadOnlyCollection<Element> allConnectors, Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ).FirstOrDefault() ;
      var fromEndPointKey = fromEndPoint?.Key ;
      if ( fromEndPointKey != null ) {
        var fromElementUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromElementUniqueId ) &&
             allConnectors.All( c => c.UniqueId != fromElementUniqueId ) ) {
          return false ;
        }
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

    private bool HideOpenEndPointMarks( Document document )
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