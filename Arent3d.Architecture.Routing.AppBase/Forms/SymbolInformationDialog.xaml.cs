﻿using System ;
using System.Globalization ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
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

    public SymbolInformationDialog( SymbolInformationViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      CbSymbolKind.ItemsSource = viewModel.SymbolKinds ;
      CbSymbolCoordinate.ItemsSource = viewModel.SymbolCoordinates ;
      CbSymbolColor.ItemsSource = viewModel.SymbolColors ;


      PathStar.Data = LoadSymbolPathData() ;
      LabelDescription.FontSize = FontSizeDefault + viewModel.SymbolInformation.CharacterHeight ;
    }

    private Geometry LoadSymbolPathData()
    {
      var viewModel = (SymbolInformationViewModel) DataContext ;
      var symbolData = viewModel.SymbolInformation.SymbolKind == SymbolKindEnum.星.ToString() ? CreateStarData( viewModel.SymbolInformation.Height ) : CreateCircleData( viewModel.SymbolInformation.Height ) ;
      return Geometry.Parse( symbolData ) ;
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
        viewModel.AddCeedDetail( comboBox.Text ) ;
        comboBox.Text = string.Empty ;
      }
      else {
        comboBox.IsDropDownOpen = true ;
        if ( comboBox.Text.Length != 1 ) return ;
        var textBox = (TextBox) comboBox.Template.FindName( "PART_EditableTextBox", comboBox ) ;
        textBox.Select( textBox.Text.Length, 0 ) ;
      }
    }

    private void OnComboSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      var comboBox = (ComboBox) sender ;
      var viewModel = ( (SymbolInformationViewModel) DataContext ) ;
      if ( comboBox.SelectedItem != null ) {
        viewModel.AddCeedDetail( comboBox.SelectedItem.ToString() ) ;
        comboBox.SelectedItem = null ;
        comboBox.SelectedIndex = -1 ;
      }
    }

    private void OnSymbolHightKeyUp( object sender, KeyEventArgs e )
    {
      var textBox = (TextBox) sender ;
      double.TryParse( textBox.Text, out var height ) ;

      var viewModel = ( (SymbolInformationViewModel) DataContext ) ;
      viewModel.SymbolInformation.Height = height ;
      PathStar.Data = LoadSymbolPathData() ;
    }

    private void OnSymbolHeightInput( object sender, TextCompositionEventArgs e )
    {
      Regex regex = new Regex( "[^1-5]+" ) ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }

    private void OnSymbolCoordinateChange( object sender, SelectionChangedEventArgs e )
    {
      var comboBox = (ComboBox) sender ;
      switch ( comboBox.SelectedIndex ) {
        case 0 : //Top
          Canvas.SetLeft( CanvasStar, 42 ) ;
          Canvas.SetTop( CanvasStar, 50 ) ; 
          
          Canvas.SetTop( CanvasText, 0 ) ;
          LabelDescription.HorizontalContentAlignment = HorizontalAlignment.Center ;
          break ;
        case 1 : //Left
          Canvas.SetLeft( CanvasStar, 50 ) ;
          Canvas.SetTop( CanvasStar, 30 ) ;
 
          Canvas.SetTop( CanvasText, 25 ) ;
          LabelDescription.HorizontalContentAlignment = HorizontalAlignment.Left ;
          break ;
        case 2: //Middle
          Canvas.SetLeft( CanvasStar, 42 ) ;
          Canvas.SetTop( CanvasStar, 30 ) ;
 
          Canvas.SetTop( CanvasText, 25 ) ;
          LabelDescription.HorizontalContentAlignment = HorizontalAlignment.Center ;
          break ;
        case 3 : //Right
          Canvas.SetLeft( CanvasStar, 0 ) ;
          Canvas.SetTop( CanvasStar, 30 ) ;
 
          Canvas.SetTop( CanvasText, 25 ) ;
          LabelDescription.HorizontalContentAlignment = HorizontalAlignment.Right ;
          break ;
        
        default : //Bottom
          Canvas.SetLeft( CanvasStar, 42 ) ;
          Canvas.SetTop( CanvasStar, 20 ) ;
 
          Canvas.SetTop( CanvasText, 70 ) ;
          LabelDescription.HorizontalContentAlignment = HorizontalAlignment.Center ;
          break ;
      }
    }

    private void OnSymbolTextHeightInput( object sender, KeyEventArgs e )
    {
      var textBox = (TextBox) sender ;
      double.TryParse( textBox.Text, out var height ) ;

      LabelDescription.FontSize = FontSizeDefault + height ;
    }

    private void OnCheckedDescriptionChanged( object sender, RoutedEventArgs e )
    {
      var checkbox = (CheckBox) sender ;
      LabelDescription.Visibility = checkbox.IsChecked == true ? Visibility.Visible : Visibility.Hidden ;
    }

    private string CreateStarDataNotFill( double symbolHeight )
    {
      double length = symbolHeight switch
      {
        2 => 16,
        3 => 18,
        4 => 20,
        5 => 22,
        _ => 14
      } ;

      SinCos( 18, out double sin18, out double cos18 ) ;
      SinCos( 36, out double sin36, out double cos36 ) ;

      var doi18 = sin18 * length ;
      var ke18 = cos18 * length ;
      var doi36 = sin36 * length ;
      var ke36 = cos36 * length ;

      var lengthSide = (length + doi18 ) *2 ;

      string data = "M 0,0 " ;
      data += "l " + lengthSide + ",0 " ; //P1 
      data += "l -" + lengthSide * cos36 + "," + lengthSide * sin36 + " " ; //P2 
      data += "l " + lengthSide * sin18 + ",-" + lengthSide * cos18 + " " ; //P3  
      data += "l " + lengthSide * sin18 + "," + lengthSide * cos18 ; //P4
      //data += "l -" + ke36 + "," + doi36 + " " ; //P5

      data += "Z" ;
      return data ;
    }

    private static string CreateStarData( double symbolHeight )
    {
      double length = symbolHeight switch
      {
        2 => 16,
        3 => 18,
        4 => 20,
        5 => 22,
        _ => 14
      } ;

      SinCos( 18, out var sin18, out var cos18 ) ;
      SinCos( 36, out var sin36, out var cos36 ) ;

      var doi18 = sin18 * length ;
      var ke18 = cos18 * length ;
      var doi36 = sin36 * length ;
      var ke36 = cos36 * length ;

      string data = "M 0,0 " ;
      data += "l " + length + ",0 " ; //P1 
      data += "l " + doi18 + ",-" + ke18 + " " ; //P2 
      data += "l " + doi18 + "," + ke18 + " " ; //P3  
      data += "l " + length + ",0 " ; //P4
      data += "l -" + ke36 + "," + doi36 + " " ; //P5
      data += "l " + doi18 + "," + ke18 + " " ; //P6
      data += "l -" + ke36 + ",-" + doi36 + " " ; //P7
      data += "l -" + ke36 + "," + doi36 + " " ; //P8
      data += "l " + doi18 + ",-" + ke18 + " " ; //P9
      data += "Z" ;
      return data ;
    }

    private static string CreateCircleData( double symbolHeight )
    {
      if ( symbolHeight > 5 )
        symbolHeight = 5 ;
      string temp = (symbolHeight * 5).ToString( CultureInfo.InvariantCulture ) ;
      string data = "M 0," + temp ;
      data += "A " + temp + "," + temp + " 0 0 1 "+ (symbolHeight * 10).ToString( CultureInfo.InvariantCulture ) + "," + temp  ;
      data += " A " + temp + "," + temp + " 0 0 1 0," + temp ;
      return data ;
    }

    private static void SinCos( double degrees, out double sinAngle, out double cosAngle )
    {
      var angle = Math.PI * degrees / 180.0 ;
      sinAngle = Math.Sin( angle ) ;
      cosAngle = Math.Cos( angle ) ;
    }

    private void OnSymbolKindChange( object sender, SelectionChangedEventArgs e )
    {
      PathStar.Data = LoadSymbolPathData() ;
    }
  }

  public abstract class DesignSymbolInformationViewModel : SymbolInformationViewModel
  {
    protected DesignSymbolInformationViewModel( UIDocument uiDocument, Document document, SymbolInformationModel? symbolInformationModel ) : base( uiDocument, document, symbolInformationModel )
    {
    }
  }
  
  public class CustomComboBoxColumn : System.Windows.Controls.DataGridComboBoxColumn
  {
    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
      FrameworkElement fe = base.GenerateElement(cell, dataItem);
      if ( fe is System.Windows.Controls.Control control ) 
        control.Margin = new Thickness(5, 0, 0, 0); 
      
      return fe;
    }
  }
}