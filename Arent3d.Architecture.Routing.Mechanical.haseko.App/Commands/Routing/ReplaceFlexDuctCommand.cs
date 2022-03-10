using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;
using MoreLinq.Extensions ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Routing.ReplaceFlexDuctCommand", DefaultString = "Change Duct" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class ReplaceFlexDuctCommand : IExternalCommand
  {
    private const string GeneralNotify = "The selected elements are not continuously connected!" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        if ( ! selection.GetElementIds().Any() ) {
          MessageBox.Show( "Please, select the duct elements before running the tool!" ) ;
          return Result.Cancelled ;
        }

        var filter = new ElementMulticategoryFilter( BuiltInCategorySets.Ducts ) ;
        var selectedElements = new FilteredElementCollector( document, selection.GetElementIds() ).WherePasses( filter ).ToElements() ;
        if ( ! selectedElements.Any() ) {
          MessageBox.Show( "Please, select the duct elements!" ) ;
          return Result.Cancelled ;
        }

        var (isContinuousConnected, mesage, connectorRefs, points) = IsContinuousConnected( selectedElements ) ;
        if ( ! isContinuousConnected ) {
          MessageBox.Show( mesage ) ;
          return Result.Cancelled ;
        }

        var viewModel = new ReplaceFlexDuctViewModel( document, ( connectorRefs, points, selectedElements ) ) ;
        var replaceFlexDuctView = new ReplaceFlexDuctView() { DataContext = viewModel } ;
        replaceFlexDuctView.ShowDialog() ;

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    private (bool IsValid, string Message, List<Connector> ConnectorRefs, List<(XYZ Origin, XYZ Direction)> Points) IsContinuousConnected(
      IList<Element> elements )
    {
      var connectorRefs = new List<Connector>() ;
      var points = new List<(XYZ, XYZ)>() ;

      var connectorManagers = GetConnectorManagers( elements ) ;
      if ( connectorManagers.Count == 0 )
        return ( false, "Not found the connectors!", connectorRefs, points ) ;

      if ( connectorManagers.Count == 1 )
        if ( connectorManagers[ 0 ].Connectors.Size != 2 )
          return ( false, "The number of the connectors is not satisfied!", connectorRefs, points ) ;
        else
          return ( true, string.Empty, connectorRefs,
            new List<(XYZ, XYZ)>()
            {
              ( connectorManagers[ 0 ].Lookup( 0 ).Origin, connectorManagers[ 0 ].Lookup( 0 ).CoordinateSystem.BasisZ ),
              ( connectorManagers[ 0 ].Lookup( 1 ).Origin, connectorManagers[ 0 ].Lookup( 1 ).CoordinateSystem.BasisZ )
            } ) ;

      var endConnectors = new List<(List<Connector> Connecteds, List<Connector> UnConnects)>() ;
      foreach ( var connectorManager in connectorManagers ) {
        var connectors = GetConnectors( connectorManager.Connectors ) ;

        if ( connectors.Connecteds.Count == 0 )
          return ( false, GeneralNotify, connectorRefs, points ) ;
        else {
          var insideConnecteds = GetInsideConnecteds( connectors.Connecteds, elements ) ;

          if ( insideConnecteds.Count == 0 )
            return ( false, GeneralNotify, connectorRefs, points ) ;
          else if ( insideConnecteds.Count == 1 ) {
            var outsideConnecteds = connectors.Connecteds.Where( x => x.Id != insideConnecteds[ 0 ].Id ).ToList() ;
            endConnectors.Add( ( outsideConnecteds, connectors.Unconnects ) ) ;
          }
        }
      }

      if ( endConnectors.Count != 2 )
        return ( false, GeneralNotify, connectorRefs, points ) ;
      else {
        foreach ( var endConnector in endConnectors )
          GetEndRefConnector( endConnector.Connecteds, endConnector.UnConnects, ref connectorRefs, ref points ) ;

        if ( ( connectorRefs.Count == 2 && points.Count == 0 ) || ( connectorRefs.Count == 1 && points.Count == 1 ) ||
             ( connectorRefs.Count == 0 && points.Count == 2 ) )
          return ( true, string.Empty, connectorRefs, points ) ;

        return ( false, "The flex duct cannot create!", connectorRefs, points ) ;
      }
    }

    private List<Connector> GetInsideConnecteds( IList<Connector> connecteds, IList<Element> selectedElements )
    {
      var insideConnecteds = new List<Connector>() ;

      foreach ( var connected in connecteds ) {
        var conRefs = GetConnectorRefs( connected ) ;
        var otherElements = selectedElements.Where( x => x.Id != connected.Owner.Id ) ;
        foreach ( var element in otherElements ) {
          if ( conRefs.Any( x => x.Owner.Id == element.Id ) ) {
            insideConnecteds.Add( connected ) ;
            break ;
          }
        }
      }

      return insideConnecteds ;
    }

    private void GetEndRefConnector( List<Connector> connecteds, List<Connector> unConnects, ref List<Connector> connectors,
      ref List<(XYZ, XYZ)> points )
    {
      if ( connecteds.Count > 0 ) {
        var connectedRefs = GetConnectorRefs( connecteds[ 0 ] ).Where( x => x.IsConnected ).ToList() ;
        if ( connectedRefs.Count > 0 )
          connectors.Add( connectedRefs[ 0 ] ) ;
      }
      else if ( unConnects.Count > 0 ) {
        points.Add( ( unConnects[ 0 ].Origin, unConnects[ 0 ].CoordinateSystem.BasisZ ) ) ;
      }
    }

    private List<Connector> GetConnectorRefs( Connector connector )
    {
      var connectorRefs = connector.AllRefs.OfType<Connector>() ;
      return connectorRefs.Where( x => x.Owner.Id != connector.Owner.Id ).ToList() ;
    }

    private (List<Connector> Connecteds, List<Connector> Unconnects) GetConnectors( ConnectorSet connectorSet )
    {
      var connecteds = new List<Connector>() ;
      var unconnects = new List<Connector>() ;

      connectorSet.OfType<Connector>().ForEach( x =>
      {
        if ( x.IsConnected )
          connecteds.Add( x ) ;
        else
          unconnects.Add( x ) ;
      } ) ;

      return ( connecteds, unconnects ) ;
    }

    private List<ConnectorManager> GetConnectorManagers( IList<Element> elements )
    {
      var connectorManagers = new List<ConnectorManager>() ;
      foreach ( var element in elements ) {
        if ( element is FamilyInstance familyInstance ) {
          connectorManagers.Add( familyInstance.MEPModel.ConnectorManager ) ;
        }
        else if ( element is Duct duct ) {
          connectorManagers.Add( duct.ConnectorManager ) ;
        }
      }

      return connectorManagers.Where( x => x.Connectors.Size > 0 ).ToList() ;
    }
  }
}