using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
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

        var ductCategories = new List<BuiltInCategory>() { BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_FlexDuctCurves } ;

        var filter = new ElementMulticategoryFilter( ductCategories ) ;
        var selectedElements = new FilteredElementCollector( document, selection.GetElementIds() ).WherePasses( filter ).ToElements() ;
        if ( selection.GetElementIds().Count != selectedElements.Count || ! selectedElements.Any() ) {
          MessageBox.Show( "Please, select the duct elements!" ) ;
          return Result.Cancelled ;
        }

        var (isContinuousConnected, msg, connectorRefs, points) = IsContinuousConnected( selectedElements ) ;
        if ( ! isContinuousConnected ) {
          MessageBox.Show( msg ) ;
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

    private (bool IsValid, string Message, List<Connector> ConnectorRefs, List<(XYZ Origin, XYZ Direction)> Points) IsContinuousConnected( IList<Element> elements )
    {
      var connectorRefs = new List<Connector>() ;
      var points = new List<(XYZ, XYZ)>() ;

      var connectorManagers = GetConnectorManagers( elements ) ;
      if ( connectorManagers.Count <= 1 )
        if ( ! connectorManagers.Any() || connectorManagers[ 0 ].Connectors.Size != 2 )
          return ( false, "The number of the connectors is not satisfied!", connectorRefs, points ) ;
        else {
          var connectors = connectorManagers[ 0 ].Connectors.OfType<Connector>().OrderBy( x => x.Id ).ToList() ;
          return ( true, string.Empty, connectorRefs, new List<(XYZ, XYZ)>() { ( connectors[ 0 ].Origin, connectors[ 0 ].CoordinateSystem.BasisZ ), ( connectors[ 1 ].Origin, connectors[ 1 ].CoordinateSystem.BasisZ ) } ) ;
        }

      var endConnectors = new List<(List<Connector> Connecteds, List<Connector> UnConnects)>() ;
      foreach ( var connectorManager in connectorManagers ) {
        var (connected, unConnect) = GetConnectors( connectorManager.Connectors ) ;

        if ( connected.Count == 0 )
          return ( false, GeneralNotify, connectorRefs, points ) ;

        var insideConnected = GetInsideConnected( connected, elements ) ;

        switch ( insideConnected.Count ) {
          case 0 :
            return ( false, GeneralNotify, connectorRefs, points ) ;
          case 1 :
          {
            var outsideConnected = connected.Where( x => x.Id != insideConnected[ 0 ].Id ).ToList() ;
            endConnectors.Add( ( outsideConnected, unConnect ) ) ;
            break ;
          }
        }
      }

      if ( endConnectors.Count != 2 )
        return ( false, GeneralNotify, connectorRefs, points ) ;
      foreach ( var (connected, unConnects) in endConnectors )
        GetEndRefConnector( connected, unConnects, ref connectorRefs, ref points ) ;

      if ( ( connectorRefs.Count == 2 && points.Count == 0 ) || ( connectorRefs.Count == 1 && points.Count == 1 ) || ( connectorRefs.Count == 0 && points.Count == 2 ) )
        return ( true, string.Empty, connectorRefs, points ) ;

      return ( false, "The flex duct cannot create!", connectorRefs, points ) ;
    }

    private List<Connector> GetInsideConnected( IEnumerable<Connector> connectedConnectors, IList<Element> selectedElements )
    {
      var insideConnected = new List<Connector>() ;

      foreach ( var connectedConnector in connectedConnectors ) {
        var conRefs = GetConnectorRefs( connectedConnector ) ;
        var otherElements = selectedElements.Where( x => x.Id != connectedConnector.Owner.Id ) ;
        foreach ( var element in otherElements ) {
          if ( conRefs.Any( x => x.Owner.Id == element.Id ) ) {
            insideConnected.Add( connectedConnector ) ;
            break ;
          }
        }
      }

      return insideConnected ;
    }

    private void GetEndRefConnector( IReadOnlyList<Connector> connected, IReadOnlyList<Connector> unConnects, ref List<Connector> connectors, ref List<(XYZ, XYZ)> points )
    {
      if ( connected.Count > 0 ) {
        var connectedRefs = GetConnectorRefs( connected[ 0 ] ).Where( x => ( x.ConnectorType == ConnectorType.Logical || x.IsConnected ) ).ToList() ;
        if ( connectedRefs.Count > 0 )
          connectors.Add( connectedRefs[ 0 ] ) ;
      }
      else if ( unConnects.Count > 0 ) {
        points.Add( ( unConnects[ 0 ].Origin, unConnects[ 0 ].CoordinateSystem.BasisZ ) ) ;
      }
    }

    private List<Connector> GetConnectorRefs( Connector connector )
    {
      var connectorRefs = connector.AllRefs.OfType<Connector>().Where( x => x.Domain != Domain.DomainUndefined ) ;
      return connectorRefs.Where( x => x.Owner.Id != connector.Owner.Id ).ToList() ;
    }

    private (List<Connector> Connecteds, List<Connector> Unconnects) GetConnectors( ConnectorSet connectorSet )
    {
      var connected = new List<Connector>() ;
      var unConnect = new List<Connector>() ;

      connectorSet.OfType<Connector>().ForEach( x =>
      {
        if ( x.IsConnected )
          connected.Add( x ) ;
        else
          unConnect.Add( x ) ;
      } ) ;

      return ( connected, unConnect ) ;
    }

    private List<ConnectorManager> GetConnectorManagers( IList<Element> elements )
    {
      var connectorManagers = new List<ConnectorManager>() ;
      foreach ( var element in elements ) {
        if ( element is FamilyInstance familyInstance && familyInstance.MEPModel.ConnectorManager is { } ) {
          connectorManagers.Add( familyInstance.MEPModel.ConnectorManager ) ;
        }
        else if ( element is MEPCurve { ConnectorManager: { } } mepCurve ) {
          connectorManagers.Add( mepCurve.ConnectorManager ) ;
        }
      }

      return connectorManagers.Where( x => x.Connectors.Size > 0 ).ToList() ;
    }
  }
}