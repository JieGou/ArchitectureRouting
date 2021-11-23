using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpDialog : Window
  {
    private readonly Document _document ;
    private readonly List<CeedModel> _ceeDModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModels ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;

    public PickUpDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _ceeDModels = new List<CeedModel>() ;
      _hiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _hiroiMasterModels = new List<HiroiMasterModel>() ;

      var ceeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable != null ) _ceeDModels = ceeDStorable.CeedModelData ;

      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      if ( csvStorable != null ) {
        _hiroiSetMasterNormalModels = csvStorable.HiroiSetMasterNormalModelData ;
        _hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      }
    }

    private void Load_AllData( object sender, RoutedEventArgs e )
    {
      var pickUpData = GetPickUpData( DataType.All ) ;
      ShowData( pickUpData, DataType.All ) ;
    }

    private void Load_AirConditioningPipingData( object sender, RoutedEventArgs e )
    {
      var pickUpData = GetPickUpData( DataType.AirConditioningPiping ) ;
      ShowData( pickUpData, DataType.AirConditioningPiping ) ;
    }

    private void Load_SatellitePlumbingData( object sender, RoutedEventArgs e )
    {
      var pickUpData = GetPickUpData( DataType.SatellitePlumbing ) ;
      ShowData( pickUpData, DataType.SatellitePlumbing ) ;
    }

    private void Load_ConduitData( object sender, RoutedEventArgs e )
    {
      var pickUpData = GetPickUpData( DataType.Duct ) ;
      ShowData( pickUpData, DataType.Duct ) ;
    }

    private void Load_ElectricityData( object sender, RoutedEventArgs e )
    {
      var pickUpData = GetPickUpData( DataType.Electricity ) ;
      ShowData( pickUpData, DataType.Electricity ) ;
    }

    private void Load_OtherData( object sender, RoutedEventArgs e )
    {
      var pickUpData = GetPickUpData( DataType.Other ) ;
      ShowData( pickUpData, DataType.Other ) ;
    }

    private void ShowData( List<PickUpModel> pickUpData, DataType dataType )
    {
      PickUpStorable pickUpStorable = _document.GetPickUpStorable() ;
      if ( ! pickUpData.Any() ) MessageBox.Show( "Don't have element.", "Result Message" ) ;
      else {
        pickUpData = ( from pickUpModel in pickUpData orderby pickUpModel.Floor ascending select pickUpModel ).ToList() ;
        var viewModel = new ViewModel.PickUpViewModel( pickUpStorable, pickUpData ) ;
        var contentDisplayDialog = new ContentDisplayDialog( viewModel ) ;
        contentDisplayDialog.ShowDialog() ;
        if ( ! ( contentDisplayDialog.DialogResult ?? false ) ) return ;
        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          switch ( dataType ) {
            case DataType.All :
              pickUpStorable.AllPickUpModelData = pickUpData ;
              break ;
            case DataType.AirConditioningPiping :
              pickUpStorable.AirConditioningPipingData = pickUpData ;
              break ;
            case DataType.SatellitePlumbing :
              pickUpStorable.SatellitePlumbingData = pickUpData ;
              break ;
            case DataType.Duct :
              pickUpStorable.DuctData = pickUpData ;
              break ;
            case DataType.Electricity :
              pickUpStorable.ElectricityData = pickUpData ;
              break ;
            case DataType.Other :
              pickUpStorable.OtherData = pickUpData ;
              break ;
            default :
              throw new ArgumentOutOfRangeException( nameof( dataType ), dataType, null ) ;
          }

          pickUpStorable.Save() ;
          t.Commit() ;
          DialogResult = true ;
          Close() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
          MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
          DialogResult = false ;
        }
      }
    }

    private enum DataType
    {
      All,
      AirConditioningPiping,
      SatellitePlumbing,
      Duct,
      Electricity,
      Other
    }

    private List<PickUpModel> GetPickUpData( DataType dataType )
    {
      List<PickUpModel> pickUpModels = new List<PickUpModel>() ;
      List<Element> elements = new List<Element>() ;
      switch ( dataType ) {
        case DataType.All :
          elements = _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalEquipment ).ToList() ;
          var connectors = _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).ToList() ;
          var connectorsOfConduitAndCable = GetToConnectorsOfConduit( connectors ) ;
          GetToConnectorsOfCables( connectors, connectorsOfConduitAndCable ) ;
          if ( connectorsOfConduitAndCable.Any() ) elements.AddRange( connectorsOfConduitAndCable ) ;
          break ;
        case DataType.AirConditioningPiping :
          break ;
        case DataType.SatellitePlumbing :
          break ;
        case DataType.Duct :
          break ;
        case DataType.Electricity :
          break ;
        default :
          break ;
      }

      foreach ( var connector in elements ) {
        if ( connector.LevelId == ElementId.InvalidElementId ) continue ;
        var element = _document.GetElement( connector.Id ) ;
        var item = string.Empty ;
        var floor = _document.GetAllElements<Level>().FirstOrDefault( l => l.Id == connector.LevelId )?.Name ;
        var constructionItems = string.Empty ;
        var facility = string.Empty ;
        var productName = string.Empty ;
        var use = string.Empty ;
        var construction = string.Empty ;
        var modelNumber = string.Empty ;
        var specification = string.Empty ;
        var specification2 = string.Empty ;
        var size = string.Empty ;
        var quantity = string.Empty ;
        var tani = string.Empty ;
        var ceeDSetCode = GetCeeDSetCodeOfElement( element ) ;
        if ( _ceeDModels.Any() && ! string.IsNullOrEmpty( ceeDSetCode ) ) {
          var ceeDModel = _ceeDModels.FirstOrDefault( x => x.CeeDSetCode == ceeDSetCode ) ;
          if ( ceeDModel != null ) {
            modelNumber = ceeDModel.CeeDModelNumber ;
            specification2 = ceeDModel.CeeDSetCode ;
            if ( _hiroiSetMasterNormalModels.Any() && ! string.IsNullOrEmpty( modelNumber ) ) {
              var hiroiSetMasterNormalModel = _hiroiSetMasterNormalModels.FirstOrDefault( h => h.ParentPartModelNumber == modelNumber ) ;
              if ( hiroiSetMasterNormalModel != null ) {
                specification = hiroiSetMasterNormalModel.ParentPartName ;
                quantity = hiroiSetMasterNormalModel.ParentPartsQuantity ;
                var materialCode1 = hiroiSetMasterNormalModel.MaterialCode1 ;
                if ( _hiroiMasterModels.Any() && ! string.IsNullOrEmpty( materialCode1 ) ) {
                  var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => int.Parse( h.Buzaicd ) == int.Parse( materialCode1 ?? string.Empty ) ) ;
                  if ( hiroiMasterModel != null ) {
                    facility = hiroiMasterModel.Setubisyu ;
                    productName = hiroiMasterModel.Hinmei ;
                    size = hiroiMasterModel.Size1 ;
                    tani = hiroiMasterModel.Tani ;
                  }
                }
              }
            }
          }
        }

        var supplement = string.Empty ;
        var supplement2 = string.Empty ;
        var glue = string.Empty ;
        var layer = string.Empty ;
        var classification = string.Empty ;
        PickUpModel pickUpModel = new PickUpModel( item, floor, constructionItems, facility, productName, use, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, glue, layer, classification ) ;
        pickUpModels.Add( pickUpModel ) ;
      }

      return pickUpModels ;
    }

    private string GetCeeDSetCodeOfElement( Element element )
    {
      var ceeDSetCode = string.Empty ;
      if ( element.GroupId == ElementId.InvalidElementId ) return ceeDSetCode ?? string.Empty ;
      var groupId = _document.GetAllElements<Group>().FirstOrDefault( g => g.AttachedParentId == element.GroupId )?.Id ;
      if ( groupId != null )
        ceeDSetCode = _document.GetAllElements<TextNote>().FirstOrDefault( t => t.GroupId == groupId )?.Text.Trim( '\r' ) ;

      return ceeDSetCode ?? string.Empty ;
    }

    private List<Element> GetToConnectorsOfConduit( IReadOnlyCollection<Element> allConnectors )
    {
      List<Element> connectors = new List<Element>() ;
      var conduits = _document.GetAllElements<Conduit>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduit in conduits ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ) ;
        var endPointKey = toEndPoint.FirstOrDefault()?.Key ;
        var elementId = endPointKey!.GetElementId() ;
        if ( string.IsNullOrEmpty( elementId ) ) continue ;
        foreach ( var connector in allConnectors ) {
          if ( connector.Id.ToString() == elementId && ! connectors.Contains( connector ) ) {
            connectors.Add( connector ) ;
          }
        }
      }

      return connectors ;
    }

    private void GetToConnectorsOfCables( IReadOnlyCollection<Element> allConnectors, List<Element> connectors )
    {
      var cables = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).Distinct().ToList() ;
      foreach ( var cable in cables ) {
        var elementId = cable.ParametersMap.get_Item( "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( _document, "To-Side Connector Id" ) ).AsString() ;
        if ( string.IsNullOrEmpty( elementId ) ) continue ;
        foreach ( var connector in allConnectors ) {
          var connectorId = connector.Id.ToString() ;
          if ( connectorId == elementId && ! connectors.Contains( connector ) ) {
            connectors.Add( connector ) ;
          }
        }
      }
    }
  }
}