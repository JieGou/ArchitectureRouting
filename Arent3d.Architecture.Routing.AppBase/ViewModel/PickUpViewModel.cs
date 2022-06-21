using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpViewModel : NotifyPropertyChanged
  {
    #region Variants

    private const string DefaultConstructionItem = "未設定" ;
    private readonly Document _document ;
    private List<PickUpModel> _pickUpModels ;
    private PickUpStorable _pickUpStorable ;
    private SymbolInformationStorable _symbolInformationStorable ;
    private CeedDetailStorable _ceedDetailStorable ;
    private readonly List<CeedModel> _ceedModels ;
    private readonly List<RegistrationOfBoardDataModel> _registrationOfBoardDataModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterEcoModels ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterNormalModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterEcoModels ;
    private Dictionary<int, string> _pickUpNumbers ;
    private int _pickUpNumber ;

    public RelayCommand<Window> ExportFileCommand => new(ExportFile) ;
    public RelayCommand<Window> SaveCommand => new(Save) ;
    public RelayCommand<Window> DeleteCommand => new(Delete) ;
    public ICommand UpdateCommand => new RelayCommand( Update ) ;
    public RelayCommand<Window> DisplaySwitchingCommand => new(DisplaySwitching) ;
    public RelayCommand<Window> CancelCommand => new(Cancel) ;

    public enum ProductType
    {
      Connector,
      Conduit,
      Cable
    }

    private enum ConduitType
    {
      Conduit,
      ConduitFitting
    }

    public List<PickUpModel> PickUpModels { get ; set ; } = new() ;

    #endregion

    #region Constructor

    public PickUpViewModel( Document document )
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
      _pickUpModels = GetPickUpData() ;
      _pickUpStorable = _document.GetPickUpStorable() ;
      if ( ! _pickUpModels.Any() ) MessageBox.Show( "Don't have element.", "Result Message" ) ;
      else {
        List<PickUpModel> pickUpConduitByNumbers = PickUpModelByNumber( ProductType.Conduit ) ;
        List<PickUpModel> pickUpRackByNumbers = PickUpModelByNumber( ProductType.Cable ) ;
        var pickUpModels = _pickUpModels.Where( p => p.EquipmentType == ProductType.Connector.GetFieldName() ).ToList() ;
        if ( pickUpConduitByNumbers.Any() ) pickUpModels.AddRange( pickUpConduitByNumbers ) ;
        if ( pickUpRackByNumbers.Any() ) pickUpModels.AddRange( pickUpRackByNumbers ) ;
        PickUpModels = ( from pickUpModel in pickUpModels orderby pickUpModel.Floor ascending select pickUpModel ).ToList() ;
      }
    }

    #endregion

    #region Bussiness Function

    private List<PickUpModel> GetPickUpData()
    {
      List<PickUpModel> pickUpModels = new() ;
      List<double> quantities = new() ;
      List<int> pickUpNumbers = new() ;
      List<string> directionZ = new() ;
      List<string> constructionItems = new() ;
      List<string?> isEcoModes = new() ;

      List<Element> allConnector = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).Where( e => e.GroupId != ElementId.InvalidElementId || ( e is FamilyInstance f && f.GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) ).ToList() ;
      foreach ( var connector in allConnector ) {
        connector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
        connector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        constructionItems.Add( string.IsNullOrEmpty( constructionItem ) ? DefaultConstructionItem : constructionItem! ) ;
        isEcoModes.Add( isEcoMode ) ;
      }

      SetPickUpModels( pickUpModels, allConnector, ProductType.Connector, quantities, pickUpNumbers, directionZ, constructionItems, isEcoModes, null, null, null ) ;
      var connectors = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      GetToConnectorsOfConduit( connectors, pickUpModels ) ;
      GetToConnectorsOfCables( connectors, pickUpModels ) ;
      GetDataFromSymbolInformation( pickUpModels ) ;
      return pickUpModels ;
    }

    private void SetPickUpModels( List<PickUpModel> pickUpModels, List<Element> elements, ProductType productType, List<double> quantities, List<int> pickUpNumbers, List<string> directionZ, List<string> constructionItemList, List<string?> isEcoModeList, Dictionary<string, string>? dictMaterialCode, List<string>? constructionClassifications, List<string>? plumbingInfos )
    {
      var index = 0 ;
      foreach ( var connector in elements ) {
        if ( connector.LevelId == ElementId.InvalidElementId ) continue ;
        var element = _document.GetElement( connector.Id ) ;
        connector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? connectorIsEcoMode ) ;
        var isEcoMode = productType == ProductType.Conduit ? isEcoModeList[ index ] : connectorIsEcoMode ;
        var item = string.Empty ;
        var floor = _document.GetAllElements<Level>().FirstOrDefault( l => l.Id == connector.LevelId )?.Name ;
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
        var quantity = productType == ProductType.Connector ? "1" : quantities[ index ].ToString() ;
        var tani = string.Empty ;
        var supplement = string.Empty ;
        var supplement2 = string.Empty ;
        var group = string.Empty ;
        var layer = string.Empty ;
        var classification = string.Empty ;
        var standard = string.Empty ;
        var pickUpNumber = productType == ProductType.Connector ? string.Empty : pickUpNumbers[ index ].ToString() ;
        var direction = productType == ProductType.Conduit ? directionZ[ index ] : string.Empty ;
        var ceedCodeModel = GetCeedSetCodeOfElement( element ) ;
        if ( _ceedModels.Any() && ceedCodeModel.Any() && ! ( productType == ProductType.Connector && ( (FamilyInstance) element ).GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) ) {
          var ceedSetCode = ceedCodeModel.First() ;
          var symbol = ceedCodeModel.Count > 1 ? ceedCodeModel.ElementAt( 1 ) : string.Empty ;
          modelNumber = ceedCodeModel.Count > 2 ? ceedCodeModel.ElementAt( 2 ) : string.Empty ;
          var ceedModels = _ceedModels.Where( x => x.CeedSetCode == ceedSetCode && x.GeneralDisplayDeviceSymbol == symbol && x.ModelNumber == modelNumber ).ToList() ;
          var ceedModel = ceedModels.FirstOrDefault() ;
          if ( ceedModel != null ) {
            modelNumber = ceedModel.ModelNumber ;
            specification2 = ceedModel.CeedSetCode ;
            supplement = ceedModel.Name ;

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
              if ( hiroiSetMasterModel != null ) {
                var materialCodes = new Dictionary<string, string>() ;
                if ( productType == ProductType.Conduit && constructionClassifications != null && ! string.IsNullOrEmpty( constructionClassifications[index] ) ) {
                  construction = constructionClassifications[ index ] ;
                  if ( plumbingInfos != null && ! string.IsNullOrEmpty( plumbingInfos[ index ] ) ) {
                    materialCodes = GetMaterialCodes( plumbingInfos, index ) ;
                  }
                }
                else {
                  materialCodes = GetMaterialCodes( hiroiSetMasterModel ) ;
                }
                if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
                  PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
                    classification, pickUpNumber, direction ) ;
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
          PickUpModelBaseOnMaterialCode( dictMaterialCode!, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
            classification, pickUpNumber, direction ) ;
        }
        
        if ( productType == ProductType.Conduit && constructionClassifications != null && ! string.IsNullOrEmpty( constructionClassifications[index] ) && ceedCodeModel.Count == 1 ) {
          construction = constructionClassifications[ index ] ;
          if ( plumbingInfos != null && ! string.IsNullOrEmpty( plumbingInfos[ index ] ) ) {
            var materialCodes = GetMaterialCodes( plumbingInfos, index ) ;
            if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
              specification2 = ceedCodeModel.First() ?? string.Empty ;
              PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
                classification, pickUpNumber, direction ) ;
            }
          }
        }

        if ( productType == ProductType.Connector && ( (FamilyInstance) element ).GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) {
          const string pullBoxName = "プルボックス一式" ;
          var hiroiSetMasterModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? _hiroiSetMasterEcoModels : _hiroiSetMasterNormalModels ;
          if ( hiroiSetMasterModels.Any() ) {
            var hiroiSetMasterModel = hiroiSetMasterModels.FirstOrDefault( h => h.ParentPartName == pullBoxName ) ;
            if ( hiroiSetMasterModel != null ) {
              var materialCodes = GetMaterialCodes( hiroiSetMasterModel ) ;
              if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
                PickUpModelBaseOnMaterialCode( materialCodes, specification, productName, size, tani, standard, productType, pickUpModels, floor, constructionItems, construction, modelNumber, specification2, item, equipmentType, use, usageName, quantity, supplement, supplement2, group, layer,
                  classification, pickUpNumber, direction ) ;
              }
            }
          }
        }

        index++ ;
      }
    }

    private void PickUpModelBaseOnMaterialCode( Dictionary<string, string> materialCodes, string specification, string productName, string size, string tani, string standard, ProductType productType, List<PickUpModel> pickUpModels, string? floor, string constructionItems, string construction,
      string modelNumber, string specification2, string item, string equipmentType, string use, string usageName, string quantity, string supplement, string supplement2, string group, string layer, string classification, string pickUpNumber, string direction )
    {
      const string defaultConduitTani = "m" ;
      foreach ( var (materialCode, name) in materialCodes ) {
        specification = name ;
        var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => int.Parse( h.Buzaicd ) == int.Parse( materialCode.Split( '-' ).First() ) ) ;
        if ( hiroiMasterModel != null ) {
          productName = hiroiMasterModel.Hinmei ;
          size = hiroiMasterModel.Size2 ;
          tani = hiroiMasterModel.Tani ;
          standard = hiroiMasterModel.Kikaku ;
        }

        if ( productType == ProductType.Connector ) {
          var pickUpModel = pickUpModels.FirstOrDefault( p =>
            p.Floor == floor && p.ConstructionItems == constructionItems && p.ProductName == productName && p.Construction == construction && p.ModelNumber == modelNumber && p.Specification == specification && p.Specification2 == specification2 && p.Size == size && p.Tani == tani ) ;
          if ( pickUpModel != null )
            pickUpModel.Quantity = ( int.Parse( pickUpModel.Quantity ) + 1 ).ToString() ;
          else {
            pickUpModel = new PickUpModel( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, group, layer, classification, standard, pickUpNumber, direction, materialCode ) ;
            pickUpModels.Add( pickUpModel ) ;
          }
        }
        else {
          if ( ! string.IsNullOrEmpty( tani ) && tani != defaultConduitTani ) tani = defaultConduitTani ;
          PickUpModel pickUpModel = new( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, group, layer, classification, standard, pickUpNumber, direction, materialCode ) ;
          pickUpModels.Add( pickUpModel ) ;
        }
      }
    }

    private Dictionary<string, string> GetMaterialCodes( HiroiSetMasterModel hiroiSetMasterNormalModel )
    {
      Dictionary<string, string> materialCodes = new() ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode1 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode1 + "-1", hiroiSetMasterNormalModel.Name1 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode2 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode2 + "-2", hiroiSetMasterNormalModel.Name2 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode3 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode3 + "-3", hiroiSetMasterNormalModel.Name3 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode4 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode4 + "-4", hiroiSetMasterNormalModel.Name4 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode5 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode5 + "-5", hiroiSetMasterNormalModel.Name5 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode6 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode6 + "-6", hiroiSetMasterNormalModel.Name6 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode7 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode7 + "-7", hiroiSetMasterNormalModel.Name7 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode8 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode8 + "-8", hiroiSetMasterNormalModel.Name8 ) ;
      return materialCodes ;
    }
    
    private Dictionary<string, string> GetMaterialCodes( List<string> plumbingInfos, int index )
    {
      var materialCodes = new Dictionary<string, string>() ;
      var plumbingInfo = plumbingInfos[ index ].Split( ':' ) ;
      var plumbingName = plumbingInfo.First() ;
      var plumbingType = plumbingInfo.ElementAt( 1 ) ;
      var plumbingSize = plumbingInfo.ElementAt( 2 ) ;
      var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => plumbingName.Contains( h.Hinmei ) && plumbingType == h.Type && plumbingSize == h.Size1 ) 
                             ?? _hiroiMasterModels.FirstOrDefault( h => plumbingType.Contains( h.Type ) && plumbingSize == h.Size1 ) ;
      if ( hiroiMasterModel != null ) {
        materialCodes.Add( hiroiMasterModel.Buzaicd, hiroiMasterModel.Kikaku ) ;
      }
      return materialCodes ;
    }

    private List<string> GetCeedSetCodeOfElement( Element element )
    {
      element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCode ) ;
      return ! string.IsNullOrEmpty( ceedSetCode ) ? ceedSetCode!.Split( ':' ).ToList() : new List<string>() ;
    }

    private void GetToConnectorsOfConduit( IReadOnlyCollection<Element> allConnectors, List<PickUpModel> pickUpModels )
    {
      _pickUpNumber = 1 ;
      _pickUpNumbers = new Dictionary<int, string>() ;
      List<Element> pickUpConnectors = new() ;
      List<double> quantities = new() ;
      List<int> pickUpNumbers = new() ;
      List<string> directionZ = new() ;
      List<string> constructionItems = new() ;
      List<string?> isEcoModes = new() ;
      List<string> constructionClassifications = new() ;
      Dictionary<string, string> dictMaterialCode = new() ;
      List<string> plumbingInfos = new() ;

      var conduits = _document.GetAllElements<Conduit>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduit in conduits ) {
        conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var quantity = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( _document, "Length" ) ).AsDouble() ;
        conduit.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
        if ( string.IsNullOrEmpty( constructionItem ) ) constructionItem = DefaultConstructionItem ;
        AddPickUpConduit( allConnectors, pickUpConnectors, quantities, pickUpNumbers, directionZ, plumbingInfos, conduit, quantity, ConduitType.Conduit, constructionItems, constructionItem!, dictMaterialCode, isEcoModes, isEcoMode, constructionClassifications, string.Empty,string.Empty ) ;
      }

      var conduitFittings = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduitFitting in conduitFittings ) {
        conduitFitting.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var quantity = conduitFitting.ParametersMap.get_Item( "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( _document, "電線管長さ" ) ).AsDouble() ;
        conduitFitting.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
        if ( string.IsNullOrEmpty( constructionItem ) ) constructionItem = DefaultConstructionItem ;
        AddPickUpConduit( allConnectors, pickUpConnectors, quantities, pickUpNumbers, directionZ, plumbingInfos, conduitFitting, quantity, ConduitType.ConduitFitting, constructionItems, constructionItem!, dictMaterialCode, isEcoModes, isEcoMode, constructionClassifications, string.Empty, string.Empty ) ;
      }
      
      var changePlumbingInformationStorable = _document.GetChangePlumbingInformationStorable() ;
      if ( changePlumbingInformationStorable.ChangePlumbingInformationModelData.Any() ) {
        foreach ( var changePlumbingInformationModel in changePlumbingInformationStorable.ChangePlumbingInformationModelData ) {
          var conduit = _document.GetElement( changePlumbingInformationModel.ConduitId ) ;
          if ( conduit == null ) continue ;
          conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
          var quantity = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( _document, "Length" ) ).AsDouble() ;
          var constructionItem = changePlumbingInformationModel.ConstructionItems ;
          var plumbingInfo = string.Join( ":", changePlumbingInformationModel.PlumbingName, changePlumbingInformationModel.PlumbingType, changePlumbingInformationModel.PlumbingSize ) ;
          AddPickUpConduit( allConnectors, pickUpConnectors, quantities, pickUpNumbers, directionZ, plumbingInfos, conduit, quantity, ConduitType.Conduit, constructionItems, constructionItem, dictMaterialCode, isEcoModes, isEcoMode, constructionClassifications, 
            changePlumbingInformationModel.ClassificationOfPlumbing, plumbingInfo, changePlumbingInformationModel.ConnectorId ) ;
        }
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, ProductType.Conduit, quantities, pickUpNumbers, directionZ, constructionItems, isEcoModes, dictMaterialCode, constructionClassifications, plumbingInfos ) ;
    }

    private void GetToConnectorsOfCables( IReadOnlyCollection<Element> allConnectors, List<PickUpModel> pickUpModels )
    {
      List<Element> pickUpConnectors = new() ;
      List<double> quantities = new() ;
      List<int> pickUpNumbers = new() ;
      List<string> directionZ = new() ;
      List<string> constructionItems = new() ;
      List<string?> isEcoModes = new() ;

      var cables = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).Distinct().ToList() ;
      foreach ( var cable in cables ) {
        cable.TryGetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, out string? toElementId ) ;
        cable.TryGetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, out string? fromElementId ) ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var checkPickUp = AddPickUpConnectors( allConnectors, pickUpConnectors, toElementId!, fromElementId!, pickUpNumbers ) ;
        if ( ! checkPickUp ) continue ;
        var quantity = cable.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( _document, "トレイ長さ" ) ).AsDouble() ;
        quantities.Add( Math.Round( quantity, 2 ) ) ;
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, ProductType.Cable, quantities, pickUpNumbers, directionZ, constructionItems, isEcoModes, null, null, null ) ;
    }

    private void AddPickUpConduit( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, List<double> quantities, List<int> pickUpNumbers, List<string> directionZ, List<string> plumbingInfos, Element conduit, double quantity, ConduitType conduitType, List<string> constructionItems,
      string constructionItem, Dictionary<string, string> dictMaterialCode, List<string?> isEcoModes, string? isEcoMode, List<string> constructionClassifications, string constructionClassification, string plumbingInfo, string? connectorId = null )
    {
      var routeName = conduit.GetRouteName() ;
      if ( string.IsNullOrEmpty( routeName ) ) return ;
      var checkPickUp = string.IsNullOrEmpty( connectorId ) 
        ? AddPickUpConnectors( allConnectors, pickUpConnectors, routeName!, pickUpNumbers, dictMaterialCode ) 
        : AddPickUpConnectors( allConnectors, pickUpConnectors, routeName!, pickUpNumbers, connectorId! ) ;
      if ( ! checkPickUp ) return ;
      quantities.Add( Math.Round( quantity, 2 ) ) ;
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
    }

    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, string routeName, List<int> pickUpNumbers, Dictionary<string, string> dictMaterialCode )
    {
      var toConnector = GetConnectorOfRoute( allConnectors, routeName, false ) ;
      var isPickUpByFromConnector = toConnector != null && ( toConnector.Name == ElectricalRoutingFamilyType.PressureConnector.GetFamilyName() || toConnector.Name == ElectricalRoutingFamilyType.ToJboxConnector.GetFamilyName() ) ;
      if( isPickUpByFromConnector )
        toConnector = GetConnectorOfRoute( allConnectors, routeName, true ) ;
      if ( toConnector == null || ( toConnector.GroupId == ElementId.InvalidElementId && toConnector.Name != ElectricalRoutingFamilyType.PullBox.GetFamilyName() ) ) return false ;

      //Case connector is Power type, check from and to connector existed in _registrationOfBoardDataModels then get material 
      if ( ( (FamilyInstance) toConnector ).GetConnectorFamilyType() == ConnectorFamilyType.Power ) {
        toConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfToConnector ) ;
        var registrationOfBoardDataModel = _registrationOfBoardDataModels.FirstOrDefault( x => x.SignalDestination == ceedCodeOfToConnector || x.AutoControlPanel == ceedCodeOfToConnector ) ;
        if ( registrationOfBoardDataModel == null )
          return false ;

        if ( registrationOfBoardDataModel.MaterialCode1.Length > 2 && ! dictMaterialCode.ContainsKey( registrationOfBoardDataModel.MaterialCode1.Substring( 2 ) ) )
          dictMaterialCode.Add( registrationOfBoardDataModel.MaterialCode1.Substring( 2 ), registrationOfBoardDataModel.Kind1 ) ;

        if ( registrationOfBoardDataModel.MaterialCode2.Length > 2 && ! dictMaterialCode.ContainsKey( registrationOfBoardDataModel.MaterialCode2.Substring( 2 ) ) )
          dictMaterialCode.Add( registrationOfBoardDataModel.MaterialCode2.Substring( 2 ), registrationOfBoardDataModel.Kind2 ) ;
      }

      pickUpConnectors.Add( toConnector ) ;
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
    
    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, string routeName, List<int> pickUpNumbers, string connectorId )
    {
      var toConnector = allConnectors.SingleOrDefault( c => c.UniqueId == connectorId) ;
      if ( toConnector == null || toConnector.GroupId == ElementId.InvalidElementId ) return false ;
      
      pickUpConnectors.Add( toConnector ) ;

      _pickUpNumbers.Add( _pickUpNumber, routeName ) ;
      pickUpNumbers.Add( _pickUpNumber ) ;
      _pickUpNumber++ ;

      return true ;
    }

    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, string elementId, string fromElementId, List<int> pickUpNumbers )
    {
      var connector = allConnectors.FirstOrDefault( c => c.UniqueId == elementId ) ;
      if ( connector!.IsTerminatePoint() || connector!.IsPassPoint() ) {
        connector!.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? connectorId ) ;
        if ( ! string.IsNullOrEmpty( connectorId ) ) {
          connector = allConnectors.FirstOrDefault( c => c.UniqueId == connectorId ) ;
          elementId = connectorId! ;
        }
      }

      if ( ! string.IsNullOrEmpty( fromElementId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromElementId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorId ) ;
          fromElementId = fromConnectorId! ;
        }
      }

      if ( connector != null && connector.GroupId != ElementId.InvalidElementId ) {
        pickUpConnectors.Add( connector ) ;
        if ( ! _pickUpNumbers.ContainsValue( fromElementId + ", " + elementId ) ) {
          _pickUpNumbers.Add( _pickUpNumber, fromElementId + ", " + elementId ) ;
          pickUpNumbers.Add( _pickUpNumber ) ;
          _pickUpNumber++ ;
        }
        else {
          var pickUpNumber = _pickUpNumbers.FirstOrDefault( n => n.Value == fromElementId + ", " + elementId ).Key ;
          pickUpNumbers.Add( pickUpNumber ) ;
        }

        return true ;
      }

      return false ;
    }

    private List<PickUpModel> PickUpModelByNumber( ProductType productType )
    {
      List<PickUpModel> pickUpModels = new() ;
      var equipmentType = productType.GetFieldName() ;
      var pickUpModelsByNumber = _pickUpModels
        .Where( p => p.EquipmentType == equipmentType )
        .GroupBy( x => x.PickUpNumber )
        .Select( g => g.ToList() ) ;
      foreach ( var pickUpModelByNumber in pickUpModelsByNumber ) {
        var pickUpModelsByProductCode = pickUpModelByNumber
          .GroupBy( x => x.ProductCode)
          .Select( g => g.ToList() ) ;
        foreach ( var pickUpModelByProductCode in pickUpModelsByProductCode ) {
          var pickUpModelsByConstructionItemsAndConstruction = pickUpModelByProductCode
            .GroupBy( x => ( x.ConstructionItems, x.Construction ) )
            .Select( g => g.ToList() ) ;
          foreach ( var pickUpModelByConstructionItemsAndConstruction in pickUpModelsByConstructionItemsAndConstruction ) {
            var sumQuantity = pickUpModelByConstructionItemsAndConstruction.Sum( p => Convert.ToDouble( p.Quantity ) ) ;
            var pickUpModel = pickUpModelByConstructionItemsAndConstruction.FirstOrDefault() ;
            if ( pickUpModel == null ) continue ;
            PickUpModel newPickUpModel = new( pickUpModel.Item, pickUpModel.Floor, pickUpModel.ConstructionItems, pickUpModel.EquipmentType, pickUpModel.ProductName, pickUpModel.Use, pickUpModel.UsageName, pickUpModel.Construction, pickUpModel.ModelNumber, pickUpModel.Specification,
              pickUpModel.Specification2, pickUpModel.Size, sumQuantity.ToString(), pickUpModel.Tani, pickUpModel.Supplement, pickUpModel.Supplement2, pickUpModel.Group, pickUpModel.Layer, pickUpModel.Classification, pickUpModel.Standard, pickUpModel.PickUpNumber, pickUpModel.Direction,
              pickUpModel.ProductCode ) ;
            pickUpModels.Add( newPickUpModel ) ;
          }
        }
      }

      return pickUpModels ;
    }

    private Element? GetConnectorOfRoute( IReadOnlyCollection<Element> allConnectors, string routeName, bool isFrom )
    {
      var conduitsOfRoute = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( isFrom ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
        return toConnector ;
      }

      return null ;
    }


    /// <summary>
    /// Get all coinduit data from SymbolInformation and add to list pickupModel
    /// </summary>
    /// <param name="pickUpModels"></param>
    private void GetDataFromSymbolInformation( List<PickUpModel> pickUpModels )
    {
      var symbolInformations = _symbolInformationStorable.AllSymbolInformationModelData ;
      var ceedDetails = _ceedDetailStorable.AllCeedDetailModelData ;

      foreach ( var symbolInformation in symbolInformations ) {
        var floor = symbolInformation.Floor ;
        foreach ( var ceedDetail in ceedDetails.FindAll( x => x.ParentId == symbolInformation.Id ) ) {
          PickUpModel newPickUpModel = new( null, floor, DefaultConstructionItem, ProductType.Conduit.GetFieldName(), ceedDetail.ProductName, null, null, ceedDetail.ConstructionClassification, ceedDetail.ModeNumber, 
            ceedDetail.Specification, ceedDetail.CeedCode, ceedDetail.Size2, ceedDetail.Total.ToString( CultureInfo.InvariantCulture ), ceedDetail.Unit, ceedDetail.Supplement, null, null, null,
            ceedDetail.Classification, ceedDetail.Standard, null, null, ceedDetail.ProductCode ) ;
          var oldPickUpModel = pickUpModels.FirstOrDefault( x => x.Floor == newPickUpModel.Floor && x.ProductName == newPickUpModel.ProductName && x.ProductCode == newPickUpModel.ProductCode && x.Specification == newPickUpModel.Specification) ;
          if ( null != oldPickUpModel ) {
            oldPickUpModel.Quantity = ( double.Parse( oldPickUpModel.Quantity ) + double.Parse( newPickUpModel.Quantity ) ).ToString( CultureInfo.InvariantCulture ) ;
          }
          else {
            pickUpModels.Add( newPickUpModel ) ;
          }
        }
      }
    }

    #endregion

    #region Command Method

    private void ExportFile( Window window )
    {
      const string fileName = "file_name.dat" ;
      SaveFileDialog saveFileDialog = new() { FileName = fileName, Filter = "CSV files (*.dat)|*.dat", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != DialogResult.OK ) return ;
      try {
        using ( StreamWriter sw = new( saveFileDialog.FileName ) ) {
          foreach ( var p in _pickUpModels ) {
            List<string> param = new()
            {
              p.Floor,
              p.ConstructionItems,
              p.EquipmentType,
              p.ProductName,
              p.Use,
              p.Construction,
              p.ModelNumber,
              p.Specification,
              p.Specification2,
              p.Size,
              p.Quantity,
              p.Tani
            } ;
            string line = "\"" + string.Join( "\",\"", param ) + "\"" ;
            sw.WriteLine( line ) ;
          }

          sw.Flush() ;
          sw.Close() ;
        }

        MessageBox.Show( "Export data successfully.", "Result Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export data failed because " + ex, "Error Message" ) ;
      }

      window.DialogResult = true ;
      window.Close() ;
    }

    private void Save( Window window )
    {
      try {
        using Transaction t = new Transaction( _document, "Save data" ) ;
        t.Start() ;
        _pickUpStorable.AllPickUpModelData = _pickUpModels ;
        _pickUpStorable.Save() ;
        t.Commit() ;
        window.DialogResult = true ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
        window.DialogResult = false ;
      }
    }

    private void DisplaySwitching( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    private void Update()
    {
    }

    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    private void Delete( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    #endregion
  }
}