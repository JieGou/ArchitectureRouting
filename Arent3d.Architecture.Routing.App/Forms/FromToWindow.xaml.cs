using System ;
using System.Windows ;
using System.Collections.ObjectModel ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  
  public partial class FromToWindow : ModalWindowBase
  {
    /*public List<string>? Diameters { get ; set ; }
    public int? DiameterIndex { get ; set ; }*/
    public ObservableCollection<FromToItems> FromToItemsList { get ; set ; }

    //public FromToWindow(Document doc, IList<double> diameters, int diameterIndex)
    //public FromToWindow( Document doc, IEnumerable<Route> allRoute )
    public FromToWindow( UIDocument uiDoc, ObservableCollection<FromToItems> fromToItemsList):base(uiDoc)
    {
      InitializeComponent() ;

      FromToItemsList = fromToItemsList ;
    }


    public class FromToItems
    {
      public string? Id { get ; set ; }
      
      //From
      public ConnectorIndicator? From { get ; set ; }
      public string? FromType { get ; set ; }
      public string? FromConnectorId { get ; set ; }
      public string? FromElementId { get ; set ; }

      
      //public string? From { get ; set ; } ここから。ConnectorIndicatorの
      //適切な値を、Textに表示できるように
      public string? ToType { get ; set ; }
      public string? ToConnectorId { get ; set ; }
      public string? ToElementId { get ; set ; }

      public string? Domain { get ; set ; }

      //SystemType
      public ObservableCollection<MEPSystemType>? SystemTypes { get ; set ; }
      public MEPSystemType? SystemType { get ; set ; }
      public int SystemTypeIndex { get ; set ; } 
      
      //CurveType
      public ObservableCollection<MEPCurveType>? CurveTypes { get ; set ; }
      public MEPCurveType? CurveType { get ; set ; }
      public int CurveTypeIndex { get ; set ; }

      public bool? Direct { get ; set ; }
      //Diameters
      public string? Diameters { get ; set ; }
      
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