using System.Collections.Generic ;
using System.IO ;
using System.Reflection ;
using System.Windows.Data ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{ 
  public class ElectricalCategoryViewModel : NotifyPropertyChanged
  {
    private const string ElectricalCategoryFileName = "ElectricalCategory.xlsx";
    private const string ResourceFolderName = "resources";
    private readonly Document? _document ;
    private List<HiroiMasterModel> HiroiMasterModels ;
    private List<HiroiSetMasterModel>? HiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel>? HiroiSetMasterEcoModels ;
    private double? _quantityDefault ;
    private string? _unitDefault ;
    private string? _trajectoryDefault ;
    private string? _symbolInformationId ; 
    public List<ElectricalCategoryModel> ElectricalCategoriesEco { get ; set ; }
    public List<ElectricalCategoryModel> ElectricalCategoriesNormal { get ; set ; }
    
    public Dictionary<string, string> DictElectricalCategoriesEcoKey { get ; set ; }
    public Dictionary<string, string> DictElectricalCategoriesNormalKey { get ; set ; }
    public CeedDetailModel? CeedDetailSelected { get ; set ; }
    
    public ElectricalCategoryViewModel( Document? document, List<ElectricalCategoryModel> electricalCategoriesEco, List<ElectricalCategoryModel> electricalCategoriesNormal, Dictionary<string, string> dictElectricalCategoriesEcoKey, Dictionary<string, string> dictElectricalCategoriesNormalKey, List<HiroiMasterModel> hiroiMasterModels, List<HiroiSetMasterModel>? hiroiSetMasterNormalModels, List<HiroiSetMasterModel>? hiroiSetMasterEcoModels, double? quantityDefault, string? unitDefault, string? trajectoryDefault, string? symbolInformationId)
    {
      _document = document ; 
      ElectricalCategoriesEco = electricalCategoriesEco ;
      ElectricalCategoriesNormal = electricalCategoriesNormal ;
      DictElectricalCategoriesEcoKey = dictElectricalCategoriesEcoKey ;
      DictElectricalCategoriesNormalKey = dictElectricalCategoriesNormalKey ;
      
      HiroiMasterModels = hiroiMasterModels ;
      HiroiSetMasterEcoModels = hiroiSetMasterEcoModels ;
      HiroiSetMasterNormalModels = hiroiSetMasterNormalModels ;
      _quantityDefault = quantityDefault ;
      _trajectoryDefault = trajectoryDefault ;
      _unitDefault = unitDefault ;
      _symbolInformationId = symbolInformationId ;
    }
     
    public CeedDetailModel? LoadData(bool isEcoModel, string searchValue )
    { 
      //In case of conduit (電線管). Exp: E管, PE管, C管, CD管, SUS管
      // if ( searchValue.EndsWith( "管" ) )
      //   searchValue = "電線管 " + searchValue.Substring( 0, searchValue.Length - 1 ) ;
      
      searchValue = isEcoModel ? DictElectricalCategoriesEcoKey.ContainsKey(searchValue) ? DictElectricalCategoriesEcoKey[searchValue] : searchValue : DictElectricalCategoriesNormalKey.ContainsKey( searchValue ) ? DictElectricalCategoriesNormalKey[ searchValue ] : searchValue ; 
      var hiroiMasterViewModel = new HiroiMasterViewModel( _document, HiroiMasterModels, HiroiSetMasterEcoModels, HiroiSetMasterNormalModels, isEcoModel ) { SearchText = searchValue } ;
      var hiroiMasterDialog = new HiroiMasterDialog( hiroiMasterViewModel ) ; 
      CeedDetailSelected = true == hiroiMasterDialog.ShowDialog() ? new CeedDetailModel( hiroiMasterViewModel.HiroiMasterSelected?.Buzaicd, hiroiMasterViewModel.HiroiMasterSelected?.Hinmei, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, "", _quantityDefault, _unitDefault, _symbolInformationId, _trajectoryDefault, hiroiMasterViewModel.HiroiMasterSelected?.Size1, hiroiMasterViewModel.HiroiMasterSelected?.Size2, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, 1, hiroiMasterViewModel.HiroiMasterSelected?.Type ) : null ;
      return CeedDetailSelected ;
    }
  }
}