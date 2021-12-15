using System ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectDeviceSymbol : Window
  {
    public ObservableCollection<DeviceSymbolInfo> DeviceSymbolList { get ; } = new() ;

    public SelectDeviceSymbol( string generalDisplayDeviceSymbolList )
    {
      InitializeComponent() ;

      foreach ( var deviceSymbol in generalDisplayDeviceSymbolList.Split( Environment.NewLine.ToCharArray() ) )
        DeviceSymbolList.Add( new DeviceSymbolInfo( deviceSymbol ) ) ;
    }

    public class DeviceSymbolInfo : INotifyPropertyChanged
    {
      private bool _isSelected ;
      private readonly string _deviceSymbolName ;

      public DeviceSymbolInfo( string deviceSymbolName )
      {
        _deviceSymbolName = deviceSymbolName ;
      }

      public bool IsSelected
      {
        get => _isSelected ;
        set
        {
          _isSelected = value ;
          NotifyPropertyChanged() ;
        }
      }

      public event PropertyChangedEventHandler? PropertyChanged ;

      private void NotifyPropertyChanged( [CallerMemberName] string propertyName = "" )
      {
        PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
      }

      public override string ToString()
      {
        return _deviceSymbolName ;
      }
    }

    private void OffsetButtons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }

    private void OffsetButtons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    public string GetSelectedDeviceSymbol()
    {
      var selectedDeviceSymbol = string.Empty ;
      var deviceSymbol = DeviceSymbolList.FirstOrDefault( x => x.IsSelected ) ;
      if ( deviceSymbol != null )
        selectedDeviceSymbol = deviceSymbol.ToString() ;

      return selectedDeviceSymbol ;
    }
  }
}