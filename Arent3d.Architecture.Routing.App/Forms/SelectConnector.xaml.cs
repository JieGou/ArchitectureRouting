using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;
using System.Windows ;
using System.Linq ;
using Arent3d.Revit ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  /// <summary>
  /// Interaction logic for SelectConnector.xaml
  /// </summary>
  public partial class SelectConnector : Window
  {
    private readonly Connector? _firstConnector ;

    public ObservableCollection<ConnectorInfoClass> ConnectorList { get ; } = new() ;

    public SelectConnector( Element element, Connector? firstConnector = null )
    {
      InitializeComponent() ;

      _firstConnector = firstConnector ;

      if ( element is FamilyInstance familyInstance ) {
        foreach ( var (conn, connElm) in GetConnectorAndConnectorElementPair( familyInstance ) ) {
          ConnectorList.Add( new ConnectorInfoClass( familyInstance, connElm, conn, _firstConnector ) ) ;
        }
      }
      else if ( element is MEPCurve curve ) {
        foreach ( var c in curve.GetConnectors().Where( IsTargetConnector ) ) {
          ConnectorList.Add( new ConnectorInfoClass( c, _firstConnector ) ) ;
        }
      }

      ConnectorList.Add( new ConnectorInfoClass( element ) ) ;

      this.Left = 0 ;
      this.Top += 10 ;
    }

    private IEnumerable<(Connector, ConnectorElement)> GetConnectorAndConnectorElementPair( FamilyInstance familyInstance )
    {
      var familyInstanceTransform = familyInstance.GetTotalTransform() ;
      using var familyDocument = familyInstance.Document.EditFamily( familyInstance.Symbol.Family ) ;

      var connectors = familyInstance.GetConnectors().Where( IsTargetConnector ) ;
      var connectorElements = familyDocument.GetAllElements<ConnectorElement>().Where( IsTargetConnectorElement ) ;
      var seeker = new NearestConnectorPairSeeker( connectors, connectorElements, familyInstanceTransform ) ;

      while ( true ) {
        var (connector, connectorElement) = seeker.Pop() ;
        if ( null == connector || null == connectorElement ) break ;

        yield return ( connector, connectorElement ) ;
      }
    }

    private static bool IsTargetConnectorElement( ConnectorElement connElm ) => IsTargetDomain( connElm.Domain ) ;

    private static bool IsTargetConnector( Connector conn ) => conn.IsAnyEnd() && IsTargetDomain( conn.Domain ) ;

    private static bool IsTargetDomain( Domain domain ) 
    {
      return domain switch
      {
        Domain.DomainPiping => true,
        Domain.DomainHvac => true,
        Domain.DomainCableTrayConduit => true,
        _ => false
      } ;
    }

    private class NearestConnectorPairSeeker
    {
      private readonly List<Connector> _connectors ;
      private readonly List<ConnectorElement> _connectorElements ;
      private readonly List<DistanceInfo> _distances = new() ;
      
      public NearestConnectorPairSeeker( IEnumerable<Connector> connectors, IEnumerable<ConnectorElement> connectorElements, Transform familyInstanceTransform )
      {
        _connectors = connectors.ToList() ;
        _connectorElements = connectorElements.ToList() ;

        foreach ( var connectorElement in _connectorElements ) {
          var domain = connectorElement.Domain ;
          var systemClassification = connectorElement.SystemClassification ;
          var origin = familyInstanceTransform.OfPoint( connectorElement.Origin ) ;
          var dirZ = familyInstanceTransform.OfVector( connectorElement.CoordinateSystem.BasisZ ) ;
          var dirX = familyInstanceTransform.OfVector( connectorElement.CoordinateSystem.BasisX ) ;
          foreach ( var connector in _connectors.Where( c => domain == c.Domain ) ) {
            if ( domain != Domain.DomainCableTrayConduit && false == HasCompatibleSystemType( systemClassification, connector ) ) continue ;

            var distance = connector.Origin.DistanceTo( origin ) ;
            var angleZ = connector.CoordinateSystem.BasisZ.AngleTo( dirZ ) ;
            var angleX = connector.CoordinateSystem.BasisX.AngleTo( dirX ) ;
            _distances.Add( new DistanceInfo( connector, connectorElement, distance, angleZ, angleX ) ) ;
          }
        }

        _distances.Sort( DistanceInfo.Compare ) ;
      }

      private static bool HasCompatibleSystemType( MEPSystemClassification systemClassification, Connector connector )
      {
        if ( systemClassification == MEPSystemClassification.Global || systemClassification == MEPSystemClassification.Fitting ) return true ;
        return ( (int) systemClassification == (int) connector.GetSystemType() ) ;
      }

      public (Connector?, ConnectorElement?) Pop()
      {
        if ( 0 == _distances.Count ) return ( null, null ) ;

        var first = _distances[ 0 ] ;
        var (conn, connElm) = ( first.Connector, first.ConnectorElement ) ;
        _distances.RemoveAll( d => d.IsConnector( conn ) || d.IsConnectorElement( connElm ) ) ;

        return ( conn, connElm ) ;
      }

      private class DistanceInfo
      {
        public Connector Connector { get ; }
        public ConnectorElement ConnectorElement { get ; }
        private double Distance { get ; }
        private double DirectionalDistance { get ; }
        
        public DistanceInfo( Connector connector, ConnectorElement connectorElement, double distance, double angleZ, double angleX )
        {
          Connector = connector ;
          ConnectorElement = connectorElement ;
          Distance = distance ;
          DirectionalDistance = angleZ + angleX ;
        }

        public bool IsConnector( Connector conn )
        {
          return ( conn.Owner.Id == Connector.Owner.Id && conn.Id == Connector.Id ) ;
        }

        public bool IsConnectorElement( ConnectorElement connElm )
        {
          return ( connElm.Id == ConnectorElement.Id ) ;
        }

        public static int Compare( DistanceInfo x, DistanceInfo y )
        {
          var dist = x.Distance.CompareTo( y.Distance ) ;
          if ( 0 != dist ) return dist ;

          var dir = x.DirectionalDistance.CompareTo( y.DirectionalDistance ) ;
          if ( 0 != dir ) return dir ;

          var elm = x.ConnectorElement.Id.IntegerValue.CompareTo( y.ConnectorElement.Id.IntegerValue ) ;
          if ( 0 != elm ) return elm ;

          var connElm = x.Connector.Owner.Id.IntegerValue.CompareTo( y.Connector.Owner.Id.IntegerValue ) ;
          if ( 0 != connElm ) return connElm ;

          var conn = x.Connector.Id.CompareTo( y.Connector.Id ) ;
          if ( 0 != conn ) return conn ;

          return 0 ;
        }
      }
    }


    public class ConnectorInfoClass : INotifyPropertyChanged
    {
      public bool IsEnabled { get ; }

      private bool _isSelected = false ;

      public bool IsSelected
      {
        get => _isSelected ;
        set
        {
          if ( false == IsEnabled ) return ;

          _isSelected = value ;
          NotifyPropertyChanged() ;
        }
      }

      public event PropertyChangedEventHandler? PropertyChanged ;

      private Element Element { get ; }

      private Connector? Connector { get ; }
      private ConnectorElement? ConnectorElement { get ; }

      /// <summary>
      /// ConnectorInfo for the center of an element.
      /// </summary>
      /// <param name="element">Instance.</param>
      public ConnectorInfoClass( Element element )
      {
        Element = element ;
        Connector = null ;
        ConnectorElement = null ;

        IsEnabled = true ;
      }

      public ConnectorInfoClass( FamilyInstance familyInstance, ConnectorElement? connectorElement, Connector connector, Connector? firstConnector )
      {
        Element = familyInstance ;
        Connector = connector ;
        ConnectorElement = connectorElement ;

        IsEnabled = ( null != connectorElement ) && ( false == connector.IsConnected ) && ( ( null == firstConnector ) || ( HasCompatibleType( connector, firstConnector ) && firstConnector.HasSameShape( connectorElement ) ) ) ;
      }

      public ConnectorInfoClass( Connector connector, Connector? firstConnector )
      {
        Element = connector.Owner ;
        Connector = connector ;
        ConnectorElement = null ;

        IsEnabled = ( false == connector.IsConnected ) && ( ( null == firstConnector ) || ( HasCompatibleType( connector, firstConnector ) && firstConnector.HasSameShape( connector ) ) ) ;
      }

      private static bool HasCompatibleType( Connector connector1, Connector connector2 )
      {
        return ( connector1.Domain == connector2.Domain ) && ( connector1.GetSystemType() == connector2.GetSystemType() ) ;
      }

      private void NotifyPropertyChanged( [CallerMemberName] string propertyName = "" )
      {
        PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
      }

      public override string ToString()
      {
        if ( null != ConnectorElement ) {
          return $"{ConnectorElement.Name} - φ {ConnectorElement.Radius.RevitUnitsToMillimeters() * 2} mm - {ConnectorElement.get_Parameter( BuiltInParameter.RBS_PIPE_FLOW_DIRECTION_PARAM )?.AsValueString()}" ;
        }
        else if ( null != Connector ) {
          return $"{Connector.Id} - φ {Connector.Radius.RevitUnitsToMillimeters() * 2} mm - {Connector.Direction}" ;
        }
        else {
          return "Origin of this element" ;
        }
      }

      public Connector? GetConnector()
      {
        if ( false == IsEnabled || false == IsSelected ) return null ;

        if ( null != Connector ) return Connector ;

        return null ;
      }
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      this.DialogResult = true ;
      this.Close() ;
    }

    public Connector? GetSelectedConnector()
    {
      return ConnectorList.Select( cic => cic.GetConnector() ).NonNull().FirstOrDefault() ;
    }
  }
}