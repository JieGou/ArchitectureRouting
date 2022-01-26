using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.MessageBox ;
using Style = System.Windows.Style ;
using Window = System.Windows.Window ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeeDModelDialog : Window
  {
    private readonly Document _document ;
    private CeedViewModel? _allCeeDModels ;
    private CeedViewModel? _usingCeeDModel ;
    private string _ceeDModelNumberSearch ;
    private string _modelNumberSearch ;
    public string SelectedDeviceSymbol ;
    public string SelectedCondition ;
    public string SelectedCeeDCode ;
    public string SelectedModelNumber ;
    public string SelectedFamilyType ;

    public CeeDModelDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _allCeeDModels = null ;
      _usingCeeDModel = null ;
      _ceeDModelNumberSearch = string.Empty ;
      _modelNumberSearch = string.Empty ;
      SelectedDeviceSymbol = string.Empty ;
      SelectedCondition = string.Empty ;
      SelectedCeeDCode = string.Empty ;
      SelectedModelNumber = string.Empty ;
      SelectedFamilyType = string.Empty ;

      var oldCeeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeeDStorable != null ) {
        LoadData( oldCeeDStorable ) ;
      }

      Style rowStyle = new Style( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( DataGridRow.MouseDoubleClickEvent,
        new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
    }

    private void Row_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (CeedModel) DtGrid.SelectedValue ;
      SelectedDeviceSymbol = selectedItem.GeneralDisplayDeviceSymbol ;
      SelectedCondition = selectedItem.Condition ;
      SelectedCeeDCode = selectedItem.CeeDSetCode ;
      SelectedModelNumber = selectedItem.ModelNumber ;
      SelectedFamilyType = selectedItem.FamilyTypeName ;
      if ( string.IsNullOrEmpty( SelectedDeviceSymbol ) ) return ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Reset( object sender, RoutedEventArgs e )
    {
      CmbCeeDModelNumbers.SelectedIndex = -1 ;
      CmbCeeDModelNumbers.Text = "" ;
      CmbModelNumbers.SelectedIndex = -1 ;
      CmbModelNumbers.Text = "" ;
      var ceeDViewModels = CbShowOnlyUsingCode.IsChecked == true ? _usingCeeDModel : _allCeeDModels ;
      if ( ceeDViewModels != null )
        LoadData( ceeDViewModels ) ;
    }

    private void CmbCeeDModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _ceeDModelNumberSearch = ! string.IsNullOrEmpty( CmbCeeDModelNumbers.Text ) ? CmbCeeDModelNumbers.Text : string.Empty ;
    }

    private void CmbModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _modelNumberSearch = ! string.IsNullOrEmpty( CmbModelNumbers.Text ) ? CmbModelNumbers.Text : string.Empty ;
    }

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels == null && _usingCeeDModel == null ) return ;
      var ceeDViewModels = CbShowOnlyUsingCode.IsChecked == true ? _usingCeeDModel : _allCeeDModels ;
      if ( ceeDViewModels == null ) return ;
      if ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) ) {
        this.DataContext = ceeDViewModels ;
      }
      else {
        List<CeedModel> ceeDModels = new List<CeedModel>() ;
        switch ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) ) {
          case false when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) && c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
          case false when string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) ).ToList() ;
            break ;
          case true when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
        }

        var ceeDModelsSearch = new CeedViewModel( ceeDViewModels.CeedStorable, ceeDModels ) ;
        this.DataContext = ceeDModelsSearch ;
      }
    }

    private void Button_SymbolRegistration( object sender, RoutedEventArgs e )
    {
      var ceeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable != null ) {
        OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx", Multiselect = false } ;
        string filePath = string.Empty ;
        if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
          filePath = openFileDialog.FileName ;
        }

        if ( string.IsNullOrEmpty( filePath ) ) return ;
        var modelNumberToUse = ExcelToModelConverter.GetModelNumberToUse( filePath ) ;
        if ( ! modelNumberToUse.Any() ) return ;
        List<CeedModel> usingCeeDModel = new List<CeedModel>() ;
        foreach ( var modelNumber in modelNumberToUse ) {
          var ceeDModels = ceeDStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
          usingCeeDModel.AddRange( ceeDModels ) ;
        }

        usingCeeDModel = usingCeeDModel.Distinct().ToList() ;
        _usingCeeDModel = new CeedViewModel( ceeDStorable, usingCeeDModel ) ;
        LoadData( _usingCeeDModel ) ;
        CbShowOnlyUsingCode.Visibility = Visibility.Visible ;
        CbShowOnlyUsingCode.IsChecked = true ;
        if ( _usingCeeDModel == null || ! _usingCeeDModel.CeedModels.Any() ) return ;
        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceeDStorable.CeedModelUsedData = _usingCeeDModel.CeedModels ;
          ceeDStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
      else {
        MessageBox.Show( "Please read csv.", "Message" ) ;
      }
    }

    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      MessageBox.Show( "Please select 【CeeD】セットコード一覧表 file.", "Message" ) ;
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
      string filePath = string.Empty ;
      string fileEquipmentSymbolsPath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
        MessageBox.Show( "Please select 機器記号一覧表 file.", "Message" ) ;
        OpenFileDialog openFileEquipmentSymbolsDialog = new OpenFileDialog { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
        if ( openFileEquipmentSymbolsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
          fileEquipmentSymbolsPath = openFileEquipmentSymbolsDialog.FileName ;
        }
      }
      if ( string.IsNullOrEmpty( filePath ) || string.IsNullOrEmpty( fileEquipmentSymbolsPath ) ) return ;
      CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
      {
        List<CeedModel> ceeDModelData = ExcelToModelConverter.GetAllCeeDModelNumber( filePath, fileEquipmentSymbolsPath ) ;
        if ( ! ceeDModelData.Any() ) return ;
        ceeDStorable.CeedModelData = ceeDModelData ;
        ceeDStorable.CeedModelUsedData = new List<CeedModel>() ;
        LoadData( ceeDStorable ) ;
        CbShowOnlyUsingCode.Visibility = Visibility.Hidden ;
        CbShowOnlyUsingCode.IsChecked = false ;

        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceeDStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
      
      try {
        using Transaction t1 = new Transaction( _document, "Load connector's families" ) ;
        t1.Start() ;
        _document.MakeCertainAllConnectorFamilies() ;
        t1.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    private void LoadData( CeedStorable ceeDStorable )
    {
      var viewModel = new ViewModel.CeedViewModel( ceeDStorable ) ;
      this.DataContext = viewModel ;
      _allCeeDModels = viewModel ;
      CmbCeeDModelNumbers.ItemsSource = viewModel.CeeDModelNumbers ;
      CmbModelNumbers.ItemsSource = viewModel.ModelNumbers ;
    }

    private void ShowOnlyUsingCode_Checked( object sender, RoutedEventArgs e )
    {
      if ( _usingCeeDModel == null ) return ;
      LoadData( _usingCeeDModel ) ;
    }

    private void ShowOnlyUsingCode_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels == null ) return ;
      LoadData( _allCeeDModels ) ;
    }

    private void LoadData( CeedViewModel ceeDViewModel )
    {
      this.DataContext = ceeDViewModel ;
      CmbCeeDModelNumbers.ItemsSource = ceeDViewModel.CeeDModelNumbers ;
      CmbModelNumbers.ItemsSource = ceeDViewModel.ModelNumbers ;
    }
  }
}