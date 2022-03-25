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
  public class ShowFallMarkCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Not Connect" ), _ =>
        {
          CreateFallMarkForConduitWithZDirection( document ) ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    private void CreateFallMarkForConduitWithZDirection( Document document )
    {
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) ).OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var conduitWithZDirection = new List<XYZ>() ;
      foreach ( var conduit in conduits ) {
        var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitPosition.Curve as Line ) ! ;
        var conduitDirection = conduitLine.Direction ;
        if ( conduitDirection.Z is not (1.0 or -1.0) ) continue ;
        var conduitOrigin = conduitLine.Origin ;
        if ( ! conduitWithZDirection.Contains( conduitOrigin ) ) 
          conduitWithZDirection.Add( conduitOrigin ) ;
      }
      
      if ( ! conduitWithZDirection.Any() ) return ;
      {
        var fallMarkSymbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        fallMarkSymbol.TryGetProperty( "Lenght", out double lenghtMark ) ;
        foreach ( var (x, y, z) in conduitWithZDirection ) {
          fallMarkSymbol.Instantiate( new XYZ( x, y - lenghtMark / 2, z ), StructuralType.NonStructural ) ;
        }
      }
    }

    private bool CheckConduitWithZDirection( IReadOnlyCollection<Element> allConnectors, Element conduit )
    {
      XYZ fromOrigin = new() ;
      XYZ toOrigin ;
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      if ( fromEndPointKey != null ) {
        var fromElementUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromElementUniqueId ) ) {
          var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromElementUniqueId ) ;
          if ( fromConnector != null ) {
            var fromLocation = ( fromConnector.Location as LocationPoint ) ! ;
            fromOrigin = fromLocation.Point ;
          }
        }
      }
      
      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      if ( toEndPointKey == null ) return false ;
      {
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementUniqueId ) ) return false ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementUniqueId ) ;
        if ( toConnector == null ) return false ;
        var toLocation = ( toConnector.Location as LocationPoint ) ! ;
        toOrigin = toLocation.Point ;
      }

      return Math.Abs( fromOrigin.Z - toOrigin.Z ) > 0 && Math.Abs( fromOrigin.X - toOrigin.X ) == 0 && Math.Abs( fromOrigin.Y - toOrigin.Y ) == 0 ;
    }
  }
}