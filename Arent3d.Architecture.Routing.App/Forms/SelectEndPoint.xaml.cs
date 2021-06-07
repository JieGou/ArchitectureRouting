using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using System ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;
using System.Windows ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Architecture.Routing.EndPoints;

namespace Arent3d.Architecture.Routing.App.Forms
{
  /// <summary>
  /// Interaction logic for SelectConnector.xaml
  /// </summary>
  public partial class SelectEndPoint : Window
  {
    private readonly IEndPoint? _firstEndPoint;

    public ObservableCollection<EndPointInfoClass> EndPointList { get ; } = new() ;

    public SelectEndPoint( IEndPoint[] points, IEndPoint? firstEndPoint = null )
    {
      InitializeComponent() ;

      _firstEndPoint = firstEndPoint;

        foreach ( IEndPoint conn in points )  {
            EndPointList.Add( new EndPointInfoClass(conn ) ) ;
        }

      this.Left = 0 ;
      this.Top += 10 ;
    }

    public class EndPointInfoClass : INotifyPropertyChanged
    {

        private bool _isSelected = false;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        //private Element Element { get ; }

        private XYZ? ConnectorPosition { get; }
        private IEndPoint? Pointer { get; }

        public EndPointInfoClass( IEndPoint point )
        {
            Pointer = point;
            ConnectorPosition = point.RoutingStartPosition;

        }

        private void NotifyPropertyChanged( [CallerMemberName] string propertyName = "" )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        public IEndPoint? GetEndPoint()
        {
            if ( false == IsSelected )
                return null;

            if ( null != Pointer )
                return Pointer;

            return null;
        }
        public override string ToString()
        {
            if ( null != Pointer ) {
                return $"{Pointer.TypeName} - {ConnectorPosition?.X}, {ConnectorPosition?.Y}, {ConnectorPosition?.Z}";
            }
            else {
                return "";
            }

        }
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      this.DialogResult = true ;
      this.Close() ;
    }

    public IEndPoint GetSelectedEndPoint()
    {
        return EndPointList.Select( cic => cic.GetEndPoint() ).NonNull().FirstOrDefault();
    }
  }
}