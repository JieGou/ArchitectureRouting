using System.Collections.Generic ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ComboBox = System.Windows.Controls.ComboBox ;
using TextBox = System.Windows.Controls.TextBox ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SymbolInformationDialog
  {
    private const int FontSizeDefault = 12 ;
    private static readonly Dictionary<int, string> starDataDict = new() { 
      { 1, "M 0,0 l 10,0 l 5,-10 l 5,10 l 10,0 l -7,10 l 2,10 l -10,-5 l -10,5 l 2,-10 Z" }, 
      { 2, "M 0,0 l 12,0 l 6,-12 l 6,12 l 12,0 l -8,12 l 2,12 l -12,-6 l -12,6 l 2,-12 Z" }, 
      { 3, "M 0,0 l 14,0 l 7,-14 l 7,14 l 14,0 l -9,14 l 2,14 l -14,-7 l -14,7 l 2,-14 Z" }, 
      { 4, "M 0,0 l 16,0 l 8,-16 l 8,16 l 16,0 l -10,16 l 2,16 l -16,-8 l -16,8 l 2,-16 Z" }, 
      { 5, "M 0,0 l 18,0 l 9,-18 l 9,18 l 18,0 l -11,18 l 2,18 l -18,-9 l -18,9 l 2,-18 Z" }, 
    } ;

    public SymbolInformationDialog( SymbolInformationViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      CbSymbolKind.ItemsSource = viewModel.SymbolKinds ;
      CbSymbolCoordinate.ItemsSource = viewModel.SymbolCoordinates ;
      CbSymbolColor.ItemsSource = viewModel.SymbolColors ; 
      
      PathStar.Data = Geometry.Parse(starDataDict[int.Parse(viewModel.SymbolInformation.Height.ToString())]);
      LabelDescription.FontSize = FontSizeDefault + viewModel.SymbolInformation.CharacterHeight ;
    } 
    
    private void ButtonOK_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void ButtonCancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }

    private void OnComboboxBuzaiCDEnter( object sender, KeyEventArgs e )
    {
      var comboBox = (ComboBox) sender ; 
      var viewModel = ( (SymbolInformationViewModel) DataContext ) ; 
      
      if ( e.Key == Key.Enter ) { 
        viewModel.AddCeedDetail( comboBox.Text );
        comboBox.Text = string.Empty ; 
      } else {
        comboBox.IsDropDownOpen = true ;
        if ( comboBox.Text.Length != 1 ) return ;
        var textBox = (TextBox)comboBox.Template.FindName( "PART_EditableTextBox", comboBox ) ;
        textBox.Select( textBox.Text.Length, 0 );
      }
    }

    private void OnComboSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      var comboBox = (ComboBox) sender ; 
      var viewModel = ( (SymbolInformationViewModel) DataContext ) ;
      if ( comboBox.SelectedItem != null ) {
        viewModel.AddCeedDetail( comboBox.SelectedItem.ToString() ); 
        comboBox.SelectedItem = null ;
        comboBox.SelectedIndex = -1 ;
      }
        
    }

    private void OnSymbolHightKeyUp( object sender, KeyEventArgs e )
    {
      var textBox = (TextBox) sender ;
      var height = 3;
      int.TryParse( textBox.Text, out height ) ;
      if ( ! starDataDict.ContainsKey( height ) )
        height = 3 ;
      
      PathStar.Data = Geometry.Parse(starDataDict[height]);
    }

    private void OnSymbolHeightInput( object sender, TextCompositionEventArgs e )
    {
      Regex regex = new Regex("[^1-5]+");
      e.Handled = regex.IsMatch(e.Text);
    }

    private void OnSymbolCoordinateChange( object sender, SelectionChangedEventArgs e )
    {
      var comboBox = (ComboBox) sender ;
      switch ( comboBox.SelectedIndex ) {
        case 0: //Top
          Canvas.SetLeft( CanvasStar, 40 ) ;
          Canvas.SetTop( CanvasStar, 50 ) ;
         
          Canvas.SetLeft( CanvasText, 35 ) ;
          Canvas.SetTop( CanvasText, 0 ) ;
          break;
        case 1: //Left
          Canvas.SetLeft( CanvasStar, 50 ) ;
          Canvas.SetTop( CanvasStar, 30 ) ;
         
          Canvas.SetLeft( CanvasText, 0 ) ;
          Canvas.SetTop( CanvasText, 25 ) ;
          break;
        case 2: //Right
          Canvas.SetLeft( CanvasStar, 0 ) ;
          Canvas.SetTop( CanvasStar, 30 ) ;
         
          Canvas.SetLeft( CanvasText, 60 ) ;
          Canvas.SetTop( CanvasText, 25 ) ;
          break;
        default: //Bottom
          Canvas.SetLeft( CanvasStar, 30 ) ;
          Canvas.SetTop( CanvasStar, 20 ) ;
         
          Canvas.SetLeft( CanvasText, 35 ) ;
          Canvas.SetTop( CanvasText, 60 ) ;
          break;
           
      } 
    }

    private void OnSymbolTextHeightInput( object sender, KeyEventArgs e )
    {
      var textBox = (TextBox) sender ;
      var height = 3;
      int.TryParse( textBox.Text, out height ) ;
      if ( ! starDataDict.ContainsKey( height ) )
        height = 3 ;

      LabelDescription.FontSize = FontSizeDefault + height ;
    }

    private void OnCheckedDescriptionChanged( object sender, RoutedEventArgs e )
    {
      var checkbox = (CheckBox) sender ;
      LabelDescription.Visibility = checkbox.IsChecked == true ? Visibility.Visible : Visibility.Hidden ;
    }
  }

  public abstract class DesignSymbolInformationViewModel : SymbolInformationViewModel
  {
    protected DesignSymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel, ExternalCommandData commandData ) : base( document, symbolInformationModel, commandData )
    {
    }
  }
}