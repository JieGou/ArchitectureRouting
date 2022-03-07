using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Extensions ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  public class ReplaceFlexDuctCommand : IExternalCommand
  {
    private const string Title = "Arent" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        Func<Element, bool> filter = f =>
          f.Category.Id.IntegerValue == (int) BuiltInCategory.OST_DuctCurves || f.Category.Id.IntegerValue == (int) BuiltInCategory.OST_DuctFitting ;
        
        var elements = selection.PickObjects( ObjectType.Element, SelectionFilter.GetElementFilter( filter ) )
          .Select( r => document.GetElement( r ) ) ;
        
        var connectorManagers = GetConnectorManagers( elements ) ;
        if ( connectorManagers.Count == 0 ) {
          TaskDialog.Show( Title, "Please select again!" ) ;
          return Result.Failed ;
        }

        
        
        // var (isValid, mesage) = IsContinuousConnected( connectorManagers, out (Connector Start, Connector End)? connectors ) ;
        // if ( ! isValid ) {
        //   TaskDialog.Show( Title, mesage, TaskDialogCommonButtons.Ok ) ;
        //   return Result.Cancelled ;
        // }
        
        var count = GetConnectors( connectorManagers[ 0 ] ).Count() ;
        TaskDialog.Show( Title, $"{count}" ) ;

        // var replaceFlexDuctView = new ReplaceFlexDuctView() { DataContext = new ReplaceFlexDuctViewModel( document ) } ;
        // replaceFlexDuctView.ShowDialog() ;


        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Failed ;
      }
    }

    // private (bool IsValid, string Message, (Connector Start, Connector End)? connectors) IsContinuousConnected(
    //   List<ConnectorManager> connectorManagers )
    // {
    //   if ( ! connectorManagers.Any() )
    //     return ( false, "Not found the connectors!", null ) ;
    //
    //   if ( connectorManagers.Count == 1 ) {
    //     if ( connectorManagers[ 0 ].Connectors.Size != 2 )
    //       return ( false, "The number of the connectors is not satisfied!", null ) ;
    //     else
    //       return ( true, string.Empty, ( connectorManagers[ 0 ].Lookup( 0 ), connectorManagers[ 0 ].Lookup( 1 ) ) ) ;
    //   }
    //
    //   var connectorManager = connectorManagers[ 0 ] ;
    //   var otherConnectorManager = connectorManagers.GetRange( 1, connectorManagers.Count - 1 ) ;
    // }
    
    
    
    private static List<Connector> GetConnectors( ConnectorManager connectorManager )
    {
      var connectors = new List<Connector>() ;
      
      var connector = connectorManager.Connectors.GetEnumerator() ;
      while ( connector.MoveNext() ) {
        connectors.Add((Connector)connector.Current!);
      }

      return connectors ;
    }

    private List<ConnectorManager> GetConnectorManagers( IEnumerable<Element> elements )
    {
      var connectorManagers = new List<ConnectorManager>() ;
      foreach ( var element in elements ) {
        if ( element is FamilyInstance familyInstance ) {
          connectorManagers.Add(familyInstance.MEPModel.ConnectorManager);
        }
        else if ( element is Duct duct ) {
          connectorManagers.Add(duct.ConnectorManager);
        }
      }
      return connectorManagers ;
    }
  }
}