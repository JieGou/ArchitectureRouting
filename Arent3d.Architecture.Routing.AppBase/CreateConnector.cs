﻿using System ;
using Autodesk.Revit.DB ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public class CreateConnector
  {
    private readonly Connector? _pickedConnector ;

    public Connector? GetPickedConnector()
    {
      return _pickedConnector ;
    }

    public CreateConnector( UIDocument uiDocument, Element element, Connector? firstConnector, AddInType addInType, bool isLeakRoute = false )
    {
      var tempInstanceIds = new List<ElementId>() ;

      var familyInstance = element as FamilyInstance ;
      var connectorAndElementPairs = ( null != familyInstance ) ? GetConnectorAndConnectorElementPair( familyInstance, addInType ).EnumerateAll() : Array.Empty<(Connector, ConnectorElement)>() ;
      int orgTransparency ;
      using ( var tr = new Transaction( element.Document ) ) {
        tr.Start( "Create connector points and origin for pick" ) ;

        if ( null != familyInstance ) {
          foreach ( var (conn, connElm) in connectorAndElementPairs ) {
            if ( AddConnectorFamily( conn, connElm, familyInstance, firstConnector ) is { } instance ) {
              tempInstanceIds.Add( instance.Id ) ;
            }
          }
        }

        if ( ( familyInstance?.GetTotalTransform().Origin ?? ( element.Location as LocationPoint )?.Point ) is { } point && ! isLeakRoute ) {
          var instanceOrg = element.Document.AddTerminatePoint( element.GetRouteName()!, point, XYZ.BasisX, null, element.GetLevelId() ) ;
          tempInstanceIds.Add( instanceOrg.Id ) ;
        }

        orgTransparency = SetElementTransparency( element, 50 ) ;

        tr.Commit() ;
      }

      _pickedConnector = ConnectorPicker.GetInOutConnector( uiDocument, element, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, addInType ) ;

      using ( var tr = new Transaction( element.Document ) ) {
        tr.Start( "Delete connector points and origin for pick" ) ;

        SetElementTransparency( element, orgTransparency ) ;
        element.Document.Delete( tempInstanceIds ) ;

        tr.Commit() ;
      }
    }

    public CreateConnector( FamilyInstance familyInstance, XYZ prevPoint )
    {
      string connectorType ;
      var nextPoint = ( familyInstance.Location as LocationPoint )!.Point ;
      var distanceX = Math.Abs( prevPoint.X - nextPoint.X ) ;
      var distanceY = Math.Abs( prevPoint.Y - nextPoint.Y ) ;
      if ( distanceX == 0 ) {
        connectorType = prevPoint.Y < nextPoint.Y ? "Front" : "Back" ;
      }
      else if ( distanceY == 0 ) {
        connectorType = prevPoint.X < nextPoint.X ? "Left" : "Right" ;
      }
      else {
        if ( distanceX > distanceY ) {
          connectorType = prevPoint.X < nextPoint.X ? "Left" : "Right" ;
        }
        else {
          connectorType = prevPoint.Y < nextPoint.Y ? "Front" : "Back" ;
        }
      }
      
      _pickedConnector = ! string.IsNullOrEmpty( connectorType ) 
        ? familyInstance.GetConnectors().FirstOrDefault( x => x.Description.Contains( connectorType ) ) 
        : familyInstance.GetConnectors().FirstOrDefault() ;
    }
    
    public CreateConnector( Element element, XYZ? previousXyz, XYZ? nextXyz, double tolerance )
    { 
      var xyzElement = ( element.Location as LocationPoint )!.Point ;
      var connectorType = string.Empty ;
      
      if ( null != previousXyz ) {
        if ( Math.Abs( previousXyz.Y - xyzElement.Y ) < tolerance ) //same Y
          connectorType = previousXyz.X < xyzElement.X ? "Left" : "Right" ;
        else
          connectorType = previousXyz.Y < xyzElement.Y ? "Bottom" : "Top" ;
      }
      
      if ( null != nextXyz ) {
        if ( Math.Abs( nextXyz.X - xyzElement.X ) < tolerance ) //same X
          connectorType = nextXyz.Y < xyzElement.Y ? "Bottom" : "Top" ;
        else
          connectorType = nextXyz.X <= xyzElement.X ? "Left" : "Right" ;
      }
      
      _pickedConnector = !string.IsNullOrEmpty( connectorType ) ? element.GetConnectors().FirstOrDefault(x=>x.Description.Contains( connectorType )) : element.GetConnectors().FirstOrDefault() ; 
    }

    private static FamilyInstance? AddConnectorFamily( Connector conn, ConnectorElement connElm, FamilyInstance familyInstance, Connector? firstConnector )
    {
      if ( false == IsEnabledConnector( conn, firstConnector ) ) return null ;

      var directionType = FlowDirectionType.Bidirectional ;
      if ( connElm is { } connectorElement ) {
        if ( connectorElement.get_Parameter( BuiltInParameter.RBS_PIPE_FLOW_DIRECTION_PARAM ) is { } flowDirectionType ) {
          directionType = (FlowDirectionType)flowDirectionType.AsInteger() ;
        }
      }
      else {
        directionType = conn.Direction ;
      }

      return familyInstance.Document.AddConnectorFamily( conn, connElm.GetRouteName()!, directionType, conn.Origin,  connElm.Direction, conn.GetDiameter() * 0.5 ) ;
    }

    public static bool IsEnabledConnector( Connector connector, Connector? firstConnector )
    {
      return ( false == connector.IsConnected ) && ( ( null == firstConnector ) || ( connector.HasCompatibleSystemType( firstConnector ) && firstConnector.HasCompatibleShape( connector ) ) ) ;
    }

    private static int SetElementTransparency( Element element, int transparency )
    {
      var document = element.Document ;

      var overrideGraphicSettings = document.ActiveView.GetElementOverrides( element.Id ) ;
      var lastSurfaceTransparency = overrideGraphicSettings.Transparency ;
      overrideGraphicSettings.SetSurfaceTransparency( transparency ) ;

      document.ActiveView.SetElementOverrides( element.Id, overrideGraphicSettings ) ;
      return lastSurfaceTransparency ;
    }

    public static IEnumerable<(Connector, ConnectorElement)> GetConnectorAndConnectorElementPair( FamilyInstance familyInstance, AddInType addInType )
    {
      var familyInstanceTransform = familyInstance.GetTotalTransform() ;
      using var familyDocument = familyInstance.Document.EditFamily( familyInstance.Symbol.Family ) ;

      var connectors = familyInstance.GetConnectors().Where( c => ConnectorPicker.IsTargetConnector( c, addInType ) ) ;
      var connectorElements = familyDocument.GetAllElements<ConnectorElement>().Where( c => IsTargetConnectorElement( c, addInType ) ) ;
      var seeker = new NearestConnectorPairSeeker( connectors, connectorElements, familyInstanceTransform ) ;

      while ( true ) {
        var (connector, connectorElement) = seeker.Pop() ;
        if ( null == connector || null == connectorElement ) break ;

        yield return ( connector, connectorElement ) ;
      }
    }

    private static bool IsTargetConnectorElement( ConnectorElement connElm, AddInType addInType ) => ConnectorPicker.IsTargetDomain( connElm.Domain, addInType ) ;
  }
}