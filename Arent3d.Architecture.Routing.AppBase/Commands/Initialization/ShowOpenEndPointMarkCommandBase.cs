using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
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
      var missingConnectors = new List<Connector>() ;
      // check conduits
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      foreach ( var conduit in conduits ) {
        var connectors = conduit.GetConnectors().ToList() ;
        if ( ! IsMissingConnector( allConnectors, conduit, true ) ) {
          var from = conduit.GetRoutingConnectors( true ).FirstOrDefault() ;
          if ( from != null )
            connectors = connectors.Where( connector => ! Equal( connector.Origin, from.Origin ) ).ToList() ;
        }

        if ( ! IsMissingConnector( allConnectors, conduit, false ) ) {
          var to = conduit.GetRoutingConnectors( false ).FirstOrDefault() ;
          if ( to != null )
            connectors = connectors.Where( connector => ! Equal( connector.Origin, to.Origin ) ).ToList() ;
        }

        missingConnectors.AddRange( connectors.Where( connector =>
          connector is { IsConnected: false } &&
          ! missingConnectors.Any( item => item != null && Equal( item.Origin, connector.Origin ) ) ) ) ;
      }

      if ( ! missingConnectors.Any() ) return ;
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ).FirstOrDefault() ??
                   throw new InvalidOperationException() ;
      foreach ( var connector in missingConnectors )
        GenerateMark( document, symbol, connector ) ;
    }

    private static void GenerateMark( Document document, FamilySymbol symbol, Connector connector )
    {
      var level = ( connector.Owner as Conduit )!.ReferenceLevel ;
      var height = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
      symbol.Instantiate( new XYZ( connector.Origin.X, connector.Origin.Y, height ), level,
        StructuralType.NonStructural ) ;
    }

    private static bool Equal( XYZ a, XYZ b )
    {
      return Math.Abs( a.X - b.X ) <= GeometryUtil.Tolerance && Math.Abs( a.Y - b.Y ) <= GeometryUtil.Tolerance &&
             Math.Abs( a.Z - b.Z ) <= GeometryUtil.Tolerance ;
    }

    private static bool IsMissingConnector( IEnumerable<Element> allConnectors, Element conduit, bool isFrom )
    {
      var endPoint = conduit.GetNearestEndPoints( isFrom ).FirstOrDefault() ;
      var endPointKey = endPoint?.Key ;
      if ( endPointKey == null ) return true ;
      var fromElementUniqueId = endPointKey.GetElementUniqueId() ;
      return ! string.IsNullOrEmpty( fromElementUniqueId ) &&
             allConnectors.All( c => c.UniqueId != fromElementUniqueId ) ;
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