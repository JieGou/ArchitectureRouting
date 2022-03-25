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
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Not Connect" ), _ =>
        {
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
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) ).OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var notConnectedConduitPosition = new List<XYZ>() ;
      foreach ( var conduit in conduits ) {
        var isConnectedConduit = CheckConnectedConduit( allConnectors, conduit ) ;
        if ( isConnectedConduit ) continue ;
        var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitPosition.Curve as Line ) ! ;
        var conduitOrigin = conduitLine.Origin ;
        if ( ! notConnectedConduitPosition.Contains( conduitOrigin ) ) 
          notConnectedConduitPosition.Add( conduitOrigin ) ;
      }

      if ( ! notConnectedConduitPosition.Any() ) return ;
      {
        var fallMarkSymbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        foreach ( var conduitOrigin in notConnectedConduitPosition ) {
          fallMarkSymbol.Instantiate( conduitOrigin, StructuralType.NonStructural ) ;
        }
      }
    }

    private bool CheckConnectedConduit( IReadOnlyCollection<Element> allConnectors, Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      if ( fromEndPointKey != null ) {
        var fromElementUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromElementUniqueId ) ) {
          var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromElementUniqueId ) ;
          if ( fromConnector == null ) {
            return false ;
          }
        }
      }

      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      if ( toEndPointKey == null ) return true ;
      {
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementUniqueId ) ) return true ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementUniqueId ) ;
        if ( toConnector == null ) {
          return false ;
        }
      }

      return true ;
    }
  }
}