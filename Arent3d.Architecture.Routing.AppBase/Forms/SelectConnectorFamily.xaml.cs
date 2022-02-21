using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.IO ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows ;
using System.Windows.Forms ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.Forms.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectConnectorFamily : Window
  {
    public ObservableCollection<ConnectorFamilyInfo> ConnectorFamilyList { get ; } = new() ;

    public SelectConnectorFamily( )
    {
      InitializeComponent() ;
      LoadConnectorFamilyList() ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_ImportFamily( object sender, RoutedEventArgs e )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Family files (*.rfa)|*.rfa", Multiselect = false } ;
      string sourcePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        sourcePath = openFileDialog.FileName ;
      }

      if ( string.IsNullOrEmpty( sourcePath ) ) return ;
      var fileName = Path.GetFileName( sourcePath ) ;
      var destPath = ConnectorFamilyManager.GetFolderPath() ;
      try {
        if ( ! Directory.Exists( destPath ) ) Directory.CreateDirectory( destPath ) ;
        destPath = ConnectorFamilyManager.GetFamilyPath( fileName ) ;
        File.Copy( sourcePath, destPath, true ) ;
        if ( ! File.Exists( destPath ) ) return ;
        var isExistedFileName = ConnectorFamilyList.FirstOrDefault( f => f.ToString() == fileName ) != null ;
        if ( ! isExistedFileName ) ConnectorFamilyList.Add( new ConnectorFamilyInfo( fileName ) ) ;
      }
      catch {
        MessageBox.Show( "Load connector's family failed.", "Error" ) ;
        DialogResult = false ;
        Close() ;
      }
    }

    private void LoadConnectorFamilyList()
    {
      var path = ConnectorFamilyManager.GetFolderPath() ;
      if ( ! Directory.Exists( path ) ) return ;
      string[] files = Directory.GetFiles( path ) ;
      foreach ( string s in files ) {
        var fileName = Path.GetFileName( s ) ;
        if ( fileName.Contains( ".rfa" ) )
          ConnectorFamilyList.Add( new ConnectorFamilyInfo( fileName ) ) ;
      }

      if ( ConnectorFamilyList.Any() ) ConnectorFamilyList.First().IsSelected = true ;
    }

    public class ConnectorFamilyInfo : INotifyPropertyChanged
    {
      private bool _isSelected ;
      private readonly string _connectorFamilyName ;

      public ConnectorFamilyInfo( string connectorFamilyName )
      {
        _connectorFamilyName = connectorFamilyName ;
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
        return _connectorFamilyName ;
      }
    }
  }
}