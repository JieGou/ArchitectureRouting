using System ;
using System.Windows ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FromToWindow : WindowBase
  {
    public ObservableCollection<FromToItems> FromToItemsList { get ; set ; }

    public FromToWindow( UIDocument uiDoc, ObservableCollection<FromToItems> fromToItemsList ) : base( uiDoc )
    {
      InitializeComponent() ;

      FromToItemsList = fromToItemsList ;
    }


    public class FromToItems
    {
      public string? Id { get ; set ; }

      //From
      public ConnectorIndicator? From { get ; set ; }
      public string? FromType => From?.ToString().Split( ':' )[ 0 ] ;
      public string? FromId => From?.ElementId.ToString() ;
      public string? FromSubId => From?.ConnectorId.ToString() ;


      //To
      public ConnectorIndicator? To { get ; set ; }
      public string? ToType => To?.ToString().Split( ':' )[ 0 ] ;
      public string? ToId => To?.ElementId.ToString() ;
      public string? ToSubId => To?.ConnectorId.ToString() ;

      //Domain
      public string? Domain { get ; set ; }

      //SystemType
      public ObservableCollection<MEPSystemType>? SystemTypes { get ; set ; }
      public MEPSystemType? SystemType { get ; set ; }
      public int SystemTypeIndex { get ; set ; }

      //CurveType
      public ObservableCollection<MEPCurveType>? CurveTypes { get ; set ; }
      public MEPCurveType? CurveType { get ; set ; }
      public int CurveTypeIndex { get ; set ; }

      //Direct
      public bool? Direct { get ; set ; }

      //Diameters
      public string? Diameters { get ; set ; }

      //PassPoint
      public string? PassPoints { get ; set ; }
    }


    private void Dilog2Buttons_OnImportOnClick( object sender, RoutedEventArgs e )
    {
      TaskDialog.Show( "test", "Import" ) ;
    }

    private void Dilog2Buttons_OnExportOnClick( object sender, RoutedEventArgs e )
    {
      TaskDialog.Show( "test", "Export" ) ;
    }

    private void Dialog3Buttons_OnOnOKClick( object sender, RoutedEventArgs e )
    {
      this.Close() ;
    }

    private void Dialog3Buttons_OnOnApplyClick( object sender, RoutedEventArgs e )
    {
      throw new NotImplementedException() ;
    }

    private void Dialog3Buttons_OnOnCancelClick( object sender, RoutedEventArgs e )
    {
      this.Close() ;
    }
  }
}