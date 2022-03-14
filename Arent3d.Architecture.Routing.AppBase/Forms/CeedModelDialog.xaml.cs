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
using Autodesk.Revit.UI ;
using MessageBox = System.Windows.MessageBox ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;
using Style = System.Windows.Style ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeedModelDialog
  {
    private readonly Document _document ;
    private CeedViewModel? _allCeedModels ;
    private CeedViewModel? _usingCeedModel ;
    private string _ceedModelNumberSearch ;
    private string _modelNumberSearch ;
    private CeedModel? _selectedCeedModel ;
    public string SelectedDeviceSymbol { get ; set ; }
    public string SelectedCondition { get ; set ; }
    public string SelectedCeedCode { get ; set ; }
    public string SelectedModelNumber { get ; set ; }
    public string SelectedFloorPlanType { get ; set ; }

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

      var oldCeedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeedStorable != null ) {
        LoadData( oldCeedStorable ) ;
      }

      BtnReplaceSymbol.IsEnabled = false ;

      Style rowStyle = new Style( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( DataGridRow.MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      rowStyle.Setters.Add( new EventSetter( DataGridRow.MouseLeftButtonUpEvent, new MouseButtonEventHandler( Row_MouseLeftButtonUp ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
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

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      var ceedViewModels = CbShowOnlyUsingCode.IsChecked == true ? _usingCeedModel : _allCeedModels ;
      if ( ceedViewModels == null ) return ;
      if ( string.IsNullOrEmpty( _ceedModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) ) return ;
      var ceedModels = new List<CeedModel>() ;
      if ( ! string.IsNullOrEmpty( _ceedModelNumberSearch ) && ! string.IsNullOrEmpty( _modelNumberSearch ) )
        ceedModels = ceedViewModels.CeedModels.Where( c => c.CeedModelNumber.Contains( _ceedModelNumberSearch ) && c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
      else if ( ! string.IsNullOrEmpty( _ceedModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) )
        ceedModels = ceedViewModels.CeedModels.Where( c => c.CeedModelNumber.Contains( _ceedModelNumberSearch ) ).ToList() ;
      else if ( string.IsNullOrEmpty( _ceedModelNumberSearch ) && ! string.IsNullOrEmpty( _modelNumberSearch ) )
        ceedModels = ceedViewModels.CeedModels.Where( c => c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;

      DtGrid.ItemsSource = ceedModels ;
    }

    private void Button_SymbolRegistration( object sender, RoutedEventArgs e )
    {
      var ceedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable != null ) {
        OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx", Multiselect = false } ;
        string filePath = string.Empty ;
        if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
          filePath = openFileDialog.FileName ;
        }

        if ( string.IsNullOrEmpty( filePath ) ) return ;
        var modelNumberToUse = ExcelToModelConverter.GetModelNumberToUse( filePath ) ;
        if ( ! modelNumberToUse.Any() ) return ;
        List<CeedModel> usingCeedModel = new List<CeedModel>() ;
        foreach ( var modelNumber in modelNumberToUse ) {
          var ceedModels = ceedStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
          usingCeedModel.AddRange( ceedModels ) ;
        }

        usingCeedModel = usingCeedModel.Distinct().ToList() ;
        _usingCeedModel = new CeedViewModel( ceedStorable, usingCeedModel ) ;
        LoadData( _usingCeedModel ) ;
        CbShowOnlyUsingCode.Visibility = Visibility.Visible ;
        CbShowOnlyUsingCode.IsChecked = true ;
        if ( _usingCeedModel == null || ! _usingCeedModel.CeedModels.Any() ) return ;
        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceedStorable.CeedModelUsedData = _usingCeedModel.CeedModels ;
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
      using var progress = ProgressBar.ShowWithNewThread( UIApplication ) ;
      progress.Message = "Loading data..." ;
      CeedStorable ceedStorable = _document.GetCeedStorable() ;
      {
        List<CeedModel> ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( filePath, fileEquipmentSymbolsPath ) ;
        if ( ! ceedModelData.Any() ) return ;
        ceedStorable.CeedModelData = ceedModelData ;
        ceedStorable.CeedModelUsedData = new List<CeedModel>() ;
        LoadData( ceedStorable ) ;
        CbShowOnlyUsingCode.Visibility = Visibility.Hidden ;
        CbShowOnlyUsingCode.IsChecked = false ;

        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
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
      progress.Message = "Loading and saving data...." ;

      using ( var progressData = progress?.Reserve( 0.5 ) ) {
        UpdateCeedStorableAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        progressData?.ThrowIfCanceled() ;
      }

      using ( var progressData = progress?.Reserve( 0.9 ) ) {
        UpdateDataGridAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        BtnReplaceSymbol.IsEnabled = false ;
        progressData?.ThrowIfCanceled() ;
      }
      
      MessageBox.Show( "Replaced floor plan symbol successfully.", "Message" ) ;
    }

    private void LoadData( CeedStorable ceedStorable )
    {
      var viewModel = new CeedViewModel( ceedStorable ) ;
      this.DataContext = viewModel ;
      _allCeedModels = viewModel ;
      DtGrid.ItemsSource = viewModel.CeedModels ;
      CmbCeedModelNumbers.ItemsSource = viewModel.CeedModelNumbers ;
      CmbModelNumbers.ItemsSource = viewModel.ModelNumbers ;
    }

    private void ShowOnlyUsingCode_Checked( object sender, RoutedEventArgs e )
    {
      if ( _usingCeedModel == null ) return ;
      LoadData( _usingCeedModel ) ;
    }

    private void ShowOnlyUsingCode_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _allCeedModels == null ) return ;
      LoadData( _allCeedModels ) ;
    }

    private void LoadData( CeedViewModel ceedViewModel )
    {
      this.DataContext = ceedViewModel ;
      DtGrid.ItemsSource = ceedViewModel.CeedModels ;
      CmbCeedModelNumbers.ItemsSource = ceedViewModel.CeedModelNumbers ;
      CmbModelNumbers.ItemsSource = ceedViewModel.ModelNumbers ;
    }

    private void UpdateCeedStorableAfterReplaceFloorPlanSymbol( string connectorFamilyName)
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
        using Transaction t = new Transaction( _document, "Save CeeD data" ) ;
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
  }
}