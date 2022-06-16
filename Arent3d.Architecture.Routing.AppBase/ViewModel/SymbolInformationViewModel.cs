using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows ;
using System.Windows.Data ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SymbolInformationViewModel : NotifyPropertyChanged
  {
    private const double QuantityDefault = 100 ;
    private const string UnitDefault = "m" ;
    private const string TrajectoryDefault = "100" ;
    private const string ConstructionClassificationDefault = "導圧管類" ;

    private const string ElectricalCategoryFileName = "ElectricalCategory.xlsx" ;
    private const string ResourceFolderName = "resources" ;

    private const int DefaultDisplayItem = 100 ;

    private bool _isInChangeLoop ;

    private readonly Document? _document ;

    private ExternalCommandData _commandData ;

    public ICommand AddCeedDetailCommand => new RelayCommand( AddCeedDetail ) ;
    public ICommand DeleteCeedDetailCommand => new RelayCommand( DeleteCeedDetail ) ;
    public ICommand MoveUpCommand => new RelayCommand( MoveUp ) ;
    public ICommand MoveDownCommand => new RelayCommand( MoveDown ) ;
    public ICommand ShowElectricalCategoryCommand => new RelayCommand( ShowElectricalCategory ) ;
    public ICommand ShowCeedCodeDialogCommand => new RelayCommand( ShowCeedCodeDialog ) ;

    public SymbolInformationModel SymbolInformation { get ; }

    private List<ElectricalCategoryModel> _electricalCategoriesEco ;
    private List<ElectricalCategoryModel> _electricalCategoriesNormal ;
    private Dictionary<string, string> _dictElectricalCategoriesEcoKey ;
    private Dictionary<string, string> _dictElectricalCategoriesNormalKey ;

    #region SymbolSetting

    public readonly Array SymbolKinds = Enum.GetValues( typeof( SymbolKindEnum ) ) ;
    public readonly Array SymbolCoordinates = Enum.GetValues( typeof( SymbolCoordinateEnum ) ) ;
    public readonly Array SymbolColors = SymbolColor.DictSymbolColor.Keys.ToArray() ;

    public SymbolKindEnum SelectedSymbolKind
    {
      get => (SymbolKindEnum) Enum.Parse( typeof( SymbolKindEnum ), SymbolInformation.SymbolKind! ) ;
      set => SymbolInformation.SymbolKind = value.GetFieldName() ;
    }

    public SymbolCoordinateEnum SelectedSymbolCoordinate
    {
      get => (SymbolCoordinateEnum) Enum.Parse( typeof( SymbolCoordinateEnum ), SymbolInformation.SymbolCoordinate! ) ;
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

    private ObservableCollectionEx<CeedDetailModel> _ceedDetailList = new() ;

    private CeedDetailModel? _ceedDetailSelected ;

    public CeedDetailModel? CeedDetailSelected
    {
      get => _ceedDetailSelected ;
      set
      {
        _ceedDetailSelected = value ;
        OnPropertyChanged( nameof( CeedDetailSelected ) ) ;
      }
    }

    public List<string> BuzaiCDList { get ; set ; }

    private ObservableCollection<string> _buzaiCDListDisplay = new() ;

    public ObservableCollection<string> BuzaiCDListDisplay
    {
      get => _buzaiCDListDisplay ;
      set
      {
        _buzaiCDListDisplay = value ;
        OnPropertyChanged( nameof( BuzaiCDListDisplay ) ) ;
      }
    }

    private string _buzaiCDSearchText = string.Empty ;

    public string BuzaiCDSearchText
    {
      get => _buzaiCDSearchText ;
      set
      {
        _buzaiCDSearchText = value ;
        OnPropertyChanged( nameof( BuzaiCDSearchText ) ) ;

        if ( string.IsNullOrEmpty( value ) ) {
          BuzaiCDListDisplay = new ObservableCollection<string>( BuzaiCDList.Take( DefaultDisplayItem ).ToList() ) ;
        }
        else {
          var newSource = BuzaiCDList.Where( x => x.Length >= value.Length && x.Substring( 0, value.Length ) == value ).ToList() ;
          BuzaiCDListDisplay = new ObservableCollection<string>( newSource.Take( DefaultDisplayItem ).ToList() ) ;
        }
      }
    }

    public ObservableCollectionEx<CeedDetailModel> CeedDetailList
    {
      get => _ceedDetailList ;
      set
      {
        _ceedDetailList = value ;
        OnPropertyChanged() ;
      }
    }

    private List<ElectricalCategoryModel> LoadElectricalCategories( string sheetName, ref Dictionary<string, string> dictData )
    {
      //Load ElectricalCategory from excel file resource
      string resourcesPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, ResourceFolderName ) ;
      var filePath = Path.Combine( resourcesPath, ElectricalCategoryFileName ) ;
      return ExcelToModelConverter.GetElectricalCategories( filePath, ref dictData, sheetName ) ;
    }

    public ObservableCollection<string> ConstructionClassificationTypeList { get ; }
    public ObservableCollection<string> ClassificationTypeList { get ; }

    #endregion

    #region Command

    private void ShowCeedCodeDialog()
    {
      try {
        var ceedViewModel = new CeedViewModel( _commandData ) ;
        var dlgCeedModel = new CeedModelDialog( ceedViewModel ) ;

        if ( dlgCeedModel.ShowDialog() == false ) return ;

        var allowInputQuantity = false ;
        var ceedModelNumber = ceedViewModel?.SelectedCeedModel?.CeedModelNumber ;
        var hiroisetSelected = HiroiSetMasterNormalModels.FirstOrDefault( x => x.ParentPartModelNumber == ceedModelNumber ) ;

        if ( null != hiroisetSelected )
          AddCeedDetailBaseOnHiroiSetMaster( hiroisetSelected, ceedViewModel?.SelectedCeedModel, allowInputQuantity ) ;

        //Case seconde row with _N at the end.
        ceedModelNumber += "_N" ;
        allowInputQuantity = true ;
        hiroisetSelected = HiroiSetMasterNormalModels.FirstOrDefault( x => x.ParentPartModelNumber == ceedModelNumber ) ;

        if ( null != hiroisetSelected )
          AddCeedDetailBaseOnHiroiSetMaster( hiroisetSelected, ceedViewModel?.SelectedCeedModel, allowInputQuantity, true ) ;
      }
      catch {
      }
    }

    private void AddCeedDetailBaseOnHiroiSetMaster( HiroiSetMasterModel hiroiSetMasterModel, CeedModel? ceedModel, bool allowInputQuantity, bool isConduit = false )
    {
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode1, hiroiSetMasterModel.Name1, ceedModel, allowInputQuantity ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode2, hiroiSetMasterModel.Name2, ceedModel, allowInputQuantity, isConduit ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode3, hiroiSetMasterModel.Name3, ceedModel, allowInputQuantity ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode4, hiroiSetMasterModel.Name4, ceedModel, allowInputQuantity ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode5, hiroiSetMasterModel.Name5, ceedModel, allowInputQuantity ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode6, hiroiSetMasterModel.Name6, ceedModel, allowInputQuantity ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode7, hiroiSetMasterModel.Name7, ceedModel, allowInputQuantity ) ;
      AddCeedDetailBaseOnMaterialCode( hiroiSetMasterModel.MaterialCode8, hiroiSetMasterModel.Name8, ceedModel, allowInputQuantity ) ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
      CeedDetailSelected = CeedDetailList.Last() ;
    }

    private void AddCeedDetailBaseOnMaterialCode( string materialCode, string name, CeedModel? ceedModel, bool allowInputQuantity, bool isConduit = false )
    {
      if ( string.IsNullOrEmpty( materialCode ) ) return ;
      var hiroiMaster = HiroiMasterModels.FirstOrDefault( x => CompareBuzaiCDAndMaterialCode( x.Buzaicd, materialCode ) ) ;
      if ( null == hiroiMaster ) return ;
      var ceedSetCodeArray = ceedModel?.CeedSetCode?.Split( ':' ) ;
      var ceedCode = ceedSetCodeArray?.Length > 0 ? ceedSetCodeArray[ 0 ] : string.Empty ;
      var newCeedDetail = new CeedDetailModel( hiroiMaster.Buzaicd, name, hiroiMaster.Kikaku, string.Empty, QuantityDefault, UnitDefault, SymbolInformation.Id, TrajectoryDefault, hiroiMaster.Size1, hiroiMaster.Size2, hiroiMaster.Kikaku, CeedDetailList.Count + 1, ceedModel?.ModelNumber, ceedCode,
        ConstructionClassificationDefault, allowInputQuantity ? 0 : 1, 1, 1, string.Empty, allowInputQuantity, isConduit ) ;
      if ( ! newCeedDetail.AllowInputQuantity )
        newCeedDetail.Quantity = CeedDetailModel.Dash ;
      var doubleValue = newCeedDetail.Quantity == CeedDetailModel.Dash ? "0" : newCeedDetail.Quantity ;
      newCeedDetail.Total = ( double.Parse( doubleValue ) + newCeedDetail.QuantityCalculate ) * newCeedDetail.QuantitySet ;
      CeedDetailList.Add( newCeedDetail ) ;
    }

    private bool CompareBuzaiCDAndMaterialCode( string buzaiCd, string materialCode )
    {
      if ( materialCode.Length == 3 )
        materialCode = "000" + materialCode ;
      if ( materialCode.Length == 4 )
        materialCode = "00" + materialCode ;
      if ( materialCode.Length == 5 )
        materialCode = "0" + materialCode ;
      return string.Equals( buzaiCd, materialCode ) ;
    }

    private void ShowElectricalCategory()
    {
      if ( ! _electricalCategoriesEco.Any() ) {
        MessageBox.Show( "Can't load categories", "Message" ) ;
        return ;
      }

      ElectricalCategoryViewModel electricalCategoryViewModel =
        new(_document, _electricalCategoriesEco, _electricalCategoriesNormal, _dictElectricalCategoriesEcoKey, _dictElectricalCategoriesNormalKey, HiroiMasterModels, HiroiSetMasterNormalModels, HiroiSetMasterEcoModels, QuantityDefault, UnitDefault, TrajectoryDefault, SymbolInformation.Id) ;
      ElectricalCategoryDialog dialog = new(electricalCategoryViewModel) ;
      if ( true != dialog.ShowDialog() ) return ;
      if ( null == electricalCategoryViewModel.CeedDetailSelected ) return ;

      electricalCategoryViewModel.CeedDetailSelected.Order = CeedDetailList.Count + 1 ;
      CeedDetailList.Add( electricalCategoryViewModel.CeedDetailSelected ) ;
      CeedDetailSelected = CeedDetailList.Last() ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }

    private void MoveUp()
    {
      if ( null == CeedDetailSelected ) return ;
      if ( CeedDetailSelected.Order == 1 ) return ;
      var aboveCeedDetail = CeedDetailList.FirstOrDefault( x => x.Order == CeedDetailSelected.Order - 1 ) ;
      aboveCeedDetail!.Order += 1 ;
      CeedDetailSelected.Order -= 1 ;
      CeedDetailList = new ObservableCollectionEx<CeedDetailModel>( CeedDetailList.OrderBy( x => x.Order ) ) ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }

    private void MoveDown()
    {
      if ( null == CeedDetailSelected ) return ;
      if ( CeedDetailSelected.Order == CeedDetailList.Count ) return ;
      var belowCeedDetail = CeedDetailList.FirstOrDefault( x => x.Order == CeedDetailSelected.Order + 1 ) ;
      belowCeedDetail!.Order -= 1 ;
      CeedDetailSelected.Order += 1 ;
      CeedDetailList = new ObservableCollectionEx<CeedDetailModel>( CeedDetailList.OrderBy( x => x.Order ) ) ;
      CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
    }

    private void AddCeedDetail()
    {
      var hiroiMasterViewModel = new HiroiMasterViewModel( _document, HiroiMasterModels, _hiroiSetMasterEcoModels, _hiroiSetMasterNormalModels, true ) ;
      var hiroiMasterDialog = new HiroiMasterDialog( hiroiMasterViewModel ) ;
      if ( true == hiroiMasterDialog.ShowDialog() ) {
        var ceedDetailModel = new CeedDetailModel( hiroiMasterViewModel.HiroiMasterSelected?.Buzaicd, hiroiMasterViewModel.HiroiMasterSelected?.Hinmei, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, "", QuantityDefault, UnitDefault, this.SymbolInformation.Id, TrajectoryDefault,
          hiroiMasterViewModel.HiroiMasterSelected?.Size1, hiroiMasterViewModel.HiroiMasterSelected?.Size2, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, CeedDetailList.Count + 1, string.Empty, string.Empty, string.Empty, 1, 1, 1, string.Empty, true ) ;
        CeedDetailList.Add( ceedDetailModel ) ;
        CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
      }
    }

    public void AddCeedDetail( string buzaiCd )
    {
      var selectedHiroiMaster = HiroiMasterModels.FirstOrDefault( x => x.Buzaicd == buzaiCd ) ;
      if ( null == selectedHiroiMaster ) return ;

      if ( CeedDetailList.FirstOrDefault( x => x.ProductCode == buzaiCd ) != null ) {
        return ;
      }

      var ceedDetailModel = new CeedDetailModel( selectedHiroiMaster.Buzaicd, selectedHiroiMaster.Hinmei, selectedHiroiMaster.Kikaku, "", QuantityDefault, UnitDefault, this.SymbolInformation.Id, TrajectoryDefault, selectedHiroiMaster.Size1, selectedHiroiMaster.Size2, selectedHiroiMaster.Kikaku,
        CeedDetailList.Count + 1, string.Empty, string.Empty, string.Empty, 1, 1, 1, string.Empty, true ) ;
      CeedDetailList.Add( ceedDetailModel ) ;
      CeedDetailSelected = CeedDetailList.Last() ;
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

    public SymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel, ExternalCommandData commandData )
    {
      _commandData = commandData ;
      _document = document ;
      SymbolInformation = symbolInformationModel ?? new SymbolInformationModel() ;

      if ( ! string.IsNullOrEmpty( SymbolInformation.Id ) && SymbolInformation.Id != "-1" ) {
        CeedDetailList = new ObservableCollectionEx<CeedDetailModel>( _document!.GetCeedDetailStorable().AllCeedDetailModelData.FindAll( x => x.ParentId == SymbolInformation.Id ).OrderBy( x => x.Order ) ) ;
      }
      else {
        CeedDetailList = new ObservableCollectionEx<CeedDetailModel>() ;
      }

      CeedDetailList.ItemPropertyChanged += CeedDetailListOnItemPropertyChanged ;

      BuzaiCDList = HiroiMasterModels.Select( x => x.Buzaicd ).ToList() ;
      BuzaiCDListDisplay = new ObservableCollection<string>( BuzaiCDList.Take( DefaultDisplayItem ).ToList() ) ;
      ConstructionClassificationTypeList = new ObservableCollection<string>( Enum.GetNames( typeof( ConstructionClassificationType ) ).ToList() ) ;
      ClassificationTypeList = new ObservableCollection<string>( Enum.GetNames( typeof( ClassificationType ) ).ToList() ) ;
      _dictElectricalCategoriesEcoKey = new Dictionary<string, string>() ;
      _dictElectricalCategoriesNormalKey = new Dictionary<string, string>() ;
      _electricalCategoriesEco = LoadElectricalCategories( "Eco", ref _dictElectricalCategoriesEcoKey ) ;
      _electricalCategoriesNormal = LoadElectricalCategories( "Normal", ref _dictElectricalCategoriesNormalKey ) ;
    }

    private void CeedDetailListOnItemPropertyChanged( object sender, ItemPropertyChangedEventArgs e )
    {
      var itemChanged = CeedDetailList[ e.CollectionIndex ] ;
      if ( _isInChangeLoop ) return ;
      var restCeedDetails = CeedDetailList.Where( x => ! string.IsNullOrEmpty( x.CeedCode ) && x.CeedCode == itemChanged.CeedCode ).ToList() ;

      switch ( e.PropertyName ) {
        case "ConstructionClassification" :
        {
          foreach ( var item in restCeedDetails ) {
            _isInChangeLoop = true ;
            item.ConstructionClassification = itemChanged.ConstructionClassification ;
            SymbolInformationUtils.UpdateQuantity( CeedDetailList, itemChanged, item ) ;
          }

          _isInChangeLoop = false ;
          break ;
        }
        case "QuantitySet" :
        {
          foreach ( var item in restCeedDetails ) {
            _isInChangeLoop = true ;
            item.QuantitySet = itemChanged.QuantitySet ;
          }

          _isInChangeLoop = false ;
          break ;
        }
        case "Classification" :
        {
          SymbolInformationUtils.UpdateQuantity( CeedDetailList, itemChanged, itemChanged ) ;
          break ;
        }
        case "Quantity" :
        {
          SymbolInformationUtils.ChangeQuantityInfo( CeedDetailList, itemChanged ) ;
          break ;
        }
      }
    }
  }
}