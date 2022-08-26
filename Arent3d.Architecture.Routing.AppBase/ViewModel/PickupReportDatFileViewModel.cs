using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Microsoft.Win32 ;
using Arent3d.Architecture.Routing.Utils;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickupReportDatFileViewModel : NotifyPropertyChanged
  {
    #region Constance

    private const string TempFileName = "フォルダを選択してください.dat" ;
    private const string LengthItem = "長さ物" ;
    private const string ConstructionMaterialItem = "工事部材" ;
    private const string EquipmentMountingItem = "機器取付" ;
    private const string WiringItem = "結線" ;
    private const string BoardItem = "盤搬入据付" ;
    private const string InteriorRepairEquipmentItem = "内装・補修・設備" ;
    private const string OtherItem = "その他" ;
    private const string DefaultConstructionItem = "未設定" ;

    #endregion constance

    #region Fields and Properties

    private readonly Document _document ;
    private List<PickUpItemModel> _pickUpItemModels = new() ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;

    private string _pathName = string.Empty ;

    public string PathName
    {
      get => _pathName ;
      set
      {
        if ( value == _pathName ) return ;
        _pathName = value ;
        OnPropertyChanged() ;
      }
    }

    public string FileName { get ; set ; } = string.Empty ;

    private string PickupNumberOnOfString => IsPickupNumberOn ? "ON" : "OFF" ;

    private bool _isPickupNumberOn ;

    public bool IsPickupNumberOn
    {
      get => _isPickupNumberOn ;
      set
      {
        if ( value == _isPickupNumberOn ) return ;
        _isPickupNumberOn = value ;
        OnPropertyChanged() ;
      }
    }

    private bool _isCanSelectOutputItems ;

    public bool IsCanSelectOutputItems
    {
      get => _isCanSelectOutputItems ;
      set
      {
        if ( value == _isCanSelectOutputItems ) return ;
        _isCanSelectOutputItems = value ;
        OnPropertyChanged() ;
      }
    }

    public IEnumerable<OutputPickUpReportItemSetting> OutputReportSettingCollection { get ; }

    #endregion Fields and Properties

    #region Command

    public ICommand? GetSaveLocationCommand { get ; private set ; }

    public ICommand? SettingCommand { get ; private set ; }

    public ICommand? ExportFileCommand { get ; private set ; }

    public ICommand? CancelCommand { get ; private set ; }

    public ICommand? ApplyOutputPickupReportSettingCommand { get ; private set ; }

    public ICommand? CancelOutputPickupReportSettingCommand { get ; private set ; }

    #endregion Command

    #region Constructor

    public PickupReportDatFileViewModel( Document document, List<PickUpItemModel>? pickUpItemModels = null )
    {
      _document = document ;
      IsPickupNumberOn = true ;
      IsCanSelectOutputItems = false ;
      InitCommand() ;
      InitPickUpModels( pickUpItemModels ) ;
      _hiroiMasterModels = GetHiroiMasterModelsData() ;
      OutputReportSettingCollection = GetOutPutReportSettingCollection().ToList() ;
    }

    #endregion Constructor

    #region Initialize

    private void InitCommand()
    {
      GetSaveLocationCommand = new RelayCommand( OnGetSaveLocationExecute ) ;
      SettingCommand = new RelayCommand( OnShowOutputItemsSelectionSettingExecute ) ;
      ExportFileCommand = new RelayCommand<Window>( OnExportFileExecute ) ;
      CancelCommand = new RelayCommand<Window>( OnCancelExecute ) ;
      ApplyOutputPickupReportSettingCommand = new RelayCommand<Window>( OnApplyOutputPickupReportSettingExecute ) ;
      CancelOutputPickupReportSettingCommand = new RelayCommand<Window>( OnCancelOutputPickupReportSettingExecute ) ;
    }

    private static IEnumerable<OutputPickUpReportItemSetting> GetOutPutReportSettingCollection()
    {
      yield return new OutputPickUpReportItemSetting( LengthItem, true ) ;
      yield return new OutputPickUpReportItemSetting( ConstructionMaterialItem, true ) ;
      yield return new OutputPickUpReportItemSetting( EquipmentMountingItem ) ;
      yield return new OutputPickUpReportItemSetting( WiringItem ) ;
      yield return new OutputPickUpReportItemSetting( BoardItem ) ;
      yield return new OutputPickUpReportItemSetting( InteriorRepairEquipmentItem, true ) ;
      yield return new OutputPickUpReportItemSetting( OtherItem ) ;
    }

    private void InitPickUpModels( List<PickUpItemModel>? pickUpItemModels = null )
    {
      _pickUpItemModels = new List<PickUpItemModel>() ;
      if ( pickUpItemModels == null ) {
        var dataStorage = _document.FindOrCreateDataStorage<PickUpModel>( false ) ;
        var storagePickUpService = new StorageService<DataStorage, PickUpModel>( dataStorage ) ;
        var version = storagePickUpService.Data.PickUpData.Any()
          ? storagePickUpService.Data.PickUpData.Max( x => x.Version )
          : string.Empty ;
        if ( ! string.IsNullOrEmpty( version ) ) {
          _pickUpItemModels.AddRange( storagePickUpService.Data.PickUpData.Where( x => x.Version == version ) ) ;
        }
      }
      else {
        _pickUpItemModels.AddRange( pickUpItemModels ) ;
      }
    }

    #endregion Initialize

    #region Command Execute and Can Execute

    private void OnGetSaveLocationExecute()
    {
      var saveFileDialog = new SaveFileDialog
      {
        FileName = TempFileName, InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments )
      } ;

      var saveFileResult = saveFileDialog.ShowDialog() ;
      if ( saveFileResult == true ) {
        PathName = Path.GetDirectoryName( saveFileDialog.FileName )! ;
      }
    }

    private void OnShowOutputItemsSelectionSettingExecute()
    {
      var outputSettingDialog = new OutputPickupReportSettingDialog( this ) ;

      var previousOutputSettingCollection = ( from outPutSettingItem in OutputReportSettingCollection select outPutSettingItem.DeepCopy() ).ToList() ;

      if ( outputSettingDialog.ShowDialog() == true ) return ;

      for ( var i = 0; i <  OutputReportSettingCollection.Count(); i++) {
        OutputReportSettingCollection.ElementAt( i ).IsSelected = previousOutputSettingCollection[ i ].IsSelected ;
      }
    }

    private void OnExportFileExecute( Window ownerWindow )
    {
      if ( ! CanExecuteExportFile( out var errorMess, out var pickUpModels ) ) {
        MessageBox.Show( errorMess, "Warning!" ) ;
        return;
      }
      
      try {
        var outputStrings = GetOutputDataToWriting( pickUpModels ) ;
        var fileName = $"{FileName}{PickupNumberOnOfString}.dat" ;
        var filePath = Path.Combine( PathName, fileName ) ;
        using ( var fsStream = new FileStream( filePath, FileMode.OpenOrCreate, FileAccess.Write ) ) {
          var streamWriter = new StreamWriter( fsStream, new UnicodeEncoding() ) ;
          foreach ( var outputString in outputStrings ) {
            streamWriter.WriteLine( outputString ) ;
          }

          streamWriter.Flush() ;
          streamWriter.Close() ;
          fsStream.Close() ;
        }

        MessageBox.Show( "Export pick-up output file successfully.", "Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export file failed because " + ex, "Error message" ) ;
      }
      finally {
        ownerWindow.DataContext = null ;
        ownerWindow.Close() ;
      }
    }

    private List<string> GetOutputDataToWriting( List<PickUpItemModel> pickUpItemModels )
    {
      var outPutStrings = new List<string>() ;
      var pickupOutPutConstructionLists = GetPickupOutputConstructionLists( pickUpItemModels ) ;

      outPutStrings.Add( $"\"1\",\"{FileName}{PickupNumberOnOfString}\"" ) ;

      var ( highestLevelIndex, lowestLevelIndex) = GetHighestAndLowestLevelHasData( pickUpItemModels ) ;
      
      var lowestLevelName = string.Empty ;
      var highestLevelName = highestLevelIndex.ToString() ;
      
      if ( highestLevelIndex != lowestLevelIndex ) lowestLevelName = lowestLevelIndex.ToString() ;

      foreach ( var outputConstructionItem in pickupOutPutConstructionLists.Where( outputConstructionItem => outputConstructionItem.OutputCollection.Any() ) ) {
        outPutStrings.Add( $"\"2\",\"{outputConstructionItem.ConstructionItemName}\",\"{highestLevelName}\",\"{lowestLevelName}\"" ) ;

        foreach ( var outPutLevel in from outputItem in outputConstructionItem.OutputCollection select outputItem.OutPutLevelItems.OrderBy( x=>x.LevelIndex ).ToList() ) {
          outPutStrings.AddRange( outPutLevel.Select( x=>x.OutputString ));
        }
      }

      return outPutStrings ;
    }

    private static (int highestLevelIndex, int lowestLevelIndex) GetHighestAndLowestLevelHasData(IEnumerable<PickUpItemModel> pickUpItemModelCollection ) 
    {
      var allLevelNameCollection = pickUpItemModelCollection.Select( x => x.Floor ).Distinct().ToList() ;
      var allLevelsAndIndexCollection = GetLevelIndexOfLevelCollection().ToList() ;

      var lowestLevelIndex = 10 ;
      var highestLevelIndex = -1 ;
      
      foreach ( var levelName in allLevelNameCollection ) {
        var levelAndIndex = allLevelsAndIndexCollection.FirstOrDefault( x => levelName.Contains(x.levelName) ) ;
        if ( string.IsNullOrEmpty( levelAndIndex.levelName ) ) continue ;
        if ( lowestLevelIndex > levelAndIndex.levelIndex ) {
          lowestLevelIndex = levelAndIndex.levelIndex ;
        }

        if ( highestLevelIndex < levelAndIndex.levelIndex ) {
          highestLevelIndex = levelAndIndex.levelIndex ;
        }
      }

      return ( highestLevelIndex, lowestLevelIndex ) ;
    }

    private static IEnumerable<PickupOutputConstructionList> GetPickupOutputConstructionLists( List<PickUpItemModel> pickUpItemModels)
    {
      var pickupOutPutConstructionLists = new List<PickupOutputConstructionList>() ;

      var levelAndIndexCollection = GetLevelIndexOfLevelCollection().EnumerateAll() ;

      if ( ! levelAndIndexCollection.Any() ) {
        throw new Exception( "don't have any level in drawing, please check against!" ) ;
      } 

      foreach ( var pickUpItemModel in pickUpItemModels ) {
        var constructionName = pickUpItemModel.ConstructionItems ;

        if ( string.IsNullOrEmpty( constructionName ) ) {
          constructionName = DefaultConstructionItem ;
        }

        var constructionOutputList = pickupOutPutConstructionLists.FirstOrDefault( c => c.ConstructionItemName == constructionName ) ;
        if ( constructionOutputList == null ) {
          constructionOutputList = new PickupOutputConstructionList( constructionName ) ;
          pickupOutPutConstructionLists.Add( constructionOutputList ) ;
        }

        if ( string.IsNullOrEmpty( pickUpItemModel.Floor ) ) continue ;

        var levelAndIndex = levelAndIndexCollection.FirstOrDefault( lv => pickUpItemModel.Floor.Contains( lv.levelName ) ) ;
        var outPutString = $"\"3\",\"{pickUpItemModel.ProductName}\",\"{pickUpItemModel.Specification}\",\"{pickUpItemModel.ProductCode}\",{pickUpItemModel.Quantity},\"\",\"\"" ;
        var outputItem = constructionOutputList.OutputCollection.FirstOrDefault( it => CompareProductCode( it.ProductCode, pickUpItemModel.ProductCode ) ) ;
        if ( outputItem == null ) {
          outputItem = new PickupOutputList( pickUpItemModel.ProductCode ) ;
          constructionOutputList.OutputCollection.Add( outputItem ) ;
        }

        outputItem.OutPutLevelItems.Add( new PickUpOutPutLevelItem( levelAndIndex.levelIndex, outPutString ) ) ;

      }

      return pickupOutPutConstructionLists ;
    }

    private static IEnumerable<(string levelName, int levelIndex)> GetLevelIndexOfLevelCollection()
    {
      return new List<(string levelName, int levelIndex)>
      {
        ( "B1F", -1 ),
        ( "1F", 1 ),
        ( "2F", 2 ),
        ( "3F", 3 ),
        ( "4F", 4 ),
        ( "5F", 5 ),
        ( "6F", 6 ),
        ( "7F", 7 ),
        ( "8F", 8 ),
        ( "9F", 9 ),
        ( "10F", 10 )
      } ;
    }

    private bool CanExecuteExportFile( out string errorMess, out List<PickUpItemModel> pickUpModels )
    {
      var errorMessList = new List<string>() ;
      errorMess = "Please" ;
      var isCanExport = ! string.IsNullOrEmpty( PathName ) && ! string.IsNullOrEmpty( FileName ) ;
      if ( string.IsNullOrEmpty( PathName ) ) errorMessList.Add( "select the folder name" ) ;
      if ( string.IsNullOrEmpty( FileName ) ) errorMessList.Add( "input the file name" ) ;
      var errMessCount = errorMessList.Count ;

      if ( errMessCount == 1 ) errorMess += $" {errorMessList.FirstOrDefault()}" ;
      else {
        for ( var i = 0 ; i < errMessCount ; i++ ) {
          errorMess += $" {errorMessList[ i ]}" ;
          if ( i < errMessCount - 1 ) {
            errorMess += " and" ;
          }
        }

        errorMess += "!" ;
      }

      if ( ! isCanExport ) {
        pickUpModels = new List<PickUpItemModel>() ;
        return false ;
      }

      pickUpModels = GetExportPickUpItemModels() ;

      if ( pickUpModels.Any() ) return true ;
      errorMess = "Don't have pick up data." ;
      return false ;
    }

    private List<HiroiMasterModel> GetHiroiMasterModelsData()
    {
      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      if ( csvStorable != null ) {
        return csvStorable.HiroiMasterModelData ;
      }

      return new List<HiroiMasterModel>() ;
    }

    private List<PickUpItemModel> GetExportPickUpItemModels()
    {
      var pickUpModels = new List<PickUpItemModel>() ;

      var outputSettingItems = OutputReportSettingCollection.Where( s => s.IsSelected ).Select( s => s.ItemName ) ;

      if ( IsCanSelectOutputItems ) {
        var newPickUpModels = _pickUpItemModels.Where( p => _hiroiMasterModels.Any( h =>
          int.Parse( h.Buzaicd ) == int.Parse( p.ProductCode.Split( '-' ).First() ) &&
          outputSettingItems.Contains( h.Syurui ) ) ) ;
        pickUpModels.AddRange( newPickUpModels ) ;
      }
      else {
        pickUpModels.AddRange( _pickUpItemModels ) ;
      }

      return GroupPickupItemModels( pickUpModels ) ;
    }

    private static List<PickUpItemModel> GroupPickupItemModels(List<PickUpItemModel> pickUpItemModels)
    {
      var outputPickUpItems = new List<PickUpItemModel>() ;

      foreach ( var newPickUpItemModel in pickUpItemModels.Select( pickUpItemModel => pickUpItemModel.DeepCopy() ).Where( newPickUpItemModel => newPickUpItemModel != null ) ) {
        if ( newPickUpItemModel == null ) continue ;
        newPickUpItemModel.ProductCode = newPickUpItemModel.ProductCode.Split( '-' ).First() ;

        var existingPickupItemModel = outputPickUpItems.FirstOrDefault( x => IsTheSameGroupPickUpItem( x, newPickUpItemModel ) ) ;

        if ( existingPickupItemModel != null ) {
          if ( double.TryParse( existingPickupItemModel.Quantity, out var existingQuantity ) &&
               double.TryParse( newPickUpItemModel.Quantity, out var newQuantity ) ) {
            existingPickupItemModel.Quantity = ( Math.Round( existingQuantity, 1 ) + Math.Round( newQuantity, 1 ) ).ToString( CultureInfo.CurrentCulture ) ;
          }
        }
        else {
          outputPickUpItems.Add(newPickUpItemModel);
        }
      }
      return outputPickUpItems ;
    }

    private static bool IsTheSameGroupPickUpItem( PickUpItemModel pickUpItemModel, PickUpItemModel otherPickUpItemModel )
    {
      return pickUpItemModel.Floor == otherPickUpItemModel.Floor &&
             CompareProductCode( pickUpItemModel.ProductCode, otherPickUpItemModel.ProductCode ) &&
             pickUpItemModel.ConstructionItems == otherPickUpItemModel.ConstructionItems ;
    }

    private static bool CompareProductCode( string productCodeA, string productCodeB )
    {
      if ( int.TryParse( productCodeA, out var productCodeANumber ) &&
           int.TryParse( productCodeB, out var productCodeBNumber ) ) {
        return productCodeANumber == productCodeBNumber ;
      }
      
      return productCodeA == productCodeB ;
    }

    private static void OnCancelExecute( Window ownerWindow )
    {
      ownerWindow.DialogResult = false ;
      ownerWindow.Close() ;
    }

    private static void OnApplyOutputPickupReportSettingExecute( Window ownerWindow )
    {
      ownerWindow.DialogResult = true ;
      ownerWindow.Close() ;
    }

    private static void OnCancelOutputPickupReportSettingExecute( Window ownerWindow )
    {
      ownerWindow.DialogResult = false ;
      ownerWindow.Close() ;
    }

    #endregion Command Execute and Can Execute
  }

  [Serializable]
  public class OutputPickUpReportItemSetting
  {
    public string ItemName { get ; }

    public bool IsSelected { get ; set ; }

    public OutputPickUpReportItemSetting( string itemName, bool isSelected = false )
    {
      ItemName = itemName ;
      IsSelected = isSelected ;
    }
    
  }

  public class PickupOutputConstructionList
  {
    public string ConstructionItemName { get ; }
    public List<PickupOutputList> OutputCollection { get ; } = new() ;

    public PickupOutputConstructionList( string constructionItemName )
    {
      ConstructionItemName = constructionItemName ;
    }
  }

  public class PickupOutputList
  {
    public string ProductCode { get ; }
    
    public List<PickUpOutPutLevelItem> OutPutLevelItems { get ; } = new() ;

    public PickupOutputList( string productCode )
    {
      ProductCode = productCode ;
    }
  }

  public class PickUpOutPutLevelItem
  {
    public int LevelIndex { get ; }
    public string OutputString { get ; }

    public PickUpOutPutLevelItem( int levelIndex, string outputString )
    {
      LevelIndex = levelIndex ;
      OutputString = outputString ;
    }
  }
}