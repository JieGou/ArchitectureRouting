using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows ;
using System.Windows.Data ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SymbolInformationViewModel : NotifyPropertyChanged
  {
    private const double QuantityDefault = 100 ;
    private const string UnitDefault = "m" ;
    private const string TrajectoryDefault = "100" ;
    private const string ElectricalCategoryFileName = "ElectricalCategory.xlsx";
    private const string ResourceFolderName = "resources";
    private const int DefaultDisplayItem = 100 ;
    
    private readonly Document? _document ; 

    public ICommand AddCeedDetailCommand => new RelayCommand( AddCeedDetail ) ;
    public ICommand DeleteCeedDetailCommand => new RelayCommand( DeleteCeedDetail ) ;
    public ICommand MoveUpCommand => new RelayCommand( MoveUp ) ;
    public ICommand MoveDownCommand => new RelayCommand( MoveDown ) ;
    public ICommand ShowElectricalCategoryCommand => new RelayCommand( ShowElectricalCategory ) ;
 
    public SymbolInformationModel SymbolInformation { get ; }

    private List<ElectricalCategoryModel> _electricalCategoriesEco ;
    private List<ElectricalCategoryModel> _electricalCategoriesNormal ;

    #region SymbolSetting

    public readonly Array SymbolKinds = Enum.GetValues( typeof( SymbolKindEnum ) ) ;
    public readonly Array SymbolCoordinates = Enum.GetValues( typeof( SymbolCoordinateEnum ) ) ; 
    public readonly Array SymbolColors = SymbolColor.DictSymbolColor.Keys.ToArray() ;
     
    public SymbolKindEnum SelectedSymbolKind
    {
      get => (SymbolKindEnum)Enum.Parse( typeof( SymbolKindEnum ), SymbolInformation.SymbolKind! ) ;
      set => SymbolInformation.SymbolKind = value.GetFieldName() ;
    }

    public SymbolCoordinateEnum SelectedSymbolCoordinate
    {
      get => (SymbolCoordinateEnum)Enum.Parse( typeof( SymbolCoordinateEnum ), SymbolInformation.SymbolCoordinate! ) ;
      set => SymbolInformation.SymbolCoordinate = value.GetFieldName() ;
    }

    #endregion

    #region CeedDetail Setting

    private CsvStorable? _csvStorable ;
    private CsvStorable CsvStorable => _csvStorable ??= _document!.GetCsvStorable() ;

    private List<HiroiMasterModel>? _hiroiMasterModels ;
    private List<HiroiMasterModel> HiroiMasterModels => _hiroiMasterModels ??= CsvStorable.HiroiMasterModelData ;
    
    private List<HiroiSetMasterModel>? _hiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel> HiroiSetMasterNormalModels => _hiroiSetMasterNormalModels ??= CsvStorable.HiroiSetMasterNormalModelData ;

    private List<HiroiSetMasterModel>? _hiroiSetMasterEcoModels ;
    private List<HiroiSetMasterModel> HiroiSetMasterEcoModels => _hiroiSetMasterEcoModels ??= CsvStorable.HiroiSetMasterEcoModelData ;

    private ObservableCollection<CeedDetailModel> _ceedDetailList = new() ;
    public CeedDetailModel? CeedDetailSelected { get ; set ; }
    public List<string> BuzaiCDList { get ; set ; }
     
    public ObservableCollection<string> BuzaiCDListDisplay { get ; set ; }
    private string _buzaiCDSearchText = string.Empty ;

    public string BuzaiCDSearchText
    {
      get => _buzaiCDSearchText ;
      set
      {
        _buzaiCDSearchText = value ;
        var newSource = BuzaiCDList.Where( x => x.Contains( value ) ).ToList() ;
        BuzaiCDListDisplay = new ObservableCollection<string>( newSource.Take( DefaultDisplayItem ).ToList() ) ;  
        CollectionViewSource.GetDefaultView( BuzaiCDListDisplay ).Refresh();
        OnPropertyChanged( nameof(BuzaiCDListDisplay) );
        OnPropertyChanged( nameof(BuzaiCDSearchText) );
      }
    }

    public ObservableCollection<CeedDetailModel> CeedDetailList
    {
      get => _ceedDetailList ;
      set
      {
        _ceedDetailList = value ;
        OnPropertyChanged() ;
      }
    }

    private List<ElectricalCategoryModel> LoadElectricalCategories(string sheetName)
    {
      //Load ElectricalCategory from excel file resource
      string resourcesPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, ResourceFolderName )  ;
      var filePath = Path.Combine( resourcesPath, ElectricalCategoryFileName ) ;
      return ExcelToModelConverter.GetElectricalCategories( filePath, sheetName) ;  
    }
    

    public ObservableCollection<string> ConstructionClassificationTypeList { get ; }

    #endregion
 
    #region Command

    private void ShowElectricalCategory()
    {
      if ( ! _electricalCategoriesEco.Any() ) {
        MessageBox.Show( "Can't load categories", "Message" ) ;
        return ;
      }
      ElectricalCategoryViewModel electricalCategoryViewModel = new(_document, _electricalCategoriesEco, _electricalCategoriesNormal, HiroiMasterModels, HiroiSetMasterNormalModels, HiroiSetMasterEcoModels, QuantityDefault, UnitDefault, TrajectoryDefault, SymbolInformation.Id) ;
      ElectricalCategoryDialog dialog = new ( electricalCategoryViewModel ) ;
      if ( true != dialog.ShowDialog() ) return ;
      if ( null == electricalCategoryViewModel.CeedDetailSelected ) return ;

      electricalCategoryViewModel.CeedDetailSelected.Order = CeedDetailList.Count + 1 ;
      CeedDetailList.Add( electricalCategoryViewModel.CeedDetailSelected ) ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }

    private void MoveUp()
    {
      if ( null == CeedDetailSelected ) return;
      if(CeedDetailSelected.Order == 1) return;
      var aboveCeedDetail = CeedDetailList.FirstOrDefault( x => x.Order == CeedDetailSelected.Order - 1 ) ;
      aboveCeedDetail!.Order += 1 ;
      CeedDetailSelected.Order -= 1 ;
      CeedDetailList = new ObservableCollection<CeedDetailModel>(CeedDetailList.OrderBy( x => x.Order ) ) ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }
    
    private void MoveDown()
    {
      if ( null == CeedDetailSelected ) return;
      if(CeedDetailSelected.Order == CeedDetailList.Count) return;
      var belowCeedDetail = CeedDetailList.FirstOrDefault( x => x.Order == CeedDetailSelected.Order + 1 ) ;
      belowCeedDetail!.Order -= 1 ;
      CeedDetailSelected.Order += 1 ;
      CeedDetailList = new ObservableCollection<CeedDetailModel>(CeedDetailList.OrderBy( x => x.Order ) ) ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }

    private void AddCeedDetail()
    {
      var hiroiMasterViewModel = new HiroiMasterViewModel( _document, HiroiMasterModels, _hiroiSetMasterEcoModels, _hiroiSetMasterNormalModels, true ) ;
      var hiroiMasterDialog = new HiroiMasterDialog( hiroiMasterViewModel ) ;
      if ( true == hiroiMasterDialog.ShowDialog() ) {
        var ceedDetailModel = new CeedDetailModel( hiroiMasterViewModel.HiroiMasterSelected?.Buzaicd, hiroiMasterViewModel.HiroiMasterSelected?.Hinmei, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, "", QuantityDefault, UnitDefault, this.SymbolInformation.Id, TrajectoryDefault, hiroiMasterViewModel.HiroiMasterSelected?.Size1, hiroiMasterViewModel.HiroiMasterSelected?.Size2, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, CeedDetailList.Count + 1 ) ;
        CeedDetailList.Add( ceedDetailModel ) ; 
        CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
      }
    }

    public void AddCeedDetail(string buzaiCd)
    {
      var selectedHiroiMaster = HiroiMasterModels.FirstOrDefault( x => x.Buzaicd == buzaiCd ) ;
      if(null == selectedHiroiMaster) return;
       
      var ceedDetailModel = new CeedDetailModel( selectedHiroiMaster.Buzaicd, selectedHiroiMaster.Hinmei, selectedHiroiMaster.Kikaku, "", QuantityDefault, UnitDefault, this.SymbolInformation.Id, TrajectoryDefault, selectedHiroiMaster.Size1, selectedHiroiMaster.Size2, selectedHiroiMaster.Kikaku, CeedDetailList.Count + 1 ) ;
      CeedDetailList.Add( ceedDetailModel ) ; 
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }

    private void DeleteCeedDetail()
    {
      if ( null == CeedDetailSelected ) return ;
      
      foreach ( var ceedDetail in CeedDetailList ) {
        if ( ceedDetail.Order > CeedDetailSelected.Order )
          ceedDetail.Order -= 1 ;
      }
      CeedDetailList.Remove( CeedDetailSelected ) ;
    }

    #endregion
 
    public SymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel )
    {
      _document = document ;
      SymbolInformation = symbolInformationModel ?? new SymbolInformationModel() ;

      if ( ! string.IsNullOrEmpty( SymbolInformation.Id ) && SymbolInformation.Id != "-1" ) {
        CeedDetailList = new ObservableCollection<CeedDetailModel>( _document!.GetCeedDetailStorable().AllCeedDetailModelData.FindAll( x => x.ParentId == SymbolInformation.Id ).OrderBy( x=>x.Order ) ) ;
      }
      else {
        CeedDetailList = new ObservableCollection<CeedDetailModel>() ;
      }

      BuzaiCDList = HiroiMasterModels.Select( x => x.Buzaicd ).ToList()  ; 
      BuzaiCDListDisplay = new ObservableCollection<string>(BuzaiCDList.Take( DefaultDisplayItem ).ToList()) ;
      ConstructionClassificationTypeList = new ObservableCollection<string>( Enum.GetNames( typeof( CreateDetailTableCommandBase.ConstructionClassificationType ) ).ToList() ) ;
      _electricalCategoriesEco = LoadElectricalCategories("Eco") ;
      _electricalCategoriesNormal = LoadElectricalCategories("Normal") ;
    }
  }
}