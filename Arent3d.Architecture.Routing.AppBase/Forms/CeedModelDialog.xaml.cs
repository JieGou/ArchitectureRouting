using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using DataGridCell = System.Windows.Controls.DataGridCell ;
using KeyEventArgs = System.Windows.Input.KeyEventArgs ;
using MessageBox = System.Windows.MessageBox ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;
using Style = System.Windows.Style ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeedModelDialog
  {
    private const string HeaderCeedModelNumberColumn = "CeeD型番" ;
    private readonly DataGridColumn? _ceedModelNumberColumn ;
    private readonly Document _document ;
    private CeedViewModel? _allCeedModels ;
    private CeedViewModel? _usingCeedModel ;
    private string _ceedModelNumberSearch ;
    private string _modelNumberSearch ;
    private bool _isShowCeedModelNumber ;
    private bool _isShowOnlyUsingCode ;
    private CeedModel? _selectedCeedModel ;
    private List<CeedModel> _oldCeedModels ;
    private List<int> _rowIndex ;
    private List<string> _cellIndex ;
    public string SelectedDeviceSymbol { get ; private set ; }
    public string SelectedCondition { get ; private set ; }
    public string SelectedCeedCode { get ; private set ; }
    public string SelectedModelNumber { get ; private set ; }
    public string SelectedFloorPlanType { get ; private set ; }

    public CeedModelDialog( UIApplication uiApplication ) : base( uiApplication )
    {
      InitializeComponent() ;
      _document = uiApplication.ActiveUIDocument.Document ;
      _allCeedModels = null ;
      _usingCeedModel = null ;
      _selectedCeedModel = null ;
      _ceedModelNumberSearch = string.Empty ;
      _modelNumberSearch = string.Empty ;
      SelectedDeviceSymbol = string.Empty ;
      SelectedCondition = string.Empty ;
      SelectedCeedCode = string.Empty ;
      SelectedModelNumber = string.Empty ;
      SelectedFloorPlanType = string.Empty ;
      _isShowCeedModelNumber = false ;
      _isShowOnlyUsingCode = false ;
      _oldCeedModels = new List<CeedModel>() ;
      _rowIndex = new List<int>() ;
      _cellIndex = new List<string>() ;

      var oldCeedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeedStorable != null ) {
        LoadData( oldCeedStorable ) ;
        _isShowCeedModelNumber = oldCeedStorable.IsShowCeedModelNumber ;
        _isShowOnlyUsingCode = oldCeedStorable.IsShowOnlyUsingCode ;
        var viewModel = new CeedViewModel( oldCeedStorable ) ;
        _oldCeedModels = viewModel.CeedModels ;
        _rowIndex = oldCeedStorable.RowIndex ;
        _cellIndex = oldCeedStorable.CellIndex ;
      }

      _ceedModelNumberColumn = DtGrid.Columns.SingleOrDefault( c => c.Header.ToString() == HeaderCeedModelNumberColumn ) ;
      CbShowCeedModelNumber.IsChecked = _isShowCeedModelNumber ;

      BtnReplaceSymbol.IsEnabled = false ;

      Style rowStyle = new( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      rowStyle.Setters.Add( new EventSetter( MouseLeftButtonUpEvent, new MouseButtonEventHandler( Row_MouseLeftButtonUp ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
      
      Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, EventArgs args){
      if (! _oldCeedModels.Any() ) {
        CbShowDiff.IsChecked = false ;
      }
      else {
        CbShowDiff.IsChecked = true ;
        ChangeColorInit( _rowIndex, _cellIndex );
      }

      Loaded -= OnLoaded;
    }

    private void Row_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
    {
      BtnReplaceSymbol.IsEnabled = false ;
      if ( ( (DataGridRow) sender ).DataContext is not CeedModel ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return ;
      }

      _selectedCeedModel = ( (DataGridRow) sender ).DataContext as CeedModel ;
      BtnReplaceSymbol.IsEnabled = true ;
    }

    private void Row_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (CeedModel) DtGrid.SelectedValue ;
      SelectedDeviceSymbol = selectedItem.GeneralDisplayDeviceSymbol ;
      SelectedCondition = selectedItem.Condition ;
      SelectedCeedCode = selectedItem.CeedSetCode ;
      SelectedModelNumber = selectedItem.ModelNumber ;
      SelectedFloorPlanType = selectedItem.FloorPlanType ;
      if ( string.IsNullOrEmpty( SelectedDeviceSymbol ) ) return ;
      SaveCeedModelNumberDisplayAndOnlyUsingCodeState() ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      SaveCeedModelNumberDisplayAndOnlyUsingCodeState() ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_Reset( object sender, RoutedEventArgs e )
    {
      CmbCeedModelNumbers.SelectedIndex = -1 ;
      CmbCeedModelNumbers.Text = "" ;
      CmbModelNumbers.SelectedIndex = -1 ;
      CmbModelNumbers.Text = "" ;
      var ceedViewModels = CbShowOnlyUsingCode.IsChecked == true ? _usingCeedModel : _allCeedModels ;
      if ( ceedViewModels != null )
        LoadData( ceedViewModels ) ;
    }

    private void CmbCeedModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _ceedModelNumberSearch = ! string.IsNullOrEmpty( CmbCeedModelNumbers.Text ) ? CmbCeedModelNumbers.Text : string.Empty ;
    }

    private void CmbModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _modelNumberSearch = ! string.IsNullOrEmpty( CmbModelNumbers.Text ) ? CmbModelNumbers.Text : string.Empty ;
    }

    private void CmbModelNumbers_KeyDown( object sender, KeyEventArgs e )
    {
      if ( e.Key == Key.Enter ) {
        SearchCeedModels() ;
      }
    }

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      SearchCeedModels() ;
    }

    private void SearchCeedModels()
    {
      var ceedViewModels = CbShowOnlyUsingCode.IsChecked == true ? _usingCeedModel : _allCeedModels ;
      if ( ceedViewModels == null ) return ;
      if ( string.IsNullOrEmpty( _ceedModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) ) {
        DtGrid.ItemsSource = ceedViewModels.CeedModels ;
      }
      else {
        var ceedModels = new List<CeedModel>() ;
        if ( ! string.IsNullOrEmpty( _ceedModelNumberSearch ) && ! string.IsNullOrEmpty( _modelNumberSearch ) )
          ceedModels = ceedViewModels.CeedModels.Where( c => c.CeedModelNumber.Contains( _ceedModelNumberSearch ) && c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
        else if ( ! string.IsNullOrEmpty( _ceedModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) )
          ceedModels = ceedViewModels.CeedModels.Where( c => c.CeedModelNumber.Contains( _ceedModelNumberSearch ) ).ToList() ;
        else if ( string.IsNullOrEmpty( _ceedModelNumberSearch ) && ! string.IsNullOrEmpty( _modelNumberSearch ) )
          ceedModels = ceedViewModels.CeedModels.Where( c => c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;

        DtGrid.ItemsSource = ceedModels ;
      }
    }

    private void Button_SymbolRegistration( object sender, RoutedEventArgs e )
    {
      var ceedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable != null && ceedStorable.CeedModelData.Any() ) {
        OpenFileDialog openFileDialog = new() { Filter = "Csv files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx", Multiselect = false } ;
        string filePath = string.Empty ;
        if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
          filePath = openFileDialog.FileName ;
        }

        if ( string.IsNullOrEmpty( filePath ) ) return ;
        var modelNumberToUse = ExcelToModelConverter.GetModelNumberToUse( filePath ) ;
        if ( ! modelNumberToUse.Any() ) return ;
        List<CeedModel> usingCeedModel = new() ;
        foreach ( var modelNumber in modelNumberToUse ) {
          var ceedModels = ceedStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
          usingCeedModel.AddRange( ceedModels ) ;
        }

        usingCeedModel = usingCeedModel.Distinct().ToList() ;
        _usingCeedModel = new CeedViewModel( ceedStorable, usingCeedModel ) ;
        LoadData( _usingCeedModel ) ;
        CbShowOnlyUsingCode.Visibility = Visibility.Visible ;
        CbShowOnlyUsingCode.IsChecked = true ;
        _isShowOnlyUsingCode = true ;
        if ( _usingCeedModel == null || ! _usingCeedModel.CeedModels.Any() ) return ;
        try {
          using Transaction t = new( _document, "Save data" ) ;
          t.Start() ;
          ceedStorable.CeedModelUsedData = _usingCeedModel.CeedModels ;
          ceedStorable.IsShowOnlyUsingCode = true ;
          ceedStorable.Save() ;
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
      OpenFileDialog openFileDialog = new() { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
      string filePath = string.Empty ;
      string fileEquipmentSymbolsPath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
        MessageBox.Show( "Please select 機器記号一覧表 file.", "Message" ) ;
        OpenFileDialog openFileEquipmentSymbolsDialog = new() { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
        if ( openFileEquipmentSymbolsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
          fileEquipmentSymbolsPath = openFileEquipmentSymbolsDialog.FileName ;
        }
      }

      if ( string.IsNullOrEmpty( filePath ) || string.IsNullOrEmpty( fileEquipmentSymbolsPath ) ) return ;
      using var progress = ProgressBar.ShowWithNewThread( UIApplication ) ;
      progress.Message = "Loading data..." ;
      CeedStorable ceedStorable = _document.GetCeedStorable() ;
      {
        List<CeedModel> ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( filePath, fileEquipmentSymbolsPath ) ;
        if ( ! ceedModelData.Any() ) return ;
        ceedStorable.CeedModelData = ceedModelData ;
        ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
        ceedStorable.IsShowOnlyUsingCode = false ;
        LoadData( ceedStorable ) ;
        CbShowOnlyUsingCode.Visibility = Visibility.Hidden ;
        CbShowOnlyUsingCode.IsChecked = false ;
        _isShowOnlyUsingCode = false ;
        try {
          using Transaction t = new( _document, "Save data" ) ;
          t.Start() ;
          using ( var progressData = progress?.Reserve( 0.5 ) ) {
            ceedStorable.Save() ;
            progressData?.ThrowIfCanceled() ;
          }

          using ( var progressData = progress?.Reserve( 0.9 ) ) {
            _document.MakeCertainAllConnectorFamilies() ;
            progressData?.ThrowIfCanceled() ;
          }

          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
    }

    private void Button_ReplaceSymbol( object sender, RoutedEventArgs e )
    {
      var selectConnectorFamilyDialog = new SelectConnectorFamily( _document ) ;
      selectConnectorFamilyDialog.ShowDialog() ;
      if ( ! ( selectConnectorFamilyDialog.DialogResult ?? false ) ) return ;
      var selectedConnectorFamily = selectConnectorFamilyDialog.ConnectorFamilyList.SingleOrDefault( f => f.IsSelected ) ;
      if ( selectedConnectorFamily == null ) {
        MessageBox.Show( "No connector family selected.", "Error" ) ;
        return ;
      }

      var connectorFamilyFileName = selectedConnectorFamily.ToString() ;
      var connectorFamilyName = connectorFamilyFileName.Replace( ".rfa", "" ) ;
      if ( _selectedCeedModel == null || string.IsNullOrEmpty( connectorFamilyFileName ) ) return ;

      using var progress = ProgressBar.ShowWithNewThread( UIApplication ) ;
      progress.Message = "Processing......." ;

      using ( var progressData = progress.Reserve( 0.5 ) ) {
        UpdateCeedStorableAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        progressData?.ThrowIfCanceled() ;
      }

      using ( var progressData = progress.Reserve( 0.9 ) ) {
        UpdateDataGridAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        BtnReplaceSymbol.IsEnabled = false ;
        progressData?.ThrowIfCanceled() ;
      }

      progress.Finish() ;
      MessageBox.Show( "正常にモデルを置き換えました。", "Message" ) ;
    }

    private void Button_ReplaceMultipleSymbols( object sender, RoutedEventArgs e )
    {
      CeedViewModel.ReplaceMultipleSymbols( _document, UIApplication, ref _allCeedModels, ref _usingCeedModel, ref DtGrid ) ;
    }

    private void LoadData( CeedStorable ceedStorable )
    {
      var viewModel = new CeedViewModel( ceedStorable ) ;
      DataContext = viewModel ;
      _allCeedModels = viewModel ;
      DtGrid.ItemsSource = viewModel.CeedModels ;
      CmbCeedModelNumbers.ItemsSource = viewModel.CeedModelNumbers ;
      CmbModelNumbers.ItemsSource = viewModel.ModelNumbers ;
      CheckDiff(viewModel.CeedModels) ;
      CbShowDiff.IsChecked = false ;
      if ( ! ceedStorable.CeedModelUsedData.Any() ) return ;
      _usingCeedModel = new CeedViewModel( ceedStorable, ceedStorable.CeedModelUsedData ) ;
      CbShowOnlyUsingCode.Visibility = Visibility.Visible ;
      CbShowOnlyUsingCode.IsChecked = ceedStorable.IsShowOnlyUsingCode ;
    }

    private void ShowOnlyUsingCode_Checked( object sender, RoutedEventArgs e )
    {
      if ( _usingCeedModel == null ) return ;
      LoadData( _usingCeedModel ) ;
      _isShowOnlyUsingCode = true ;
    }

    private void ShowOnlyUsingCode_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _allCeedModels == null ) return ;
      LoadData( _allCeedModels ) ;
      _isShowOnlyUsingCode = false ;
    }

    private void ShowCeedModelNumberColumn_Checked( object sender, RoutedEventArgs e )
    {
      if ( _ceedModelNumberColumn != null ) {
        _ceedModelNumberColumn.Visibility = Visibility.Visible ;
      }

      LbCeedModelNumbers.Visibility = Visibility.Visible ;
      CmbCeedModelNumbers.Visibility = Visibility.Visible ;
      _isShowCeedModelNumber = true ;
    }

    private void ShowCeedModelNumberColumn_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _ceedModelNumberColumn != null ) {
        _ceedModelNumberColumn.Visibility = Visibility.Hidden ;
      }

      LbCeedModelNumbers.Visibility = Visibility.Hidden ;
      CmbCeedModelNumbers.Visibility = Visibility.Hidden ;
      _isShowCeedModelNumber = false ;
    }

    private void LoadData( CeedViewModel ceedViewModel )
    {
      DataContext = ceedViewModel ;
      DtGrid.ItemsSource = ceedViewModel.CeedModels ;
      CmbCeedModelNumbers.ItemsSource = ceedViewModel.CeedModelNumbers ;
      CmbModelNumbers.ItemsSource = ceedViewModel.ModelNumbers ;
    }

    private void SaveCeedModelNumberDisplayAndOnlyUsingCodeState()
    {
      var ceedStorable = _document.GetCeedStorable() ;
      try {
        using Transaction t = new( _document, "Save data" ) ;
        t.Start() ;
        ceedStorable.IsShowCeedModelNumber = _isShowCeedModelNumber ;
        ceedStorable.IsShowOnlyUsingCode = _isShowOnlyUsingCode ;
        ceedStorable.RowIndex = _rowIndex ;
        ceedStorable.CellIndex = _cellIndex ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    private void UpdateCeedStorableAfterReplaceFloorPlanSymbol( string connectorFamilyName )
    {
      var ceedStorable = _document.GetAllStorables<CeedStorable>().First() ;
      if ( ceedStorable == null ) return ;
      if ( _allCeedModels != null ) {
        var ceedModel = _allCeedModels.CeedModels.First( c => c.CeedSetCode == _selectedCeedModel!.CeedSetCode && c.GeneralDisplayDeviceSymbol == _selectedCeedModel.GeneralDisplayDeviceSymbol && c.ModelNumber == _selectedCeedModel.ModelNumber ) ;
        if ( ceedModel != null ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          ceedStorable.CeedModelData = _allCeedModels.CeedModels ;
        }
      }

      if ( _usingCeedModel != null ) {
        var ceedModel = _usingCeedModel.CeedModels.FirstOrDefault( c => c.CeedSetCode == _selectedCeedModel!.CeedSetCode && c.GeneralDisplayDeviceSymbol == _selectedCeedModel.GeneralDisplayDeviceSymbol && c.ModelNumber == _selectedCeedModel.ModelNumber ) ;
        if ( ceedModel != null ) {
          ceedModel.FloorPlanType = connectorFamilyName ;
          ceedStorable.CeedModelUsedData = _usingCeedModel.CeedModels ;
        }
      }

      try {
        using Transaction t = new( _document, "Save CeeD data" ) ;
        t.Start() ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
      }
    }

    private void UpdateDataGridAfterReplaceFloorPlanSymbol( string floorPlanType )
    {
      if ( DtGrid.ItemsSource is not List<CeedModel> newCeedModels ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return ;
      }

      var ceedModel = newCeedModels.First( c => c == _selectedCeedModel ) ;
      if ( ceedModel == null ) return ;
      ceedModel.FloorPlanType = floorPlanType ;
      DtGrid.ItemsSource = new List<CeedModel>( newCeedModels ) ;
    }
    
    private void ChangeColor()
    {
      for ( int i = 0 ; i < DtGrid.Items.Count ; i++ ) {
        var row = CeedViewModel.GetRow( DtGrid, i ) ;
        CeedModel item = (CeedModel) row.Item ;
        var oldCeedModels = _oldCeedModels ;
        var existCeedModels = oldCeedModels.Where( x =>
          x.CeedSetCode == item.CeedSetCode &&
          x.CeedModelNumber == item.CeedModelNumber ).ToList() ;
        var itemExistCeedModel = existCeedModels.Find( x =>
          x.CeedSetCode == item.CeedSetCode &&
          x.CeedModelNumber == item.CeedModelNumber &&
          x.GeneralDisplayDeviceSymbol == item.GeneralDisplayDeviceSymbol &&
          x.ModelNumber == item.ModelNumber ) ;
        if ( itemExistCeedModel != null ) {
          CeedViewModel.SetCellColor( DtGrid, Brushes.Red,
            string.IsNullOrEmpty( itemExistCeedModel.FloorPlanSymbol )
              ? itemExistCeedModel.Base64FloorPlanImages
              : itemExistCeedModel.FloorPlanSymbol,
            string.IsNullOrEmpty( item.FloorPlanSymbol )
              ? item.Base64FloorPlanImages
              : item.FloorPlanSymbol, row, 4 ) ;
          CeedViewModel.SetCellColor( DtGrid, Brushes.Red,
            string.IsNullOrEmpty( itemExistCeedModel.InstrumentationSymbol )
              ? itemExistCeedModel.Base64InstrumentationImageString
              : itemExistCeedModel.InstrumentationSymbol,
            string.IsNullOrEmpty( item.InstrumentationSymbol )
              ? item.Base64InstrumentationImageString
              : item.InstrumentationSymbol, row, 5 ) ;
          CeedViewModel.SetCellColor( DtGrid, Brushes.Red, itemExistCeedModel.Condition,
            item.Condition, row, 6 ) ;
        }
        else {
          row.Background = Brushes.Orange ;
        }
      }
    }
    
    private void UnChangeColor()
    {
      for ( int i = 0 ; i < DtGrid.Items.Count ; i++ ) {
        var row = CeedViewModel.GetRow( DtGrid, i ) ;
        for ( int j = 0 ; j < 7 ; j++ ) {
          CeedViewModel.GetCell( DtGrid,row, j )?.ClearValue( DataGridCell.BackgroundProperty );
        }
        row.ClearValue( DataGridRow.BackgroundProperty );
      }
    }
    
    private void ShowDiff_Checked( object sender, RoutedEventArgs e )
    {
      ChangeColorInit( _rowIndex, _cellIndex );
     //ChangeColor();
      CbShowDiff.IsChecked = true;
    }

    private void ShowDiff_UnChecked( object sender, RoutedEventArgs e )
    {
      UnChangeColor() ;
      CbShowDiff.IsChecked = false ;
    }

    private void ChangeColorInit( List<int> rowIndex, List<string> cellIndex )
    {
      foreach ( var index in rowIndex ) {
        var row = CeedViewModel.GetRow( DtGrid, index ) ;
        row.Background = Brushes.Orange ;
      }

      foreach ( var index in cellIndex ) {
        var value = index.Split( ',' ) ;
        var rowCellIndex = Int32.Parse( value[ 0 ] ) ;
        var columnCellIndex = Int32.Parse( value[ 1 ] ) ;
        var row = CeedViewModel.GetRow( DtGrid, rowCellIndex ) ;
        CeedViewModel.SetRedCellColor( DtGrid, row, columnCellIndex ) ;
      }
    }
    
    private void CheckDiff(List<CeedModel> ceedModels)
    {
      _rowIndex.Clear();
      _cellIndex.Clear();
      for ( int i = 0 ; i < ceedModels.Count() ; i++ ) {
        CeedModel item = ceedModels[ i ] ;
        var oldCeedModels = _oldCeedModels ;
        var existCeedModels = oldCeedModels.Where( x =>
            x.CeedSetCode == item.CeedSetCode && x.CeedModelNumber == item.CeedModelNumber )
          .ToList() ;
        var itemExistCeedModel = existCeedModels.Find( x =>
          x.CeedSetCode == item.CeedSetCode && x.CeedModelNumber == item.CeedModelNumber &&
          x.GeneralDisplayDeviceSymbol == item.GeneralDisplayDeviceSymbol &&
          x.ModelNumber == item.ModelNumber ) ;
        if ( itemExistCeedModel != null ) {
          var column4 = CeedViewModel.IsDiffCell( 
            string.IsNullOrEmpty( itemExistCeedModel.FloorPlanSymbol )
              ? itemExistCeedModel.Base64FloorPlanImages
              : itemExistCeedModel.FloorPlanSymbol,
            string.IsNullOrEmpty( item.FloorPlanSymbol )
              ? item.Base64FloorPlanImages
              : item.FloorPlanSymbol ) ;
          var column5 = CeedViewModel.IsDiffCell( 
            string.IsNullOrEmpty( itemExistCeedModel.InstrumentationSymbol )
              ? itemExistCeedModel.Base64InstrumentationImageString
              : itemExistCeedModel.InstrumentationSymbol,
            string.IsNullOrEmpty( item.InstrumentationSymbol )
              ? item.Base64InstrumentationImageString
              : item.InstrumentationSymbol ) ;
          var column6 = CeedViewModel.IsDiffCell( 
            itemExistCeedModel.Condition, item.Condition ) ;
          
          if ( column4 == true ) {
            var value = i + "," + 4 ;
            if ( ! _cellIndex.Contains( value ) ) {
              _cellIndex.Add( value ) ;
            }
          }
          
          if ( column5 == true ) {
            var value = i + "," + 5 ;
            if ( ! _cellIndex.Contains( value ) ) {
              _cellIndex.Add( value ) ;
            }
          }
          
          if ( column6 == true ) {
            var value = i + "," + 6 ;
            if ( ! _cellIndex.Contains( value ) ) {
              _cellIndex.Add( value ) ;
            }
          }
          
        }
        else {
          if ( ! _rowIndex.Contains( i ) ) {
            _rowIndex.Add( i ) ;
          }
        }
      }
    }
  }
}