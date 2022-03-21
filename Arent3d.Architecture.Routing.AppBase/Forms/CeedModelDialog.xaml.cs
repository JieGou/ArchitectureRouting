using System ;
using System.Collections.Generic ;
using System.IO ;
using System.IO.Compression ;
using System.Linq ;
using System.Text ;
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
    private const string NotExistConnectorFamilyInFolderModelWarning = "excelで指定したモデルはmodelフォルダーに存在していませんので、既存のモデルを使用します。" ;
    private const string HeaderCeedModelNumberColumn = "CeeD型番" ;
    private readonly DataGridColumn? _ceedModelNumberColumn ;
    private readonly Document _document ;
    private CeedViewModel? _allCeedModels ;
    private CeedViewModel? _usingCeedModel ;
    private string _ceedModelNumberSearch ;
    private string _modelNumberSearch ;
    private bool _isShowCeedModelNumber ;
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
      _isShowCeedModelNumber = false ;

      var oldCeedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeedStorable != null ) {
        LoadData( oldCeedStorable ) ;
        _isShowCeedModelNumber = oldCeedStorable.IsShowCeedModelNumber ;
      }
      
      _ceedModelNumberColumn = DtGrid.Columns.SingleOrDefault( c => c.Header.ToString() == HeaderCeedModelNumberColumn ) ;
      CbShowCeedModelNumber.IsChecked = _isShowCeedModelNumber ;

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
      SaveCeedModelNumberDisplayState() ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      SaveCeedModelNumberDisplayState() ;
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
      if ( ceedStorable != null && ceedStorable.CeedModelData.Any() ) {
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
      progress.Message = "Processing......." ;

      using ( var progressData = progress?.Reserve( 0.5 ) ) {
        UpdateCeedStorableAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        progressData?.ThrowIfCanceled() ;
      }

      using ( var progressData = progress?.Reserve( 0.9 ) ) {
        UpdateDataGridAfterReplaceFloorPlanSymbol( connectorFamilyName ) ;
        BtnReplaceSymbol.IsEnabled = false ;
        progressData?.ThrowIfCanceled() ;
      }
      
      progress?.Finish() ;
      MessageBox.Show( "Replaced floor plan symbol successfully.", "Message" ) ;
    }
    
    private void Button_ReplaceMultipleSymbols( object sender, RoutedEventArgs e )
    {
      string infoPath = string.Empty ;
      List<string> connectorFamilyPaths = new() ;
      MessageBox.Show( "Please select sample model.zip.", "Message" ) ;
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Model files (*.zip)|*.zip", Multiselect = false } ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        string zipPath = openFileDialog.FileName ;
        var extractDirectory = Path.GetDirectoryName( zipPath ) ?? string.Empty ;
        var extractFolder = Path.GetFileName( zipPath ).Replace( ".zip", "" ) + DateTime.Now.ToString( " yyyy-MM-dd HH-mm-ss" ) ;
        var extractPath = Path.Combine( extractDirectory, extractFolder ) ;
        if ( ! Path.HasExtension( extractPath ) ) {
          ZipFile.ExtractToDirectory( zipPath, extractPath ) ;
        }

        infoPath = Directory.GetFiles( extractPath ).First( f => Path.GetExtension( f ) is ".xls" or ".xlsx" ) ;
        DirectoryInfo dirInfo = new( extractPath ) ;
        var familyFolder = dirInfo.GetDirectories().First() ;
        if ( familyFolder != null ) {
          connectorFamilyPaths = Directory.GetFiles( familyFolder.FullName ).ToList() ;
        }
      }
      
      if ( string.IsNullOrEmpty( infoPath ) ) return ;
      if ( connectorFamilyPaths.Any() ) {
        var connectorFamilyFiles = CeedViewModel.LoadConnectorFamily( _document, connectorFamilyPaths ) ;
        var connectorFamilyReplacements = ExcelToModelConverter.GetConnectorFamilyReplacements( infoPath ) ;
        using var progress = ProgressBar.ShowWithNewThread( UIApplication ) ;
        progress.Message = "Processing......." ;

        using ( var progressData = progress?.Reserve( 0.5 ) ) {
          UpdateCeedStorableAfterReplaceMultipleSymbols( connectorFamilyReplacements, connectorFamilyFiles ) ;
          progressData?.ThrowIfCanceled() ;
        }

        using ( var progressData = progress?.Reserve( 0.9 ) ) {
          UpdateDataGridAfterReplaceMultipleSymbols( connectorFamilyReplacements, connectorFamilyFiles ) ;
          BtnReplaceSymbol.IsEnabled = false ;
          progressData?.ThrowIfCanceled() ;
        }
      
        progress?.Finish() ;
        MessageBox.Show( "Replaced multiple floor plan symbols successfully.", "Message" ) ;
      }
      else {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarning, "Message" ) ;
      }
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
    
    private void ShowCeedModelNumberColumn_Checked( object sender, RoutedEventArgs e )
    {
      if ( _ceedModelNumberColumn != null )
        _ceedModelNumberColumn.Visibility = Visibility.Visible ;
      LbCeedModelNumbers.Visibility = Visibility.Visible ;
      CmbCeedModelNumbers.Visibility = Visibility.Visible ;
      _isShowCeedModelNumber = true ;
    }

    private void ShowCeedModelNumberColumn_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _ceedModelNumberColumn != null )
        _ceedModelNumberColumn.Visibility = Visibility.Hidden ;
      LbCeedModelNumbers.Visibility = Visibility.Hidden ;
      CmbCeedModelNumbers.Visibility = Visibility.Hidden ;
      _isShowCeedModelNumber = false ;
    }

    private void LoadData( CeedViewModel ceedViewModel )
    {
      this.DataContext = ceedViewModel ;
      DtGrid.ItemsSource = ceedViewModel.CeedModels ;
      CmbCeedModelNumbers.ItemsSource = ceedViewModel.CeedModelNumbers ;
      CmbModelNumbers.ItemsSource = ceedViewModel.ModelNumbers ;
    }

    private void SaveCeedModelNumberDisplayState()
    {
      var ceedStorable = _document.GetCeedStorable() ;
      try {
        using Transaction t = new Transaction( _document, "Save data" ) ;
        t.Start() ;
        ceedStorable.IsShowCeedModelNumber = _isShowCeedModelNumber ;
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
    
    private void UpdateCeedStorableAfterReplaceMultipleSymbols( IReadOnlyCollection<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName )
    {
      List<string> deviceSymbolsNotHaveConnectorFamily = new () ;
      var ceedStorable = _document.GetAllStorables<CeedStorable>().First() ;
      if ( ceedStorable == null ) return ;
      if ( _allCeedModels != null ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) {
            var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
            foreach ( var deviceSymbol in deviceSymbols ) {
              var generalDisplayDeviceSymbol = deviceSymbol.Normalize( NormalizationForm.FormKC ) ;
              var ceedModels = _allCeedModels.CeedModels.Where( c => c.GeneralDisplayDeviceSymbol == generalDisplayDeviceSymbol ).ToList() ;
              if ( ! ceedModels.Any() ) continue ;
              var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
              foreach ( var ceedModel in ceedModels ) {
                ceedModel.FloorPlanType = connectorFamilyName ;
              }
            }
          }
          else {
            deviceSymbolsNotHaveConnectorFamily.AddRange( connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ) ;
          }
        }

        ceedStorable.CeedModelData = _allCeedModels.CeedModels ;
      }

      if ( _usingCeedModel != null ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
          var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
          foreach ( var deviceSymbol in deviceSymbols ) {
            var ceedModels = _usingCeedModel.CeedModels.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
            if ( ! ceedModels.Any() ) continue ;
            var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
            foreach ( var ceedModel in ceedModels ) {
              ceedModel.FloorPlanType = connectorFamilyName ;
            }
          }
        }
        ceedStorable.CeedModelData = _usingCeedModel.CeedModels ;
      }

      try {
        using Transaction t = new( _document, "Save CeeD data" ) ;
        t.Start() ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
        return ;
      }

      if ( ! NotExistConnectorFamilyInFolderModelWarning.Any() ) {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarning + "( " + string.Join( ", ", deviceSymbolsNotHaveConnectorFamily ) + " )", "Message" ) ;
      }
    }
    
    private void UpdateDataGridAfterReplaceMultipleSymbols( IReadOnlyCollection<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName )
    {
      if ( DtGrid.ItemsSource is not List<CeedModel> newCeedModels ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return ;
      }
      foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
        if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
        var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
        foreach ( var deviceSymbol in deviceSymbols ) {
          var ceedModels = newCeedModels.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
          if ( ! ceedModels.Any() ) continue ;
          var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
          foreach ( var ceedModel in ceedModels ) {
            ceedModel.FloorPlanType = connectorFamilyName ;
          }
        }
      }

      DtGrid.ItemsSource = new List<CeedModel>( newCeedModels ) ;
    }
  }
}