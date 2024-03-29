﻿using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel.Models ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using MessageBox = System.Windows.MessageBox ;
using Expression = System.Linq.Expressions.Expression;
using System.Linq.Expressions;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using DataGrid = System.Windows.Controls.DataGrid ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Utils ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using MoreLinq ;
using MoreLinq.Extensions ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpViewModel : NotifyPropertyChanged
  {
    
    #region Variants

    private const string DefaultConstructionItem = "未設定" ;
    private const string VersionDateTimeFormat = "yyyy/MM/dd HH:mm:ss" ;
    public const string MaterialDefault = "アルミ" ;
    public const string NoCover = "無し" ;
    
    private readonly Document _document ;
    private List<PickUpItemModel> _pickUpModels ;
    private readonly StorageService<Level, PickUpModel>? _storagePickUpServiceByLevel ;
    private readonly StorageService<DataStorage, PickUpModel>? _storagePickUpService ;
    private readonly List<DetailTableItemModel> _detailTableModels ;
    private readonly SymbolInformationStorable _symbolInformationStorable ;
    private readonly CeedDetailStorable _ceedDetailStorable ;
    private readonly List<CeedModel> _ceedModels ;
    private readonly List<RegistrationOfBoardDataModel> _registrationOfBoardDataModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterEcoModels ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterNormalModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterEcoModels ;
    private Dictionary<int, string> _pickUpNumbers ;
    private int _pickUpNumber ;
    private readonly string? _version ;
    private readonly EquipmentCategory? _equipmentCategory ;

    private record SortPickUpRack( HiroiMasterModel HiroiMasterModel, PickUpItemModel PickUpItemModel ) ;

    private string Version => _version ?? DateTime.Now.ToString( VersionDateTimeFormat ) ;
    public readonly List<PickUpItemModel> DataPickUpModels ;

    public RelayCommand<Window> ExportFileCommand => new(ExportFile) ;
    public RelayCommand<Window> CancelCommand => new(Cancel) ;
    public bool IsExportCsv { get ; set ; } = true ;
    public ICommand SelectAllCommand
    {
      get
      {
        return new RelayCommand<DataGrid>( dg => null != dg, dg =>
        {
          dg.SelectAll();
        } ) ;
      }
    }
    public ICommand DeleteDbCommand
    {
      get
      {
        return new RelayCommand<DataGrid>( _ => _pickUpModels.Any(), _ =>
        {
          try {
            if ( ! string.IsNullOrEmpty( _version ) ) {
              using var transaction = new Transaction( _document, "Delete Data" ) ;
                        
              transaction.Start() ;

              if ( _storagePickUpService != null ) {
                _storagePickUpService.Data.PickUpData.RemoveAll( p => p.Version == _version ) ;
                _storagePickUpService.SaveChange() ;
              } else if ( _storagePickUpServiceByLevel != null ) {
                _storagePickUpServiceByLevel.Data.PickUpData.RemoveAll( p => p.Version == _version ) ;
                _storagePickUpServiceByLevel.SaveChange() ;
              }
            
              transaction.Commit() ;
            }
            
            OriginPickUpModels = new List<PickUpItemModel>() ;
            _pickUpModels = new List<PickUpItemModel>() ;
            MessageBox.Show( "Deleted data successfully!", "Delete Data" ) ;
          }
          catch ( Exception exception ) {
            MessageBox.Show( exception.Message, "Delete Data." ) ;
          }
        } ) ;
      }
    }
    
    public ICommand SaveCommand
    {
      get
      {
        return new RelayCommand<Window>( _ => string.IsNullOrEmpty( _version ) || ! _pickUpModels.Any(), window =>
        {
          try {
            if ( _pickUpModels.Any() )
              SavePickUpModels() ;
            window.DialogResult = true ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
            window.DialogResult = false ;
          }
        } ) ;
      }
    }

    public enum ProductType
    {
      Connector,
      Conduit,
      Cable,
      CableTray,
      CableTrayFitting
    }
    
    public enum EquipmentCategory
    {
      OnlyPieces,
      OnlyLongItems
    }

    private enum ConduitType
    {
      Conduit,
      ConduitFitting
    }

    private List<PickUpItemModel>? _originPickUpModels ;
    public List<PickUpItemModel> OriginPickUpModels
    {
      get => _originPickUpModels ??= new List<PickUpItemModel>() ;
      set
      {
        _originPickUpModels = value ;
        FilterPickUpModels = MergePickUpModels(  _originPickUpModels  ) ;
        OnPropertyChanged();
      }
    }

    private List<PickUpItemModel>? _filterPickUpModels ;
    public List<PickUpItemModel> FilterPickUpModels
    {
      get => _filterPickUpModels ??= new List<PickUpItemModel>() ;
      set
      {
        _filterPickUpModels = value ;
        OnPropertyChanged();
      }
    }

    private Dictionary<string, List<Func<PickUpItemModel, bool>>>? _conditionFilter ;
    public Dictionary<string, List<Func<PickUpItemModel, bool>>> ConditionFilter
    {
      get => _conditionFilter ??= new Dictionary<string, List<Func<PickUpItemModel, bool>>>() ;
      set
      {
        _conditionFilter = value ;
        OnPropertyChanged();
      }
    }

    public ParameterExpression ParameterExpression { get ; } = Expression.Parameter(typeof(PickUpItemModel), "p");
    public Dictionary<string, List<ConstantExpression>> FilterRules { get ; } = new() ;

    #endregion

    #region Constructor

    public PickUpViewModel( Document document, Level? level, string? version = null, EquipmentCategory? equipmentCategory = null )
    {
      _document = document ;
      _ceedModels = new List<CeedModel>() ;
      _registrationOfBoardDataModels = new List<RegistrationOfBoardDataModel>() ;
      _hiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _hiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      _hiroiMasterModels = new List<HiroiMasterModel>() ;
      _hiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      _hiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      _pickUpNumbers = new Dictionary<int, string>() ;
      _pickUpNumber = 1 ;
      _equipmentCategory = equipmentCategory ;

      var ceedStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable != null ) _ceedModels = ceedStorable.CeedModelData ;

      var registrationOfBoardDataStorable = _document.GetAllStorables<RegistrationOfBoardDataStorable>().FirstOrDefault() ;
      if ( registrationOfBoardDataStorable != null ) _registrationOfBoardDataModels = registrationOfBoardDataStorable.RegistrationOfBoardData ;

      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      if ( csvStorable != null ) {
        _hiroiSetMasterNormalModels = csvStorable.HiroiSetMasterNormalModelData ;
        _hiroiSetMasterEcoModels = csvStorable.HiroiSetMasterEcoModelData ;
        _hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        _hiroiSetCdMasterNormalModels = csvStorable.HiroiSetCdMasterNormalModelData ;
        _hiroiSetCdMasterEcoModels = csvStorable.HiroiSetCdMasterEcoModelData ;
      }

      _symbolInformationStorable = _document.GetSymbolInformationStorable() ;
      _ceedDetailStorable = _document.GetCeedDetailStorable() ;
      _version = version ;

      // Get storage
      if ( level == null ) {
        var dataStorage = document.FindOrCreateDataStorage<PickUpModel>( false ) ;
        _storagePickUpService = new StorageService<DataStorage, PickUpModel>( dataStorage ) ;
        _detailTableModels = document.GetAllDatas<Level, DetailTableModel>().SelectMany( x => x.Data.DetailTableData ).ToList() ;
      }
      else {
        var storageDetailTableModel = new StorageService<Level, DetailTableModel>( level ) ;
        _detailTableModels = storageDetailTableModel.Data.DetailTableData ;
        _storagePickUpServiceByLevel = new StorageService<Level, PickUpModel>( level ) ;
      }

      // Get pick up data
      if ( string.IsNullOrEmpty( _version ) || _version == EquipmentCategoryViewModel.LatestVersion ) {
        _pickUpModels = GetPickUpData() ;
        if ( level != null )
          _pickUpModels = _pickUpModels.Where( p => p.Floor == level.Name ).ToList() ;
      }
      else if ( _storagePickUpService != null ) {
        _pickUpModels = _storagePickUpService.Data.PickUpData.Where( p => p.Version == _version ).ToList() ;
      }
      else if ( _storagePickUpServiceByLevel != null ) {
        _pickUpModels = _storagePickUpServiceByLevel.Data.PickUpData.Where( p => p.Version == _version ).ToList() ;
      }
      else {
        _pickUpModels = new List<PickUpItemModel>() ;
      }

      DataPickUpModels = _pickUpModels ;


      if ( ! _pickUpModels.Any() ) {
        MessageBox.Show( "Don't have element.", "Result Message" ) ;
      }
      else {
        var pickUpModels = new List<PickUpItemModel>() ;

        var racks = new List<PickUpItemModel>() ;
        if ( equipmentCategory is null or EquipmentCategory.OnlyLongItems ) {
          var pickUpConduitByNumbers = MergePickUpModels( PickUpModelByNumber( ProductType.Conduit ), ProductType.Conduit ) ;
          if ( pickUpConduitByNumbers.Any() )
            pickUpModels.AddRange( pickUpConduitByNumbers ) ;

          var pickUpRackByNumbers = PickUpModelByNumber( ProductType.Cable ) ;
          if ( pickUpRackByNumbers.Any() )
            pickUpModels.AddRange( pickUpRackByNumbers ) ;

          var pickUpCableTrays = MergePickUpModels( _pickUpModels.Where( x => x.EquipmentType == ProductType.CableTray.GetFieldName() ), ProductType.CableTray ) ;
          if ( pickUpCableTrays.Any() )
            racks.AddRange( pickUpCableTrays ) ;
        }

        if ( equipmentCategory is null or EquipmentCategory.OnlyPieces ) {
          var pickUpFittings = _pickUpModels.Where( x => x.EquipmentType == ProductType.CableTrayFitting.GetFieldName() ).ToList() ;
          if ( pickUpFittings.Any() )
            racks.AddRange( pickUpFittings ) ;

          var pickUpConnectors = _pickUpModels.Where( p => p.EquipmentType == ProductType.Connector.GetFieldName() ).ToList() ;
          pickUpModels.AddRange( pickUpConnectors ) ;
        }

        if ( racks.Any() ) {
          var sortPickUpRacks = racks.GroupBy( x =>
          {
            var indexFrom = x.Specification.IndexOf( "(", StringComparison.Ordinal ) ;
            if ( indexFrom == -1 )
              return string.Empty ;

            var indexTo = x.Specification.IndexOf( ")", StringComparison.Ordinal ) ;
            if ( indexTo == -1 )
              return string.Empty ;

            return indexFrom < indexTo ? x.Specification.Substring( indexFrom + 1, indexTo - indexFrom - 1 ) : string.Empty ;
          } ).ToDictionary(x => x.Key, x => x.ToList()).OrderBy( x => x.Key ).SelectMany( x => x.Value.OrderBy( y => y.ProductName ).ThenBy( z => z.Classification ) ) ;
          pickUpModels.AddRange( sortPickUpRacks ) ;
        }

        OriginPickUpModels = ( from pickUpModel in pickUpModels orderby pickUpModel.Floor select pickUpModel ).ToList() ;
      }
    }

    #endregion

    #region Business Function

    private List<PickUpItemModel> GetPickUpData()
    {
      List<PickUpItemModel> pickUpModels = new() ;

      List<double> quantities = new() ;
      List<int> pickUpNumbers = new() ;
      List<string> directionZ = new() ;
      List<string> constructionItems = new() ;
      List<string?> isEcoModes = new() ;
      List<string> routeName = new() ;
      List<(Element Connector, Element? Conduit)> pickUpElements = new() ;
        
      var allConnector = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).Where( e => e.GetConnectorFamilyType() != null ).ToList() ;
      foreach ( var connector in allConnector ) {
        connector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
        connector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        constructionItems.Add( string.IsNullOrEmpty( constructionItem ) ? DefaultConstructionItem : constructionItem! ) ;
        isEcoModes.Add( isEcoMode ) ;
        pickUpElements.Add((connector, null));
      }
      
      SetPickUpModels( pickUpModels, pickUpElements, ProductType.Connector, quantities, pickUpNumbers, directionZ, constructionItems, isEcoModes, null, null, null, routeName ) ;

      var collector = new FilteredElementCollector( _document ) ;
      var filter = new ElementMulticategoryFilter( BuiltInCategorySets.PickUpElements ) ;
      var connectors = collector.WhereElementIsNotElementType().WherePasses( filter ).ToList() ;
      GetToConnectorsOfConduit( connectors, pickUpModels ) ;
      GetToConnectorsOfCables( connectors, pickUpModels ) ;
      GetDataFromSymbolInformation( pickUpModels ) ;
      GetPickupDataForRack( pickUpModels ) ;
      
      foreach ( var pickUpModel in pickUpModels ) {
        if(pickUpModel.EquipmentType == $"{ProductType.Cable}" || pickUpModel.EquipmentType == $"{ProductType.Conduit}" || pickUpModel.EquipmentType == $"{ProductType.CableTray}")
          pickUpModel.Quantity = $"{Math.Round( double.Parse( pickUpModel.Quantity ).RevitUnitsToMillimeters() / 1000, 2 )}" ;
      }
      
      return pickUpModels ;
    }

    private void SetPickUpModels( List<PickUpItemModel> pickUpModels, List<(Element Connector, Element? Conduit)> pickUpElements, ProductType productType, List<double> quantities, List<int> pickUpNumbers, List<string> directionZ,
      List<string> constructionItemList, List<string?> isEcoModeList, List<MaterialCodeInfo>? dictMaterialCode, List<string>? constructionClassifications, List<string>? plumbingInfos, List<string> routeNames )
    {
      var index = 0 ;
      foreach ( var pickUpElement in pickUpElements ) {
        if ( pickUpElement.Connector.LevelId == ElementId.InvalidElementId ) continue ;
        var element = _document.GetElement( pickUpElement.Connector.Id ) ;
        pickUpElement.Connector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? connectorIsEcoMode ) ;
        var isEcoMode = productType == ProductType.Conduit ? isEcoModeList[ index ] : connectorIsEcoMode ;
        var item = string.Empty ;
        var floor = _document.GetAllElements<Level>().FirstOrDefault( l => l.Id == pickUpElement.Connector.LevelId )?.Name ;
        var constructionItems = productType != ProductType.Cable ? constructionItemList[ index ] : DefaultConstructionItem ;
        var equipmentType = productType.GetFieldName() ;
        var productName = string.Empty ;
        var use = string.Empty ;
        var usageName = string.Empty ;
        var construction = string.Empty ;
        var modelNumber = string.Empty ;
        var specification = string.Empty ;
        var specification2 = string.Empty ;
        var size = string.Empty ;
        var quantity = productType == ProductType.Connector ? "1" : $"{quantities[ index ]}" ;
        var tani = string.Empty ;
        var supplement = string.Empty ;
        var supplement2 = string.Empty ;
        var group = string.Empty ;
        var layer = string.Empty ;
        var classification = string.Empty ;
        var standard = string.Empty ;
        var ceedSetCode = string.Empty ;
        var deviceSymbol = string.Empty ;
        var condition = string.Empty ;
        var pickUpNumber = productType == ProductType.Connector ? string.Empty : pickUpNumbers[ index ].ToString() ;
        var direction = productType == ProductType.Conduit ? directionZ[ index ] : string.Empty ;
        var ceedCodeModel = GetCeedSetCodeOfElement( element ) ;
        var routeName = routeNames.Any() ? routeNames[ index ] ?? string.Empty : string.Empty;
        if ( _ceedModels.Any() && ceedCodeModel.Any() && ! ( productType == ProductType.Connector && ( (FamilyInstance) element ).GetConnectorFamilyType() is ConnectorFamilyType.PullBox or ConnectorFamilyType.Handhole ) ) {
          ceedSetCode = ceedCodeModel.First() ;
          
          deviceSymbol = ceedCodeModel.Count > 1 ? ceedCodeModel.ElementAt( 1 ) : string.Empty ;
          modelNumber = ceedCodeModel.Count > 2 ? ceedCodeModel.ElementAt( 2 ) : string.Empty ;
          var ceedModels = _ceedModels.Where( x => x.CeedSetCode == ceedSetCode && x.GeneralDisplayDeviceSymbol == deviceSymbol && x.ModelNumber == modelNumber ).ToList() ;
          var ceedModel = ceedModels.FirstOrDefault() ;
          if ( ceedModel != null ) {
            modelNumber = ceedModel.ModelNumber ;
            specification2 = ceedModel.CeedSetCode ;
            supplement = ceedModel.Name ;
            condition = ceedModel.Condition ;
            var ceedModelNumber = ceedModel.CeedModelNumber ;
            // TODO: hiroisetcdmaster_normal.csvとhiroisetcdmaster_eco.csvの中身が全く一緒なので、hiroiSetCdMasterModelsに対してエコ/ノーマルモードの判定が必要ない
            var hiroiSetCdMasterModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? _hiroiSetCdMasterEcoModels : _hiroiSetCdMasterNormalModels ;
            if ( hiroiSetCdMasterModels.Any() ) {
              var hiroiSetCdMasterModel = hiroiSetCdMasterModels.FirstOrDefault( h => h.SetCode == ceedSetCode ) ;
              if ( hiroiSetCdMasterModel != null ) {
                ceedModelNumber = productType == ProductType.Connector ? hiroiSetCdMasterModel.QuantityParentPartModelNumber : hiroiSetCdMasterModel.LengthParentPartModelNumber ;
                construction = productType == ProductType.Conduit ? hiroiSetCdMasterModel.ConstructionClassification : string.Empty ;
              }
            }

            var hiroiSetMasterModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? _hiroiSetMasterEcoModels : _hiroiSetMasterNormalModels ;
            if ( hiroiSetMasterModels.Any() && ! string.IsNullOrEmpty( ceedModelNumber ) ) {
              var hiroiSetMasterModel = hiroiSetMasterModels.FirstOrDefault( h => h.ParentPartModelNumber == ceedModelNumber ) ;

              var rName = pickUpElement.Conduit?.GetRouteName() ?? string.Empty ;
              if ( ! string.IsNullOrEmpty( rName ) ) {
                var rNameArray = rName.Split( '_' ) ;
                rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
              }
              var detailTableModelItemList = null != pickUpElement.Conduit ? 
                _detailTableModels.Where( x =>
                {
                  var routeNameArray = x.RouteName.Split( '_' ) ;
                  var startRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
                  return (x.FromConnectorUniqueId == pickUpElement.Connector.UniqueId || x.ToConnectorUniqueId == pickUpElement.Connector.UniqueId) && startRouteName == rName;
                } ).ToList() 
                : new List<DetailTableItemModel>() ;
              if ( productType == ProductType.Conduit && detailTableModelItemList.Count > 0 && null != hiroiSetMasterModel) {
                foreach ( var detailTableItemModel in detailTableModelItemList ) {
                  var materialCodes = GetMaterialCodes( productType, hiroiSetMasterModel, detailTableItemModel ) ;
                  
                  if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
                    PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, 
                      use, usageName, quantity, supplement, supplement2, @group, layer, classification, pickUpNumber, direction, ceedSetCode, deviceSymbol, condition, routeName) ;
                  }
                }
              } else if ( hiroiSetMasterModel != null ) {
                var materialCodes = new List<MaterialCodeInfo>() ;
                if ( productType == ProductType.Conduit && constructionClassifications != null && ! string.IsNullOrEmpty( constructionClassifications[index] ) ) {
                  construction = constructionClassifications[ index ] ;
                  if ( plumbingInfos != null && ! string.IsNullOrEmpty( plumbingInfos[ index ] ) ) {
                    materialCodes = GetMaterialCodes( plumbingInfos, index ) ;
                  }
                }
                else {
                  materialCodes = GetMaterialCodes( productType, hiroiSetMasterModel, null ) ;
                  var qtt = pickUpElement.Connector.GetPropertyInt(ElectricalRoutingElementParameter.Quantity);
                  foreach ( var materialCode in materialCodes ) {
                    materialCode.Quantity = $"{qtt}" ;
                  }
                }
                if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
                  PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
                    classification, pickUpNumber, direction, ceedSetCode, deviceSymbol, condition, routeName ) ;
                }
              }
            }
          }
        }
         
        //Set pickupModel in case productType is Conduit and connector is Power
        if ( productType == ProductType.Conduit && dictMaterialCode != null && dictMaterialCode.Any() && ( (FamilyInstance) element ).GetConnectorFamilyType() == ConnectorFamilyType.Power ) {
          modelNumber = string.Empty ;
          element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfToConnector ) ;
          specification2 = ceedCodeOfToConnector ?? string.Empty ;
          PickUpModelBaseOnMaterialCode( dictMaterialCode, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
            classification, pickUpNumber, direction, ceedSetCode, deviceSymbol, condition, routeName ) ;
        }
        
        if ( productType == ProductType.Conduit && constructionClassifications != null && ! string.IsNullOrEmpty( constructionClassifications[index] ) && ceedCodeModel.Count == 1 ) {
          construction = constructionClassifications[ index ] ;
          if ( plumbingInfos != null && ! string.IsNullOrEmpty( plumbingInfos[ index ] ) ) {
            var materialCodes = GetMaterialCodes( plumbingInfos, index ) ;
            if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
              specification2 = ceedCodeModel.First() ?? string.Empty ;
              PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
                classification, pickUpNumber, direction, ceedSetCode, deviceSymbol, condition, routeName ) ;
            }
          }
        }
        
        if ( productType == ProductType.Conduit && constructionClassifications != null && ! string.IsNullOrEmpty( constructionClassifications[index] ) && ceedCodeModel.Count == 1 ) {
          construction = constructionClassifications[ index ] ;
          if ( plumbingInfos != null && ! string.IsNullOrEmpty( plumbingInfos[ index ] ) ) {
            var materialCodes = GetMaterialCodes( plumbingInfos, index ) ;
            if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
              specification2 = ceedCodeModel.First() ?? string.Empty ;
              PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
                classification, pickUpNumber, direction, ceedSetCode, deviceSymbol, condition, routeName ) ;
            }
          }
        }

        if ( productType == ProductType.Connector && ( (FamilyInstance) element ).GetConnectorFamilyType() is ConnectorFamilyType.PullBox or ConnectorFamilyType.Handhole ) {
          var materialCodes = new List<MaterialCodeInfo>() ;
          var materialCodePullBox = element.ParametersMap.get_Item( PullBoxRouteManager.MaterialCodeParameter ).AsString() ;
          
          if ( ! string.IsNullOrEmpty( materialCodePullBox ) ) {
            var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => h.Buzaicd == materialCodePullBox ) ;
            if ( hiroiMasterModel != null ) {
              materialCodes.Add( new MaterialCodeInfo( hiroiMasterModel.Buzaicd, hiroiMasterModel.Ryakumeicd, "1" ) ) ;
            }
          }
          else {
            const string pullBoxName = "プルボックス一式" ;
            var hiroiSetMasterModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? _hiroiSetMasterEcoModels : _hiroiSetMasterNormalModels ;
            if ( hiroiSetMasterModels.Any() ) {
              var hiroiSetMasterModel = hiroiSetMasterModels.FirstOrDefault( h => h.ParentPartName == pullBoxName ) ;
              if ( hiroiSetMasterModel != null ) {
                materialCodes = GetMaterialCodes( productType, hiroiSetMasterModel, null ) ;
              }
            }
          }
          
          if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
            PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
              classification, pickUpNumber, direction, ceedSetCode, deviceSymbol, condition, routeName ) ;
          }
        }

        index++ ;
      }
    }

    private void PickUpModelBaseOnMaterialCode( List<MaterialCodeInfo>  materialCodes, string specification, string productName, string size, string tani, string standard, ProductType productType, List<PickUpItemModel> pickUpModels, 
      string? floor, string constructionItems, string construction, string modelNumber, string specification2, string item, string equipmentType, string use, string usageName, string quantity, string supplement, string supplement2, 
      string group, string layer, string classification, string pickUpNumber, string direction, string ceedSetCode, string deviceSymbol, string condition, string relatedRouteName)
    {
      var routeName = string.Empty ;
      if ( !string.IsNullOrEmpty( relatedRouteName ) ) {
        var routeNameArray = relatedRouteName.Split( '_' ) ;
        routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      }
      const string defaultConduitTani = "m" ;
      foreach ( var materialCode in materialCodes ) {
        specification = materialCode.Name ;
        var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => int.Parse( h.Buzaicd ) == int.Parse( materialCode.MaterialCode.Split( '-' ).First() ) ) ;
        if ( hiroiMasterModel != null ) {
          productName = hiroiMasterModel.Hinmei ;
          size = hiroiMasterModel.Size2 ;
          tani = hiroiMasterModel.Tani ;
          standard = hiroiMasterModel.Kikaku ;
        }
        
        var isWire = IsWire( materialCode.MaterialCode ) ;
        var wireBook = string.Empty ;
        if ( isWire ) 
        {
          var routes = RouteCache.Get( DocumentKey.Get( _document ) ) ;
          var ecoMode = FindEcoMode( relatedRouteName, routes ) ;
          var detailTable = _detailTableModels.FirstOrDefault( x=>x.RouteName.Contains(routeName)) ;
          wireBook = detailTable != null ? detailTable.WireBook : FindWireBookDefault( ceedSetCode, materialCode.MaterialCode.Split( '-' ).First(), ecoMode ) ;
        }

        if ( productType == ProductType.Connector ) {
          var pickUpModel = pickUpModels.FirstOrDefault( p =>
            p.Floor == floor && p.ConstructionItems == constructionItems && p.ProductName == productName && p.Construction == construction && p.ModelNumber == modelNumber && p.Specification == specification && p.Specification2 == specification2 && p.Size == size && p.Tani == tani ) ;
          quantity = materialCode.Quantity ;
          if ( pickUpModel != null ) {
            pickUpModel.Quantity = ( int.Parse( pickUpModel.Quantity ) + int.Parse( quantity ) ).ToString() ;
            pickUpModel.SumQuantity += "+" + quantity ;
          }
          else {
            pickUpModel = new PickUpItemModel( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, group, layer, classification, standard, pickUpNumber, direction, materialCode.MaterialCode,
              ceedSetCode, deviceSymbol, condition,quantity, routeName, relatedRouteName, null, wireBook) ;
            pickUpModels.Add( pickUpModel ) ;
          }
        }
        else {
          if ( ! string.IsNullOrEmpty( tani ) && tani != defaultConduitTani ) tani = defaultConduitTani ;
          PickUpItemModel pickUpModel = new( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, 
            supplement2, group, layer, classification, standard, pickUpNumber, direction, materialCode.MaterialCode, ceedSetCode, deviceSymbol, condition,quantity, routeName, relatedRouteName, null, wireBook) ;
          pickUpModels.Add( pickUpModel ) ;
        }
      }
    }

    private List<MaterialCodeInfo> GetMaterialCodes(ProductType productType, HiroiSetMasterModel hiroiSetMasterModel, DetailTableItemModel? detailTableItemModel )
    {
      List<MaterialCodeInfo> materialCodes = new() ;

      if ( productType is ProductType.Conduit && null != detailTableItemModel) {
        //Plumping
        var plumbingKey = $"{detailTableItemModel.PlumbingType}{detailTableItemModel.PlumbingSize}" ;
        plumbingKey = plumbingKey.Replace( DetailTableViewModel.DefaultChildPlumbingSymbol, string.Empty ) ;

        if ( ! string.IsNullOrEmpty( plumbingKey ) ) {
          var hiroiMasterModelForPlumbing = _hiroiMasterModels.FirstOrDefault( x => $"{x.Type}{x.Size1}".Replace( " ", "" ) == plumbingKey ) ;
          if ( null != hiroiMasterModelForPlumbing ) {
            materialCodes.Add(new MaterialCodeInfo (hiroiMasterModelForPlumbing.Buzaicd + $"-{materialCodes.Count + 1}", hiroiMasterModelForPlumbing.Kikaku, "1" ));
          }
        }

        //Wiring
        var wireStrip = Regex.IsMatch( detailTableItemModel.WireStrip, @"^\d" ) ? $"x{detailTableItemModel.WireStrip}" : "" ;
        var wiringKey = $"{detailTableItemModel.WireType}{detailTableItemModel.WireSize}{wireStrip}" ;
        // TODO: 600V_はハードコードしているため、このハードコード部分を解消する必要がある。600V_と3kV_の種類、サイズが重なっているため現状場合分けが必要
        var hiroiMasterModelForWiring = _hiroiMasterModels.FirstOrDefault( x => FormatRyakumeicd(x.Ryakumeicd) == wiringKey ) ;
        if ( null != hiroiMasterModelForWiring ) {
          for ( var i = 0 ; i < int.Parse(detailTableItemModel.WireBook) ; i++ ) {
            materialCodes.Add(new MaterialCodeInfo (hiroiMasterModelForWiring.Buzaicd + $"-{materialCodes.Count + 1}", hiroiMasterModelForWiring.Kikaku, "1" ));
          }
        }
      }
      else {
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode1 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode1 + "-1", hiroiSetMasterModel.Name1, hiroiSetMasterModel.Quantity1 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode2 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode2 + "-2", hiroiSetMasterModel.Name2, hiroiSetMasterModel.Quantity2 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode3 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode3 + "-3", hiroiSetMasterModel.Name3, hiroiSetMasterModel.Quantity3 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode4 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode4 + "-4", hiroiSetMasterModel.Name4, hiroiSetMasterModel.Quantity4 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode5 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode5 + "-5", hiroiSetMasterModel.Name5, hiroiSetMasterModel.Quantity5 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode6 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode6 + "-6", hiroiSetMasterModel.Name6, hiroiSetMasterModel.Quantity6 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode7 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode7 + "-7", hiroiSetMasterModel.Name7, hiroiSetMasterModel.Quantity7 ) ) ;
        if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode8 ) ) materialCodes.Add( new MaterialCodeInfo ( hiroiSetMasterModel.MaterialCode8 + "-8", hiroiSetMasterModel.Name8, hiroiSetMasterModel.Quantity8 ) ) ;
      }
      
      return materialCodes ;
    }

    public static string FormatRyakumeicd( string ryakumeicd )
    {
      return ryakumeicd.Replace( " ", "" ).Replace( "*", "" ).Replace( "600V_", "" ) ;
    }
    
    private List<MaterialCodeInfo> GetMaterialCodes( List<string> plumbingInfos, int index )
    {
      var materialCodes = new List<MaterialCodeInfo>() ;
      var plumbingInfo = plumbingInfos[ index ].Split( ':' ) ;
      var plumbingName = plumbingInfo.First() ;
      var plumbingType = plumbingInfo.ElementAt( 1 ) ;
      var plumbingSize = plumbingInfo.ElementAt( 2 ) ;
      var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => plumbingName.Contains( h.Hinmei ) && plumbingType == h.Type && plumbingSize == h.Size1 ) 
                             ?? _hiroiMasterModels.FirstOrDefault( h => plumbingType.Contains( h.Type ) && plumbingSize == h.Size1 ) ;
      if ( hiroiMasterModel != null ) {
        materialCodes.Add( new MaterialCodeInfo( hiroiMasterModel.Buzaicd, hiroiMasterModel.Kikaku, string.Empty ) ) ;
      }
      return materialCodes ;
    }

    private List<string> GetCeedSetCodeOfElement( Element element )
    {
      element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCode ) ;
      return ! string.IsNullOrEmpty( ceedSetCode ) ? ceedSetCode!.Split( ':' ).ToList() : new List<string>() ;
    }

    private static int GetQuantity( Element conduit )
    {
      var quantity = 1 ;

      var routeName = RouteUtil.GetMainRouteName( conduit.GetRouteName() ) ;
      if ( string.IsNullOrEmpty( routeName ) )
        return quantity ;

      var toConnector = ConduitUtil.GetConnectorOfRoute( conduit.Document, routeName, false ) ;
      if ( null == toConnector )
        return quantity ;
      
      quantity = toConnector.GetPropertyInt(ElectricalRoutingElementParameter.Quantity);
      return quantity ;
    }

    private void GetToConnectorsOfConduit( IReadOnlyCollection<Element> allConnectors, List<PickUpItemModel> pickUpModels )
    {
      _pickUpNumber = 1 ;
      _pickUpNumbers = new Dictionary<int, string>() ;
      List<(Element Connector, Element? Conduit)> pickUpConnectors = new() ;
      List<double> quantities = new() ;
      List<int> pickUpNumbers = new() ;
      List<string> directionZ = new() ;
      List<string> constructionItems = new() ;
      List<string?> isEcoModes = new() ;
      List<string> constructionClassifications = new() ;
      List<MaterialCodeInfo> dictMaterialCode = new() ;
      List<string> plumbingInfos = new() ;
      List<string> routes = new() ;
      List<string> routeNames = new() ;

      var conduits = _document.GetAllElements<Conduit>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      var pullBoxs = allConnectors.Where( c => c.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ||  c.Name == ElectricalRoutingFamilyType.Handhole.GetFamilyName() ).ToList() ;
      
      foreach ( var conduit in conduits ) {
        conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var quantity = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( _document, "Length" ) ).AsDouble() * GetQuantity(conduit);
        conduit.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
        if ( string.IsNullOrEmpty( constructionItem ) ) 
          constructionItem = DefaultConstructionItem ;
        AddPickUpConduit( routes, allConnectors, pullBoxs, pickUpConnectors, quantities, pickUpNumbers, directionZ, plumbingInfos, conduit, quantity, ConduitType.Conduit, constructionItems, constructionItem!, 
          dictMaterialCode, isEcoModes, isEcoMode, constructionClassifications, string.Empty,string.Empty, routeNames) ;
      }

      var conduitFittings = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduitFitting in conduitFittings ) {
        conduitFitting.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var quantity = conduitFitting.ParametersMap.get_Item( "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( _document, "電線管長さ" ) ).AsDouble() * GetQuantity(conduitFitting);
        conduitFitting.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
        if ( string.IsNullOrEmpty( constructionItem ) ) 
          constructionItem = DefaultConstructionItem ;
        AddPickUpConduit( routes, allConnectors, pullBoxs, pickUpConnectors, quantities, pickUpNumbers, directionZ, plumbingInfos, conduitFitting, quantity, ConduitType.ConduitFitting, constructionItems, constructionItem!, 
          dictMaterialCode, isEcoModes, isEcoMode, constructionClassifications, string.Empty, string.Empty, routeNames) ;
      }
      
      var changePlumbingInformationStorable = _document.GetChangePlumbingInformationStorable() ;
      if ( changePlumbingInformationStorable.ChangePlumbingInformationModelData.Any() ) {
        foreach ( var changePlumbingInformationModel in changePlumbingInformationStorable.ChangePlumbingInformationModelData ) {
          var conduit = _document.GetElement( changePlumbingInformationModel.ConduitId ) ;
          if ( conduit == null ) 
            continue ;
          conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
          var quantity = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( _document, "Length" ) ).AsDouble() ;
          var constructionItem = changePlumbingInformationModel.ConstructionItems ;
          var plumbingInfo = string.Join( ":", changePlumbingInformationModel.PlumbingName, changePlumbingInformationModel.PlumbingType, changePlumbingInformationModel.PlumbingSize ) ;
          AddPickUpConduit(routes, allConnectors, pullBoxs, pickUpConnectors, quantities, pickUpNumbers, directionZ, plumbingInfos, conduit, quantity, ConduitType.Conduit, constructionItems, constructionItem, dictMaterialCode, 
            isEcoModes, isEcoMode, constructionClassifications, changePlumbingInformationModel.ClassificationOfPlumbing, plumbingInfo, routeNames,  changePlumbingInformationModel.ConnectorId  ) ;
        }
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, ProductType.Conduit, quantities, pickUpNumbers, directionZ, constructionItems, isEcoModes, dictMaterialCode, constructionClassifications, plumbingInfos, routeNames ) ;
    }

    private double? GetLengthPullBox( ICollection<string> routes, string routeName )
    {
      var routeRelatedConduit = _document.CollectRoutes( AddInType.Electrical).FirstOrDefault( r=>r.RouteName == routeName ) ;
      var connectorsOfPullBox = routeRelatedConduit?.GetAllConnectors().Where( x => x.Owner.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ||  x.Owner.Name == ElectricalRoutingFamilyType.Handhole.GetFamilyName() ) ;
      double? length = 0 ;
      if ( connectorsOfPullBox != null ) {
        foreach ( var connectorOfPullBox in connectorsOfPullBox ) {
          if ( connectorOfPullBox == null ) continue ;
          var pullBox = _document.GetElement( connectorOfPullBox.Owner.Id ) ;
          var width = pullBox.LookupParameter( "Width" )?.AsDouble() ;
          var depth =  pullBox.LookupParameter( "Depth" )?.AsDouble() ;
          var height =  pullBox.LookupParameter( "Height" )?.AsDouble() ;
      
          var directionOfConnector = connectorOfPullBox.Description ;
          if ( directionOfConnector == null ) return null ;
          if ( RoutingElementExtensions.ConnectorPosition.Left.EnumToString() == directionOfConnector || RoutingElementExtensions.ConnectorPosition.Right.EnumToString() == directionOfConnector ) {
            routes.Add( routeName ) ;
            if ( width != null ) length += width / 2 ;
          } else if ( RoutingElementExtensions.ConnectorPosition.Front.EnumToString() == directionOfConnector || RoutingElementExtensions.ConnectorPosition.Back.EnumToString() == directionOfConnector ) {
            routes.Add( routeName ) ;
            if ( depth != null ) length += depth / 2 ;
          }
          else if ( RoutingElementExtensions.ConnectorPosition.Top.EnumToString() == directionOfConnector || RoutingElementExtensions.ConnectorPosition.Bottom.EnumToString() == directionOfConnector ) {
            routes.Add( routeName ) ;
            if ( height != null ) length += height / 2 ;
          }
        }
      }

      return length == 0 ? null : length;
    }

    private void GetToConnectorsOfCables( IReadOnlyCollection<Element> allConnectors, List<PickUpItemModel> pickUpModels )
    {
      List<(Element Connector, Element? Conduit)> pickUpConnectors = new() ;
      List<double> quantities = new() ;
      List<int> pickUpNumbers = new() ;
      List<string> directionZ = new() ;
      List<string> constructionItems = new() ;
      List<string?> isEcoModes = new() ;
      List<string> routeNames = new() ;

      var cables = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).Distinct().ToList() ;
      foreach ( var cable in cables ) {
        cable.TryGetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, out string? toElementId ) ;
        cable.TryGetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, out string? fromElementId ) ;
        if ( string.IsNullOrEmpty( toElementId ) )
          continue ;
        
        var checkPickUp = AddPickUpConnectors( allConnectors, pickUpConnectors, toElementId!, fromElementId!, pickUpNumbers ) ;
        if ( ! checkPickUp ) 
          continue ;
        
        var quantity = cable.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( _document, "トレイ長さ" ) ).AsDouble() ;
        quantities.Add( Math.Round( quantity, 2 ) ) ;
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, ProductType.Cable, quantities, pickUpNumbers, directionZ, constructionItems, isEcoModes, null, null, null, routeNames ) ;
    }

    private void AddPickUpConduit( List<string> routes, IReadOnlyCollection<Element> allConnectors, IReadOnlyCollection<Element> pullBoxs, List<(Element, Element?)> pickUpConnectors, 
      List<double> quantities, List<int> pickUpNumbers, List<string> directionZ, List<string> plumbingInfos, Element conduit, 
      double quantity, ConduitType conduitType, List<string> constructionItems, string constructionItem, List<MaterialCodeInfo> dictMaterialCode, 
      List<string?> isEcoModes, string? isEcoMode, List<string> constructionClassifications, string constructionClassification,
      string plumbingInfo, List<string> routeNames, string? connectorId = null )
    {
      var rName = conduit.GetRouteName()! ;
      if ( string.IsNullOrEmpty( rName ) ) return ;
      var routeNameArray = rName.Split( '_' ) ;
      var routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      var checkPickUp = string.IsNullOrEmpty( connectorId ) 
        ? AddPickUpConnectors( allConnectors, pickUpConnectors, routeName, pickUpNumbers, dictMaterialCode, conduit ) 
        : AddPickUpConnectors( allConnectors, pickUpConnectors, routeName, pickUpNumbers, connectorId!, conduit ) ;
      if ( ! checkPickUp ) 
        return ;
      routeNames.Add( rName );
      switch ( conduitType ) {
        case ConduitType.Conduit :
          var location = ( conduit.Location as LocationCurve )! ;
          var line = ( location.Curve as Line )! ;
          var isDirectionZ = line.Direction.Z is 1.0 or -1.0 ? line.Origin.X + ", " + line.Origin.Y : string.Empty ;
          directionZ.Add( isDirectionZ ) ;
          break ;
        case ConduitType.ConduitFitting :
          directionZ.Add( string.Empty ) ;
          break ;
      }

      constructionItems.Add( string.IsNullOrEmpty( constructionItem ) ? DefaultConstructionItem : constructionItem ) ;
      isEcoModes.Add( string.IsNullOrEmpty( isEcoMode ) ? string.Empty : isEcoMode ) ;
      constructionClassifications.Add( constructionClassification ) ;
      plumbingInfos.Add( plumbingInfo ) ;
      if ( pullBoxs.Any() && ! routes.Contains( rName ) ) {
        var lengthPullBox = GetLengthPullBox( routes, rName ) ;
        if ( lengthPullBox != null ) {
          quantity += (double) lengthPullBox ;
        }
      }

      quantities.Add( Math.Round( quantity, 2 ) ) ;
    }

    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<(Element Connector, Element? Conduit)> pickUpConnectors, string routeName, List<int> pickUpNumbers, 
      List<MaterialCodeInfo> dictMaterialCode, Element conduit )
    {
      var toConnector = GetConnectorOfRoute( allConnectors, routeName, false ) ;
      var isPickUpByFromConnector = toConnector != null && ( toConnector.Name == ElectricalRoutingFamilyType.PressureConnector.GetFamilyName() || toConnector.Name == ElectricalRoutingFamilyType.ToJboxConnector.GetFamilyName() ) ;
      if( isPickUpByFromConnector )
        toConnector = GetConnectorOfRoute( allConnectors, routeName, true ) ;
      if ( toConnector == null || (_detailTableModels.FirstOrDefault(x=>x.ToConnectorUniqueId == toConnector.UniqueId) == null &&
                                   ( toConnector.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() || toConnector.Name == ElectricalRoutingFamilyType.Handhole.GetFamilyName() ) ) ) return false ;
      
      //Case connector is Power type, check from and to connector existed in _registrationOfBoardDataModels then get material 
      if ( ( (FamilyInstance) toConnector ).GetConnectorFamilyType() == ConnectorFamilyType.Power ) {
        toConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfToConnector ) ;
        var registrationOfBoardDataModel = _registrationOfBoardDataModels.FirstOrDefault( x => x.SignalDestination == ceedCodeOfToConnector || x.AutoControlPanel == ceedCodeOfToConnector ) ;
        if ( registrationOfBoardDataModel == null )
          return false ;

        if ( registrationOfBoardDataModel.MaterialCode1.Length > 2 && ! dictMaterialCode.Exists( m => m.MaterialCode == Convert.ToInt32( registrationOfBoardDataModel.MaterialCode1 ).ToString() ) )
          dictMaterialCode.Add( new MaterialCodeInfo( Convert.ToInt32( registrationOfBoardDataModel.MaterialCode1 ).ToString(), registrationOfBoardDataModel.Kind1, registrationOfBoardDataModel.Number1 ) ) ;

        if ( registrationOfBoardDataModel.MaterialCode2.Length > 2 && ! dictMaterialCode.Exists( m => m.MaterialCode == Convert.ToInt32( registrationOfBoardDataModel.MaterialCode2 ).ToString() ) )
          dictMaterialCode.Add( new MaterialCodeInfo( Convert.ToInt32( registrationOfBoardDataModel.MaterialCode2 ).ToString(), registrationOfBoardDataModel.Kind2, registrationOfBoardDataModel.Number2 ) ) ;
      }

      pickUpConnectors.Add( (toConnector, conduit) ) ;
      if ( ! _pickUpNumbers.ContainsValue( routeName ) ) {
        _pickUpNumbers.Add( _pickUpNumber, routeName ) ;
        pickUpNumbers.Add( _pickUpNumber ) ;
        _pickUpNumber++ ;
      }
      else {
        var pickUpNumber = _pickUpNumbers.FirstOrDefault( n => n.Value == routeName ).Key ;
        pickUpNumbers.Add( pickUpNumber ) ;
      }

      return true ;
    }
    
    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<(Element Connector, Element? Conduit)> pickUpConnectors, string routeName, 
      List<int> pickUpNumbers, string connectorId, Element conduit )
    {
      var toConnector = allConnectors.SingleOrDefault( c => c.UniqueId == connectorId) ;
      if ( toConnector == null )
        return false ;
      
      pickUpConnectors.Add( (toConnector, conduit) ) ;

      _pickUpNumbers.Add( _pickUpNumber, routeName ) ;
      pickUpNumbers.Add( _pickUpNumber ) ;
      _pickUpNumber++ ;

      return true ;
    }

    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<(Element Connector, Element? Conduit)> pickUpConnectors, string toElementId, string fromElementId, List<int> pickUpNumbers )
    {
      var connector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
      if ( connector!.IsTerminatePoint() || connector!.IsPassPoint() ) {
        connector!.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? connectorId ) ;
        if ( ! string.IsNullOrEmpty( connectorId ) ) {
          connector = allConnectors.FirstOrDefault( c => c.UniqueId == connectorId ) ;
          toElementId = connectorId! ;
        }
      }

      if ( ! string.IsNullOrEmpty( fromElementId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromElementId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorId ) ;
          fromElementId = fromConnectorId! ;
        }
      }

      if ( connector == null ) 
        return false ;
      
      pickUpConnectors.Add( (connector, null) ) ;
      if ( ! _pickUpNumbers.ContainsValue( fromElementId + ", " + toElementId ) ) {
        _pickUpNumbers.Add( _pickUpNumber, fromElementId + ", " + toElementId ) ;
        pickUpNumbers.Add( _pickUpNumber ) ;
        _pickUpNumber++ ;
      }
      else {
        var pickUpNumber = _pickUpNumbers.FirstOrDefault( n => n.Value == fromElementId + ", " + toElementId ).Key ;
        pickUpNumbers.Add( pickUpNumber ) ;
      }

      return true ;

    }

    private List<PickUpItemModel> PickUpModelByNumber( ProductType productType )
    {
      List<PickUpItemModel> pickUpModels = new() ;
      
      var equipmentType = productType.GetFieldName() ;
      var pickUpModelsByNumber = _pickUpModels.Where( p => p.EquipmentType == equipmentType )
        .GroupBy( x => x.PickUpNumber )
        .Select( g => g.ToList() ) ;
      
      foreach ( var pickUpModelByNumber in pickUpModelsByNumber ) {
        var pickUpModelByProductCodes = PickUpModelByProductCode( pickUpModelByNumber ) ;
        pickUpModels.AddRange(pickUpModelByProductCodes);
      }

      return pickUpModels ;
    }

    private List<PickUpItemModel> PickUpModelByProductCode( List<PickUpItemModel> pickUpModels )
    {
      List<PickUpItemModel> pickUpModelByProductCodes = new() ;
      
      var pickUpModelsByProductCode = pickUpModels.GroupBy( x => x.ProductCode.Split( '-' ).First() )
        .Select( g => g.ToList() ) ;
        
      foreach ( var pickUpModelByProductCode in pickUpModelsByProductCode ) {
        var pickUpModelsByConstructionItemsAndConstruction = pickUpModelByProductCode.GroupBy( x => ( x.ConstructionItems, x.Construction ) )
          .Select( g => g.ToList() ) ;
          
        foreach ( var pickUpModelByConstructionItemsAndConstruction in pickUpModelsByConstructionItemsAndConstruction ) {
          var sumQuantity = pickUpModelByConstructionItemsAndConstruction.Sum( p => Math.Round(Convert.ToDouble( p.Quantity ), 1)) ;
            
          var pickUpModel = pickUpModelByConstructionItemsAndConstruction.FirstOrDefault() ;
          if ( pickUpModel == null ) 
            continue ;

          PickUpItemModel newPickUpModel = new(pickUpModel.Item, pickUpModel.Floor, pickUpModel.ConstructionItems,
            pickUpModel.EquipmentType, pickUpModel.ProductName, pickUpModel.Use, pickUpModel.UsageName,
            pickUpModel.Construction, pickUpModel.ModelNumber, pickUpModel.Specification, pickUpModel.Specification2,
            pickUpModel.Size, $"{sumQuantity}", pickUpModel.Tani, pickUpModel.Supplement, pickUpModel.Supplement2,
            pickUpModel.Group, pickUpModel.Layer, pickUpModel.Classification, pickUpModel.Standard,
            pickUpModel.PickUpNumber, pickUpModel.Direction, pickUpModel.ProductCode, pickUpModel.CeedSetCode,
            pickUpModel.DeviceSymbol, pickUpModel.Condition, pickUpModel.SumQuantity, pickUpModel.RouteName, pickUpModel.RelatedRouteName,
            pickUpModel.Version, pickUpModel.WireBook ) ;
          
          pickUpModelByProductCodes.Add( newPickUpModel ) ;
        }
      }

      return pickUpModelByProductCodes ;
    }

    private Element? GetConnectorOfRoute( IReadOnlyCollection<Element> allConnectors, string routeName, bool isFrom )
    {
      var conduitsOfRoute = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == routeName ;
      } ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( isFrom ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() || toConnector.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() || toConnector.Name == ElectricalRoutingFamilyType.Handhole.GetFamilyName() ) continue ;
        return toConnector ;
      }

      return null ;
    }


    /// <summary>
    /// Get all conduit data from SymbolInformation and add to list pickupModel
    /// </summary>
    /// <param name="pickUpModels"></param>
    private void GetDataFromSymbolInformation( List<PickUpItemModel> pickUpModels )
    {
      var symbolInformations = _symbolInformationStorable.AllSymbolInformationModelData ;
      var ceedDetails = _ceedDetailStorable.AllCeedDetailModelData ;

      foreach ( var symbolInformation in symbolInformations ) {
        var floor = symbolInformation.Floor ;
        foreach ( var ceedDetail in ceedDetails.FindAll( x => x.ParentId == symbolInformation.SymbolUniqueId ) ) {
          PickUpItemModel newPickUpModel = new( null, floor, DefaultConstructionItem, ceedDetail.Unit == "m" ? ProductType.Conduit.GetFieldName() : ProductType.Connector.GetFieldName(), ceedDetail.Specification, null, null, 
            ceedDetail.ConstructionClassification, ceedDetail.ModeNumber, ceedDetail.ProductName, ceedDetail.CeedCode, ceedDetail.Size2, ceedDetail.Unit == "m" ? $"{ceedDetail.Total.MetersToRevitUnits()}" : $"{ceedDetail.Total}", 
            ceedDetail.Unit, ceedDetail.Supplement, null, null, null, ceedDetail.Classification, ceedDetail.Standard, null, null, ceedDetail.ProductCode, null, null, null, $"{ceedDetail.QuantitySet}" ) ;
          var oldPickUpModel = pickUpModels.FirstOrDefault( x => x.Floor == newPickUpModel.Floor && x.ProductName == newPickUpModel.ProductName && x.ProductCode == newPickUpModel.ProductCode 
                                                                 && x.Specification == newPickUpModel.Specification && x.ConstructionItems == newPickUpModel.ConstructionItems && x.Construction == newPickUpModel.Construction ) ;
          if ( null != oldPickUpModel ) {
            oldPickUpModel.Quantity = ( double.Parse( oldPickUpModel.Quantity ) + double.Parse( newPickUpModel.Quantity ) ).ToString( CultureInfo.InvariantCulture ) ;
          }
          else {
            pickUpModels.Add( newPickUpModel ) ;
          }
        }
      }
    }

    private void GetPickupDataForRack( List<PickUpItemModel> pickUpModels )
    {
      var levels = _document.GetAllInstances<Level>().OrderBy( x => x.Elevation ).EnumerateAll() ;
      
      var cableTrays = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_CableTrayFitting )
        .OfType<FamilyInstance>()
        .Where(x => x.Symbol.FamilyName == ElectricalRoutingFamilyType.CableTray.GetFamilyName()).EnumerateAll() ;
      foreach ( var cableTray in cableTrays ) {
        var elevationFormLevel = cableTray.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).AsDouble() ;
        var level = levels.Where( x => x.Elevation <= elevationFormLevel ).OrderByDescending(x => x.Elevation).FirstOrDefault() ;
        
        var material = cableTray.GetPropertyString( ElectricalRoutingElementParameter.Material ) ?? string.Empty ;
        var showMaterial = material != MaterialDefault && !string.IsNullOrEmpty(material) ? $"({material})" : string.Empty ;

        var constructionItem = cableTray.GetPropertyString( ElectricalRoutingElementParameter.ConstructionItem ) ?? DefaultConstructionItem ;
        
        var pickUpCableTray = new PickUpItemModel
        {
          ProductName = "ケーブルラック",
          Floor = level?.Name ?? string.Empty,
          Specification = $"{Math.Round(cableTray.LookupParameter("トレイ幅").AsDouble().RevitUnitsToMillimeters())}mm{( !string.IsNullOrEmpty(showMaterial) ? $"{showMaterial}" : string.Empty )}",
          EquipmentType = ProductType.CableTray.GetFieldName(),
          ConstructionItems = constructionItem,
          Quantity = $"{cableTray.LookupParameter("トレイ長さ").AsDouble()}",
          Tani = "m"
        } ;
        pickUpModels.Add(pickUpCableTray);

        if ( cableTray.GetPropertyBool( ElectricalRoutingElementParameter.Separator ) ) {
          var pickUpSeparator = new PickUpItemModel
          {
            ProductName = "ケーブルラック",
            Floor = level?.Name ?? string.Empty,
            Specification = $"直線ｾﾊﾟﾚｰﾀ{( !string.IsNullOrEmpty(showMaterial) ? $"{showMaterial}" : string.Empty )}",
            EquipmentType = ProductType.CableTray.GetFieldName(),
            ConstructionItems = constructionItem,
            Quantity = $"{cableTray.LookupParameter("トレイ長さ").AsDouble()}",
            Tani = "m"
          } ;
          pickUpModels.Add(pickUpSeparator);
        }
        
        if(cableTray.GetPropertyString(ElectricalRoutingElementParameter.Cover) is not { } cover || cover == NoCover)
          continue;
        
        var pickUpCover = new PickUpItemModel
        {
          ProductName = "ケーブルラックカバー",
          Floor = level?.Name ?? string.Empty,
          Specification = $"{Math.Round(cableTray.LookupParameter("トレイ幅").AsDouble().RevitUnitsToMillimeters())}mm{( !string.IsNullOrEmpty(showMaterial) ? $"{showMaterial}" : string.Empty )}",
          EquipmentType = ProductType.CableTray.GetFieldName(),
          ConstructionItems = constructionItem,
          Quantity = $"{cableTray.LookupParameter("トレイ長さ").AsDouble()}",
          Tani = "m"
        } ;
        pickUpModels.Add(pickUpCover);
      }

      var cableTrayFittings = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_CableTrayFitting )
        .OfType<FamilyInstance>()
        .Where(x => x.Symbol.FamilyName == ElectricalRoutingFamilyType.CableTrayFitting.GetFamilyName());
      foreach ( var cableTrayFitting in cableTrayFittings ) {
        var elevationFormLevel = cableTrayFitting.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).AsDouble() ;
        var level = levels.Where( x => x.Elevation <= elevationFormLevel ).OrderByDescending(x => x.Elevation).FirstOrDefault() ;
        
        var material = cableTrayFitting.GetPropertyString( ElectricalRoutingElementParameter.Material ) ?? string.Empty ;
        var showMaterial = material != MaterialDefault && !string.IsNullOrEmpty(material) ? $"({material})" : string.Empty ;
        
        var constructionItem = cableTrayFitting.GetPropertyString( ElectricalRoutingElementParameter.ConstructionItem ) ?? DefaultConstructionItem ;
        
        var pickUpFitting = new PickUpItemModel
        {
          ProductName = "L形分岐ラック",
          Floor = level?.Name ?? string.Empty,
          Specification = $"{Math.Round(cableTrayFitting.LookupParameter("トレイ幅").AsDouble().RevitUnitsToMillimeters())}mm{( !string.IsNullOrEmpty(showMaterial) ? $"{showMaterial}" : string.Empty )}",
          EquipmentType = ProductType.CableTrayFitting.GetFieldName(),
          ConstructionItems = constructionItem,
          Quantity = "1",
          Tani = "個"
        } ;
        pickUpModels.Add(pickUpFitting);

        if ( cableTrayFitting.GetPropertyBool( ElectricalRoutingElementParameter.Separator ) ) {
          var pickUpSeparator = new PickUpItemModel
          {
            ProductName = "ケーブルラック",
            Floor = level?.Name ?? string.Empty,
            Specification = $"分岐ｾﾊﾟﾚｰﾀ{( !string.IsNullOrEmpty(showMaterial) ? $"{showMaterial}" : string.Empty )}",
            EquipmentType = ProductType.CableTrayFitting.GetFieldName(),
            ConstructionItems = constructionItem,
            Quantity = "1",
            Tani = "個"
          } ;
          pickUpModels.Add(pickUpSeparator);
        }

        if(cableTrayFitting.GetPropertyString(ElectricalRoutingElementParameter.Cover) is not { } cover || cover == NoCover)
          continue;
        
        var pickUpCover = new PickUpItemModel
        {
          ProductName = "L形分岐ラックカバー",
          Floor = level?.Name ?? string.Empty,
          Specification = $"{Math.Round(cableTrayFitting.LookupParameter("トレイ幅").AsDouble().RevitUnitsToMillimeters())}mm{( !string.IsNullOrEmpty(showMaterial) ? $"{showMaterial}" : string.Empty )}",
          EquipmentType = ProductType.CableTrayFitting.GetFieldName(),
          ConstructionItems = constructionItem,
          Quantity = "1",
          Tani = "個"
        } ;
        pickUpModels.Add(pickUpCover);
      }
    }

    #endregion

    #region Command Method

    private void ExportFile( Window window )
    {
      try {
        if ( ! _pickUpModels.Any() ) return ;

        var pickUpModels = new List<PickUpItemModel>() ;
        if ( _equipmentCategory is null or EquipmentCategory.OnlyLongItems ) {
          pickUpModels.AddRange( _pickUpModels.Where( p=> p.EquipmentType ==  ProductType.Conduit.GetFieldName() || p.EquipmentType == ProductType.Cable.GetFieldName()) ) ;
        }

        if ( _equipmentCategory is null or EquipmentCategory.OnlyPieces ) {
          var pickUpConnectors =  _pickUpModels.Where( p => p.EquipmentType == ProductType.Connector.GetFieldName() ) ;
          pickUpModels.AddRange( pickUpConnectors ); 
        }
        
        var pickUpReportViewModel = new PickUpReportViewModel( _document, pickUpModels, IsExportCsv ) ;
        var dialog = new PickUpReportDialog( pickUpReportViewModel ) ;
        dialog.ShowDialog() ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export data failed because " + ex, "Error Message" ) ;
      }
    }

    private void SavePickUpModels()
    {
      if ( ! _pickUpModels.Any() ) return ;

      var version = Version ;
      _pickUpModels.ForEach( p => p.Version = version );
      
      using var t = new Transaction( _document, "Save data" ) ;
      t.Start() ;
      
      if ( _storagePickUpService != null ) {
        _storagePickUpService.Data.PickUpData.RemoveAll( p => p.Version == version ) ;
        _storagePickUpService.Data.PickUpData.AddRange( _pickUpModels ) ;
        _storagePickUpService.SaveChange() ;
      } else if ( _storagePickUpServiceByLevel != null ) {
        _storagePickUpServiceByLevel.Data.PickUpData.RemoveAll( p => p.Version == version ) ;
        _storagePickUpServiceByLevel.Data.PickUpData.AddRange( _pickUpModels ) ;
        _storagePickUpServiceByLevel.SaveChange() ;
      }
      
      t.Commit() ;
    }

    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    public ICommand FilterCommand
    {
      get
      {
        return new RelayCommand<DataGridTextColumn>( dgc => null != dgc, dgc =>
        {
          if(dgc.Binding is not System.Windows.Data.Binding binding)
            return;

          var optionModels = OriginPickUpModels.Select( x => $"{x.GetType().GetProperty( binding.Path.Path )!.GetValue( x )}" )
            .Where(x => !string.IsNullOrEmpty(x)).Distinct().Select( x => new OptionModel { Name = x, IsChecked = true} ).OrderBy(x => x.Name).ToList() ;

          if ( FilterRules.ContainsKey( binding.Path.Path ) ) {
            var fieldRule = FilterRules[ binding.Path.Path ] ;
            foreach ( var optionModel in optionModels ) {
              if ( fieldRule.All( x => $"{x.Value}" != optionModel.Name ) )
                optionModel.IsChecked = false ;
            }
          }

          var contentHeader = FindVisualChildren<TextBlock>( dgc.HeaderTemplate.LoadContent() ).First() ;
          var viewModel = new FilterFieldViewModel( contentHeader.Text, optionModels ) ;
          var view = new FilterFieldView { DataContext = viewModel } ;
          view.ShowDialog() ;

          if ( !viewModel.IsOk ) 
            return;
          
          var constantExpressions = new List<ConstantExpression>() ;
          foreach ( var fieldValue in viewModel.FieldValues ) {
            if(!fieldValue.IsChecked)
              continue;
            
            constantExpressions.Add(Expression.Constant(fieldValue.Name, typeof(string)));
          }

          if ( constantExpressions.Any() ) {
            if ( FilterRules.ContainsKey( binding.Path.Path ) ) 
              FilterRules[ binding.Path.Path ] = constantExpressions ;
            else
              FilterRules.Add(binding.Path.Path, constantExpressions);
          }
          else {
            if ( FilterRules.ContainsKey( binding.Path.Path ) )
              FilterRules.Remove( binding.Path.Path ) ;
          }

          var dlg = CompileExpression( ParameterExpression, FilterRules ) ;
          if(null == dlg)
            return;

          FilterPickUpModels = MergePickUpModels( OriginPickUpModels.Where( dlg ).ToList() ) ;
        } ) ;
      }
    }

    #endregion

    private Func<PickUpItemModel, bool>? CompileExpression(ParameterExpression parameterExpression, Dictionary<string, List<ConstantExpression>> filterRules)
    {
      if ( filterRules.Count == 0 )
        return null ;

      var filterRule = filterRules.ElementAt( 0 ) ;
      var propertyName = Expression.Property(parameterExpression, filterRule.Key);
      var leftBinaryExpression = OrElse( propertyName, filterRule.Value ) ;

      for ( var i = 1 ; i < filterRules.Count ; i++ ) {
        var proName = Expression.Property(parameterExpression, filterRules.ElementAt(i).Key);
        var rightBinaryExpression = OrElse( proName, filterRules.ElementAt(i).Value ) ;
        leftBinaryExpression = Expression.AndAlso(leftBinaryExpression, rightBinaryExpression);
      }
      
      var expressionTree = Expression.Lambda<Func<PickUpItemModel, bool>>(leftBinaryExpression, parameterExpression );
      return expressionTree.Compile() ;
    }

    private BinaryExpression OrElse(MemberExpression memberExpression, List<ConstantExpression> constantExpressions)
    {
      var leftExpression = Expression.Equal( memberExpression, Expression.Constant( constantExpressions[ 0 ].Value, typeof( string ) ) ) ;
      if ( constantExpressions.Count == 1 )
        return leftExpression ;
 
      for ( var i = 1 ; i < constantExpressions.Count ; i++ ) {
        leftExpression = Expression.OrElse( leftExpression, Expression.Equal( memberExpression, Expression.Constant( constantExpressions[ i ].Value, typeof( string ) ) ) ) ;
      }

      return leftExpression ;
    }
    
    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
      var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (var i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);

        if (child is T childType)
        {
          yield return childType;
        }

        foreach (var other in FindVisualChildren<T>(child))
        {
          yield return other;
        }
      }
    }

    private List<PickUpItemModel> MergePickUpModels( IEnumerable<PickUpItemModel> pickUpModels )
    {
      return pickUpModels.GroupBy( p => new
      {
        p.Classification,
        p.Condition,
        p.Construction,
        p.Floor,
        p.Layer,
        p.Specification,
        p.Supplement,
        p.Tani,
        p.ConstructionItems,
        p.DeviceSymbol,
        p.ModelNumber,
        p.ProductName,
        p.CeedSetCode
      } ).Select( p =>
      {
        var newModel = p.First() ;
        newModel.Quantity = $"{p.Sum( x => Convert.ToDouble( x.Quantity ) )}" ;
        newModel.PickUpNumber = string.Empty ;
        return newModel ;
      } ).OrderBy( p => p.Floor ).ToList() ;
    }
    
    private string FindWireBookDefault( string ceedCode, string productCode, string ecoMode )
    {
      var result = string.Empty ;
      var hiroiSetCdMasterModel = ! string.IsNullOrEmpty( ecoMode ) && bool.Parse( ecoMode ) ?  _hiroiSetCdMasterEcoModels.SingleOrDefault( x => x.SetCode == ceedCode ) : _hiroiSetCdMasterNormalModels.SingleOrDefault( x => x.SetCode == ceedCode );
      if ( hiroiSetCdMasterModel == null ) return result ;
      var lengthParentPartModelNumber = hiroiSetCdMasterModel.LengthParentPartModelNumber ;
      var hiroiSetMasterModel = ! string.IsNullOrEmpty( ecoMode ) && bool.Parse( ecoMode ) ?  _hiroiSetMasterEcoModels.SingleOrDefault( x => x.ParentPartModelNumber == lengthParentPartModelNumber ) :  _hiroiSetMasterNormalModels.SingleOrDefault( x => x.ParentPartModelNumber == lengthParentPartModelNumber );
      if ( hiroiSetMasterModel == null ) return result ;
      result = GetWireBook( productCode, hiroiSetMasterModel ) ;
      return result ;
    }
    
    private string GetWireBook( string materialCode, HiroiSetMasterModel hiroiSetMasterModel ) 
    {
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode1 ) && int.Parse(hiroiSetMasterModel.MaterialCode1) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity1 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode2 ) && int.Parse(hiroiSetMasterModel.MaterialCode2) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity2 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode3 ) && int.Parse(hiroiSetMasterModel.MaterialCode3) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity3 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode4 ) && int.Parse(hiroiSetMasterModel.MaterialCode4) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity4 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode5 ) && int.Parse(hiroiSetMasterModel.MaterialCode5) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity5 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode6 ) && int.Parse(hiroiSetMasterModel.MaterialCode6) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity6 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode7 ) && int.Parse(hiroiSetMasterModel.MaterialCode7) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity7 ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode8 ) && int.Parse(hiroiSetMasterModel.MaterialCode8) == int.Parse(materialCode) ) return hiroiSetMasterModel.Quantity8 ;
      return string.Empty ;
    }
    
    private string FindEcoMode( string routeName, RouteCache routes )
    {
      var lastSegment = GetLastSegment( routeName, routes ) ;
      if ( lastSegment == null ) return string.Empty ;
      var toEndPointKey = lastSegment.ToEndPoint.Key ;
      var toElementId = toEndPointKey.GetElementUniqueId() ;
      if ( string.IsNullOrEmpty( toElementId ) ) return string.Empty ;
      var toConnector = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .FirstOrDefault( c => c.UniqueId == toElementId ) ;
      return toConnector == null ? string.Empty : toConnector.LookupParameter( "IsEcoMode" ).AsString() ;
    }
    
    private bool IsWire( string productCode )
    {
      var hiroiMaster = _hiroiMasterModels.SingleOrDefault( h => ( int.Parse( h.Buzaicd ) == int.Parse( productCode.Split( '-' ).First() ) ) ) ;
      return hiroiMaster is { Buzaisyu: "電線" } ;
    }
    
    private RouteSegment? GetLastSegment( string routeName, RouteCache routes )
    {
      if ( string.IsNullOrEmpty( routeName ) ) return null ;
      var route = routes.SingleOrDefault( x => x.Key == routeName ) ;
      return route.Value.RouteSegments.LastOrDefault();
    }
    
    private List<PickUpItemModel> MergePickUpModels( IEnumerable<PickUpItemModel> pickUpModels, ProductType productType )
    {
      switch ( productType ) {
        case ProductType.Conduit :
        {
          return pickUpModels.GroupBy( p => new { p.Construction, p.Classification, p.ProductName, p.Specification, p.Floor, p.ConstructionItems } ).Select( p =>
          {
            var newModel = p.First() ;
            newModel.Quantity = $"{p.Sum( x => Convert.ToDouble( x.Quantity ) )}" ;
            newModel.CeedSetCode = string.Empty ;
            newModel.ModelNumber = string.Empty ;
            newModel.Condition = string.Empty ;
            newModel.DeviceSymbol = string.Empty ;
            return newModel ;
          } ).OrderBy( p => p.Floor ).ToList() ;
        }
        case ProductType.Connector :
        {
          return pickUpModels.GroupBy( p => new { p.CeedSetCode, p.ModelNumber, p.Condition, p.DeviceSymbol, p.Floor, p.ConstructionItems } ).Select( p =>
          {
            var newModel = p.First() ;
            newModel.Quantity = $"{p.Sum( x => Convert.ToDouble( x.Quantity ) )}" ;
            newModel.Construction = string.Empty ;
            newModel.Classification = string.Empty ;
            newModel.Specification = string.Empty ;
            return newModel ;
          } ).OrderBy( p => p.Floor ).ToList() ;
        }
        case ProductType.CableTray or ProductType.CableTrayFitting :
        {
          return pickUpModels.GroupBy( p => new { p.ProductName, p.Specification } ).Select( p =>
          {
            var newModel = p.First() ;
            newModel.Quantity = $"{p.Sum( x => Convert.ToDouble( x.Quantity ) )}" ;
            return newModel ;
          } ).OrderBy(p => p.Floor).ToList() ;
        }
        default :
        {
          return new List<PickUpItemModel>() ;
        }  
      }
    }
    
    public class MaterialCodeInfo
    {
      public string MaterialCode { get ; }
      public string Name { get ; }
      public string Quantity { get ; set ; }

      public MaterialCodeInfo( string materialCode, string name, string quantity )
      {
        MaterialCode = materialCode ;
        Name = name ;
        Quantity = quantity ;
      }
    }
  }
}