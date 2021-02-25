using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using System ;
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
        var familyInstanceTransform = familyInstance.GetTotalTransform() ;
        var familyDocument = familyInstance.Document.EditFamily( familyInstance.Symbol.Family ) ;
        foreach ( var conn in familyDocument.GetAllElements<ConnectorElement>().Where( IsTargetConnectorElement ) ) {
          ConnectorList.Add( new ConnectorInfoClass( familyInstance, familyInstanceTransform, conn, _firstConnector ) ) ;
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

    private static bool IsTargetConnectorElement( ConnectorElement el )
    {
      return el.Domain switch
      {
        Domain.DomainPiping => true,
        Domain.DomainHvac => true,
        _ => false
      } ;
    }

    private static bool IsTargetConnector( Connector el )
    {
      if ( false == el.IsAnyEnd() ) return false ;

      return el.Domain switch
      {
        Domain.DomainPiping => true,
        Domain.DomainHvac => true,
        _ => false
      } ;
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

      private XYZ? ConnectorPosition { get ; }
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
        ConnectorPosition = null ;

        IsEnabled = true ;
      }

      public ConnectorInfoClass( FamilyInstance familyInstance, Transform familyInstanceTransform, ConnectorElement connectorElement, Connector? firstElement )
      {
        Element = familyInstance ;
        Connector = null ;
        ConnectorElement = connectorElement ;
        ConnectorPosition = familyInstanceTransform.OfPoint( connectorElement.Origin ) ;

        IsEnabled = ( null == firstElement ) || ( HasCompatibleType( firstElement ) && firstElement.HasSameShape( ConnectorElement ) ) ;
      }

      public ConnectorInfoClass( Connector connector, Connector? firstElement )
      {
        Element = connector.Owner ;
        Connector = connector ;
        ConnectorElement = null ;
        ConnectorPosition = connector.Origin ;

        IsEnabled = ( false == connector.IsConnected ) && ( ( null == firstElement ) || ( HasCompatibleType( firstElement ) && firstElement.HasSameShape( connector ) ) ) ;
      }

      private void NotifyPropertyChanged( [CallerMemberName] string propertyName = "" )
      {
        PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
      }

      public override string ToString()
      {
        if ( null != ConnectorElement ) {
          return $"{ConnectorElement.Name} - {UnitUtils.ConvertFromInternalUnits( ConnectorElement.Radius, UnitTypeId.Millimeters ) * 2} - {ConnectorElement.get_Parameter( BuiltInParameter.RBS_PIPE_FLOW_DIRECTION_PARAM )?.AsValueString()}" ;
        }
        else if ( null != Connector ) {
          return $"{Connector.Id} - {UnitUtils.ConvertFromInternalUnits( Connector.Radius, UnitTypeId.Millimeters ) * 2} - {Connector.Direction}" ;
        }
        else {
          return "Origin of this element" ;
        }
      }

      public Connector? GetConnector()
      {
        if ( false == IsEnabled || false == IsSelected ) return null ;

        if ( null != Connector ) return Connector ;

        if ( null == ConnectorElement ) return null ;
        return Element.GetConnectors().FirstOrDefault( IsMatch ) ;
      }

      private bool IsMatch( Connector connector )
      {
        return HasCompatibleType( connector ) && HasSamePosition( connector ) ;
      }

      private bool HasCompatibleType( Connector connector )
      {
        if ( false == connector.IsAnyEnd() ) return false ;
        if ( null == ConnectorElement ) return true ;

        if ( connector.Domain != ConnectorElement.Domain ) return false ;

        if ( ConnectorElement.SystemClassification != MEPSystemClassification.Global ) {
          if ( connector.GetSystemTypeName() != ConnectorElement.SystemClassification.ToString() ) return false ;
        }

        return true ;
      }

      private bool HasSamePosition( Connector connector )
      {
        return connector.Origin.IsAlmostEqualTo( ConnectorPosition ) ;
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