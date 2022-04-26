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
          "TransactionName.Commands.Routing.ShowOpenEndPointMark".GetAppStringByKeyOrDefault(
            "Show Open End Point Mark" ), _ =>
          {
            var openEndPointMarkInstanceIds = GetExistedOpenEndPointMarkInstanceIds( document ) ;
            if ( openEndPointMarkInstanceIds.Count > 0 )
              document.Delete( openEndPointMarkInstanceIds ) ; // remove marks are displaying
            else
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
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      foreach ( var conduit in conduits ) {
        var connectors = conduit.GetConnectors().ToList() ;
        if ( ! IsMissingConnector( allConnectors, conduit, true ) ) {
          var from = conduit.GetRoutingConnectors( true ).FirstOrDefault() ;
          if ( from != null )
            connectors = connectors.Where( connector => ! connector.Origin.IsAlmostEqualTo( from.Origin ) ).ToList() ;
        }

        if ( ! IsMissingConnector( allConnectors, conduit, false ) ) {
          var to = conduit.GetRoutingConnectors( false ).FirstOrDefault() ;
          if ( to != null )
            connectors = connectors.Where( connector => ! connector.Origin.IsAlmostEqualTo( to.Origin ) ).ToList() ;
        }

        missingConnectors.AddRange( connectors.Where( connector =>
          connector is { IsConnected: false } &&
          ! missingConnectors.Any( item => item != null && item.Origin.IsAlmostEqualTo( connector.Origin ) ) ) ) ;
      }

      if ( ! missingConnectors.Any() ) return ;
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ).FirstOrDefault() ??
                   throw new InvalidOperationException() ;
      foreach ( var connector in missingConnectors )
        GenerateOpenEndPointMark( document, symbol, connector ) ;
    }

    private static void GenerateOpenEndPointMark( Document document, FamilySymbol symbol, Connector connector )
    {
      var level = ( connector.Owner as Conduit )!.ReferenceLevel ;
      var height = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
      symbol.Instantiate( new XYZ( connector.Origin.X, connector.Origin.Y, height ), level,
        StructuralType.NonStructural ) ;
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

    private static List<ElementId> GetExistedOpenEndPointMarkInstanceIds( Document document )
    {
      var fallMarkSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.OpenEndPointMark ) ??
                            throw new InvalidOperationException() ;
      return document.GetAllFamilyInstances( fallMarkSymbols ).Select( item => item.Id ).ToList() ;
    }
  }
}