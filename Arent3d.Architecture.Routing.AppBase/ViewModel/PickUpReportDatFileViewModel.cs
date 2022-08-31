using System ;
using System.Collections.Generic ;
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
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Microsoft.Win32 ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpReportDatFileViewModel : NotifyPropertyChanged
  {
    #region Constance
    
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

    private string PickUpNumberOnOffString => IsPickUpNumberOn ? "ON" : "OFF" ;

    private bool _isPickUpNumberOn ;

    public bool IsPickUpNumberOn
    {
      get => _isPickUpNumberOn ;
      set
      {
        if ( value == _isPickUpNumberOn ) return ;
        _isPickUpNumberOn = value ;
        OnPropertyChanged() ;
      }
    }

    private bool _isOutputSettingOn ;

    public bool IsOutputSettingOn
    {
      get => _isOutputSettingOn ;
      set
      {
        if ( value == _isOutputSettingOn ) return ;
        _isOutputSettingOn = value ;
        OnPropertyChanged() ;
      }
    }

    public IEnumerable<PickUpReportViewModel.PickUpSettingItem> OutputReportSettingCollection { get ; private set ; }

    #endregion Fields and Properties

    #region Command

    public ICommand? GetSaveLocationCommand { get ; private set ; }

    public ICommand? SettingCommand { get ; private set ; }

    public ICommand? ExportFileCommand { get ; private set ; }

    public ICommand? CancelCommand { get ; private set ; }

    public ICommand? ApplyOutputSettingCommand { get ; private set ; }

    #endregion Command

    #region Constructor

    public PickUpReportDatFileViewModel( Document document, List<PickUpItemModel>? pickUpItemModels = null )
    {
      _document = document ;
      IsPickUpNumberOn = true ;
      IsOutputSettingOn = false ;
      InitCommand() ;
      InitPickUpModels( pickUpItemModels ) ;
      _hiroiMasterModels = GetHiroiMasterModelsData() ;
      OutputReportSettingCollection = PickUpReportViewModel.GetOutputReportSettings() ;
    }

    #endregion Constructor

    #region Initialize

    private void InitCommand()
    {
      GetSaveLocationCommand = new RelayCommand( OnGetSaveLocationExecute ) ;
      SettingCommand = new RelayCommand( OnShowOutputItemsSelectionSettingExecute ) ;
      ExportFileCommand = new RelayCommand<Window>( OnExportFileExecute ) ;
      CancelCommand = new RelayCommand<Window>( OnCancelExecute ) ;
      ApplyOutputSettingCommand = new RelayCommand<Window>( OnApplyOutputSettingExecute ) ;
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
      const string tempFileName = "フォルダを選択してください。" ;
      var saveFileDialog = new SaveFileDialog
      {
        FileName = "App.SelectFolder".GetAppStringByKeyOrDefault( tempFileName ), InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments )
      } ;
      
      if ( saveFileDialog.ShowDialog() is true ) {
        PathName = Path.GetDirectoryName( saveFileDialog.FileName )! ;
      }
    }

    private void OnShowOutputItemsSelectionSettingExecute()
    {
      var outputSettingDialog = new SettingOutputPickUpReport( this ) ;

      var previousOutputSettingCollection = ( from outPutSettingItem in OutputReportSettingCollection select new PickUpReportViewModel.PickUpSettingItem(outPutSettingItem.Name,outPutSettingItem.IsSelected) ).ToList() ;

      if ( outputSettingDialog.ShowDialog() == true ) return ;

      OutputReportSettingCollection = previousOutputSettingCollection ;
    }

    private void OnExportFileExecute( Window ownerWindow )
    {
      if ( ! CanExecuteExportFile( out var errorMess, out var pickUpModels ) ) {
        MessageBox.Show( errorMess, "Warning!" ) ;
        return;
      }
      
      try {
        var outputStrings = GetOutputDataToWriting( pickUpModels ) ;
        var fileName = $"{FileName}{PickUpNumberOnOffString}.dat" ;
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
        ownerWindow.Close() ;
      }
    }

    private List<string> GetOutputDataToWriting( IEnumerable<PickUpItemModel> pickUpItemModels )
    {
      var outPutStrings = new List<string>() ;
      var pickUpOutPutConstructionLists = GetPickUpOutputConstructionLists( pickUpItemModels, _document ) ;

      outPutStrings.Add( $"\"1\",\"{FileName}{PickUpNumberOnOffString}\"" ) ;

      var ( highestLevelIndex, lowestLevelIndex) = GetHighestAndLowestLevelHasData( pickUpItemModels,_document ) ;
      
      var lowestLevelName = string.Empty ;
      var highestLevelName = highestLevelIndex.ToString() ;
      
      if ( highestLevelIndex != lowestLevelIndex ) lowestLevelName = lowestLevelIndex.ToString() ;

      foreach ( var outputConstructionItem in pickUpOutPutConstructionLists.Where( outputConstructionItem => outputConstructionItem.OutputCollection.Any() ) ) {
        outPutStrings.Add( $"\"2\",\"{outputConstructionItem.ConstructionItemName}\",\"{highestLevelName}\",\"{lowestLevelName}\"" ) ;

        foreach ( var outPutLevel in from outputItem in outputConstructionItem.OutputCollection select outputItem.OutPutLevelItems.OrderBy( x=>x.LevelIndex ).ToList() ) {
          outPutStrings.AddRange( outPutLevel.Select( x=>x.OutputString ));
        }
      }

      return outPutStrings ;
    }

    private static (int highestLevelIndex, int lowestLevelIndex) GetHighestAndLowestLevelHasData(IEnumerable<PickUpItemModel> pickUpItemModelCollection, Document document ) 
    {
      var allLevelNameCollection = pickUpItemModelCollection.Select( x => x.Floor ).Distinct().ToList() ;
      var allLevelsAndIndexCollection = GetLevelIndexOfLevelCollection( document ).ToList() ;

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

    private static IEnumerable<PickUpOutputConstructionList> GetPickUpOutputConstructionLists( IEnumerable<PickUpItemModel> pickUpItemModels, Document document )
    {
      var pickUpOutPutConstructionLists = new List<PickUpOutputConstructionList>() ;

      var levelAndIndexCollection = GetLevelIndexOfLevelCollection( document ).EnumerateAll() ;

      if ( ! levelAndIndexCollection.Any() ) {
        throw new Exception( "Don't have any level in drawing, please check again!" ) ;
      } 

      foreach ( var pickUpItemModel in pickUpItemModels ) {
        var constructionName = pickUpItemModel.ConstructionItems ;

        if ( string.IsNullOrEmpty( constructionName ) ) {
          constructionName = DefaultConstructionItem ;
        }

        var constructionOutputList = pickUpOutPutConstructionLists.FirstOrDefault( c => c.ConstructionItemName == constructionName ) ;
        if ( constructionOutputList == null ) {
          constructionOutputList = new PickUpOutputConstructionList( constructionName ) ;
          pickUpOutPutConstructionLists.Add( constructionOutputList ) ;
        }

        if ( string.IsNullOrEmpty( pickUpItemModel.Floor ) ) continue ;

        var levelAndIndex = levelAndIndexCollection.FirstOrDefault( lv => pickUpItemModel.Floor.Contains( lv.levelName ) ) ;
        var outPutString = $"\"3\",\"{pickUpItemModel.ProductName}\",\"{pickUpItemModel.Specification}\",\"{pickUpItemModel.ProductCode}\",{pickUpItemModel.Quantity},\"\",\"\"" ;
        var outputItem = constructionOutputList.OutputCollection.FirstOrDefault( it => CompareProductCode( it.ProductCode, pickUpItemModel.ProductCode ) ) ;
        if ( outputItem == null ) {
          outputItem = new PickUpOutputList( pickUpItemModel.ProductCode ) ;
          constructionOutputList.OutputCollection.Add( outputItem ) ;
        }

        outputItem.OutPutLevelItems.Add( new PickUpOutPutLevelItem( levelAndIndex.levelIndex, outPutString ) ) ;

      }

      return pickUpOutPutConstructionLists ;
    }

    private static IEnumerable<(string levelName, int levelIndex)> GetLevelIndexOfLevelCollection(Document document)
    {
      var allLevels = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ) ;
      var positiveLevels = allLevels.Where( lv => (int)lv.Elevation.RevitUnitsToMillimeters() > 0 ).OrderBy( lv => lv.Elevation ) ;
      var negativeLevels = allLevels.Where( lv => (int)lv.Elevation.RevitUnitsToMillimeters() <= 0 ).OrderByDescending( lv => lv.Elevation ) ;

      var positiveIndex = 1 ;
      var negativeIndex = -1 ;
      
      foreach ( var level in positiveLevels ) {
        yield return ( level.Name, positiveIndex ) ;
        positiveIndex++ ;
      }

      foreach ( var level in negativeLevels ) {
        yield return ( level.Name, negativeIndex ) ;
        negativeIndex-- ;
      }
      
    }

    private bool CanExecuteExportFile( out string errorMess, out IEnumerable<PickUpItemModel> pickUpModels )
    {
      var errorMessList = new List<string>() ;
      errorMess = "Please" ;
      var isCanExport = ! string.IsNullOrEmpty( PathName ) && ! string.IsNullOrEmpty( FileName ) ;
      if ( string.IsNullOrEmpty( PathName ) ) errorMessList.Add( "select the output folder" ) ;
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

      pickUpModels = GetExportPickUpItemModels(_document) ;

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

    private IEnumerable<PickUpItemModel> GetExportPickUpItemModels( Document document )
    {
      var pickUpModels = new List<PickUpItemModel>() ;

      var outputSettingItems = OutputReportSettingCollection.Where( s => s.IsSelected ).Select( s => s.Name ) ;

      if ( IsOutputSettingOn ) {
        var newPickUpModels = _pickUpItemModels.Where( p => _hiroiMasterModels.Any( h =>
          int.Parse( h.Buzaicd ) == int.Parse( p.ProductCode.Split( '-' ).First() ) &&
          outputSettingItems.Contains( h.Syurui ) ) ) ;
        pickUpModels.AddRange( newPickUpModels ) ;
      }
      else {
        pickUpModels.AddRange( _pickUpItemModels ) ;
      }

      return CalculateTotalQuantity( pickUpModels, document ) ;
    }

    private static IEnumerable<PickUpItemModel> CalculateTotalQuantity(List<PickUpItemModel> pickUpItemModels, Document document)
    {
      return pickUpItemModels
        .GroupBy( x => ( x.Floor, x.ConstructionItems, x.ProductCode ), new GroupPickUpItemComparer() ).Select( p =>
        {
          var first = p.First() ;
          var newModel = new PickUpItemModel( first ) ;
          newModel.ProductCode = newModel.ProductCode.Split( '-' ).FirstOrDefault() ?? newModel.ProductCode ;
          newModel.Quantity = $"{p.Sum( x => Convert.ToDouble( x.Quantity ) )}" ;
          return newModel ;
        } ).OrderBy( x => GetLevelIndexOfLevelCollection( document ).FirstOrDefault( y => y.levelName == x.Floor ) ) ;
    }
    
    public static bool CompareProductCode( string productCodeA, string productCodeB )
    {
      productCodeA = productCodeA.Split( '-' ).FirstOrDefault() ?? productCodeA ;
      productCodeB = productCodeB.Split( '-' ).FirstOrDefault() ?? productCodeB ;
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

    private static void OnApplyOutputSettingExecute( Window ownerWindow )
    {
      ownerWindow.DialogResult = true ;
      ownerWindow.Close() ;
    }

    #endregion Command Execute and Can Execute
  }

  public class PickUpOutputConstructionList
  {
    public string ConstructionItemName { get ; }
    public List<PickUpOutputList> OutputCollection { get ; } = new() ;

    public PickUpOutputConstructionList( string constructionItemName )
    {
      ConstructionItemName = constructionItemName ;
    }
  }

  public class PickUpOutputList
  {
    public string ProductCode { get ; }
    
    public List<PickUpOutPutLevelItem> OutPutLevelItems { get ; } = new() ;

    public PickUpOutputList( string productCode )
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

  public class GroupPickUpItemComparer : EqualityComparer<(string levelName,string constructionItems,string productCode)>
  {
    public override bool Equals( (string levelName, string constructionItems, string productCode) first, (string levelName, string constructionItems, string productCode) second )
    {
      return first.levelName == second.levelName && first.constructionItems == second.constructionItems &&
             PickUpReportDatFileViewModel.CompareProductCode( first.productCode, second.productCode ) ;
    }

    public override int GetHashCode( (string levelName,string constructionItems,string productCode) obj )
    {
      return obj.GetHashCode() ;
    }
  }
  
}