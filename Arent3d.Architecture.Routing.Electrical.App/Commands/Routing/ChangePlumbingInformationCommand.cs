﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ChangePlumbingInformationCommand", DefaultString = "Change\nPlumbing Information" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class ChangePlumbingInformationCommand : IExternalCommand
  {
    private const string DefaultConstructionItems = "未設定" ;
    private const string DefaultParentPlumbingType = "E" ;
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDoc = commandData.Application.ActiveUIDocument ;
        var doc = uiDoc.Document ;
        var selection = uiDoc.Selection ;

        return doc.Transaction( "TransactionName.Commands.Routing.AddSymbol".GetAppStringByKeyOrDefault( "Create Detail Symbol" ), _ =>
        {
          var allElementSelected = selection.GetElementIds() ;
          if ( ! allElementSelected.Any() ) 
            return Result.Cancelled ;
          
          var connectorsSelected = doc.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.OtherElectricalElements )
            .Where( e => allElementSelected.Contains( e.Id ) && 
                         e.Symbol.FamilyName != ElectricalRoutingFamilyType.FromPowerEquipment.GetFamilyName() && 
                         e.Symbol.FamilyName != ElectricalRoutingFamilyType.ToPowerEquipment.GetFamilyName() ).ToList() ;
          var conduitsSelected = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( e => allElementSelected.Contains( e.Id ) ).ToList() ;
          var conduitAndConnectorDic = GetConduitAndConnector( doc, connectorsSelected, conduitsSelected ) ;
          if ( ! conduitAndConnectorDic.Any() ) return Result.Cancelled ;

          var changePlumbingInformationStorable = doc.GetChangePlumbingInformationStorable() ;
          var viewModel = CreateChangePlumbingInformationViewModel( doc, conduitAndConnectorDic, changePlumbingInformationStorable ) ;
          var view = new ChangePlumbingInformationDialog { DataContext = viewModel } ;
          view.ShowDialog() ;
          if ( ! ( view.DialogResult ?? false ) ) return Result.Cancelled ;

          foreach ( var item in viewModel.ChangePlumbingInformationModels ) {
            var oldChangePlumbingInformationModel = changePlumbingInformationStorable.ChangePlumbingInformationModelData.SingleOrDefault( c => c.ConduitId == item.ConduitId ) ;
            if ( oldChangePlumbingInformationModel == null ) {
              var changePlumbingInformationModel = new ChangePlumbingInformationModel( item.ConduitId, item.ConnectorId, item.PlumbingType, item.PlumbingSize, item.NumberOfPlumbing, item.PlumbingName, item.ClassificationOfPlumbing, item.ConstructionItems, item.WireCrossSectionalArea, item.IsExposure, item.IsInDoor, item.ConduitDirectionZ ) ;
              changePlumbingInformationStorable.ChangePlumbingInformationModelData.Add( changePlumbingInformationModel ) ;
            }
            else {
              oldChangePlumbingInformationModel.PlumbingType = item.PlumbingType ;
              oldChangePlumbingInformationModel.PlumbingSize = item.PlumbingSize ;
              oldChangePlumbingInformationModel.NumberOfPlumbing = item.NumberOfPlumbing ;
              oldChangePlumbingInformationModel.PlumbingName = item.PlumbingName ;
              oldChangePlumbingInformationModel.ClassificationOfPlumbing = item.ClassificationOfPlumbing ;
              oldChangePlumbingInformationModel.ConstructionItems = item.ConstructionItems ;
              oldChangePlumbingInformationModel.IsExposure = item.IsExposure ;
              oldChangePlumbingInformationModel.IsInDoor = item.IsInDoor ;
            }
          }

          changePlumbingInformationStorable.Save() ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    private Dictionary<Element, Element> GetConduitAndConnector( Document document, List<FamilyInstance> connectors, List<Element> conduits )
    {
      var conduitAndConnectorDic = new Dictionary<Element, Element>() ;
      if ( connectors.Any() ) {
        var allConduitWithDirectionByZ = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.Location is LocationCurve { Curve: Line line } && ( line.Direction.Z is 1 or -1 ) ).Distinct().ToDictionary( c => c, c => ( ( c.Location as LocationCurve )?.Curve as Line )?.Origin ?? new XYZ() ) ;
        foreach ( var connector in connectors ) {
          var connectorLocation = ( connector.Location as LocationPoint ) ! ;
          var (x, y, _) = connectorLocation.Point ;
          var conduitsOfConnector = allConduitWithDirectionByZ.Where( c => Math.Abs( c.Value!.X - x ) < 0.01 && Math.Abs( c.Value.Y - y ) < 0.01 ).Select( c => c.Key ) ;
          foreach ( var conduit in conduitsOfConnector ) {
            if ( conduitAndConnectorDic.SingleOrDefault( c => c.Key.UniqueId == conduit.UniqueId ).Key == null ) {
              conduitAndConnectorDic.Add( conduit, connector ) ;
            }
          }
        }
      }

      if ( conduits.Any() ) {
        var allConnectors = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.OtherElectricalElements )
          .Where( f => f.Symbol.FamilyName != ElectricalRoutingFamilyType.FromPowerEquipment.GetFamilyName() && 
                       f.Symbol.FamilyName != ElectricalRoutingFamilyType.ToPowerEquipment.GetFamilyName() ).ToList() ;
        var allConduitWithDirectionByZ = conduits.Where( c => c.Location is LocationCurve { Curve: Line line } && ( line.Direction.Z is 1 or -1 ) ).Distinct().ToDictionary( c => c, c => ( ( c.Location as LocationCurve )?.Curve as Line )?.Origin ?? new XYZ() ) ;
        foreach ( var (conduit, (x, y, _)) in allConduitWithDirectionByZ ) {
          var connectorOfConduit = allConnectors.Where( c => c.Location is LocationPoint connectorLocation && Math.Abs( connectorLocation.Point.X - x ) < 0.01 && Math.Abs( connectorLocation.Point.Y - y ) < 0.01 ) ;
          foreach ( var connector in connectorOfConduit ) {
            if ( conduitAndConnectorDic.SingleOrDefault( c => c.Key.UniqueId == conduit.UniqueId ).Key == null ) {
              conduitAndConnectorDic.Add( conduit, connector ) ;
            }
          }
        }
      }

      return conduitAndConnectorDic ;
    }

    private enum ConcealmentOrExposure
    {
      隠蔽,
      露出
    }

    private enum InOrOutDoor
    {
      屋内,
      屋外
    }

    private ChangePlumbingInformationViewModel CreateChangePlumbingInformationViewModel( Document doc, Dictionary<Element, Element> conduitAndConnectorDic, ChangePlumbingInformationStorable changePlumbingInformationStorable )
    {
      const double percentage = 0.32 ;
      const string outDoorCondition = "屋外" ; 
      var ceedModels = doc.GetCeedStorable().CeedModelData ;
      var storageService = new StorageService<Level, DetailSymbolModel>( ( (ViewPlan) doc.ActiveView ).GenLevel ) ;
      var csvStorable = doc.GetCsvStorable() ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var registrationOfBoardDataModels = doc.GetRegistrationOfBoardDataStorable().RegistrationOfBoardData ;

      var plumbingTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
      var plumbingTypes = ( from conduitTypeName in plumbingTypeNames select new DetailTableItemModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
      plumbingTypes.Add( new DetailTableItemModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;

      var hiroiCdModel = csvStorable.HiroiSetCdMasterNormalModelData ;
      var classificationOfPlumbingNames = hiroiCdModel.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
      var classificationsOfPlumbing = ( from classificationOfPlumbingName in classificationOfPlumbingNames select new DetailTableItemModel.ComboboxItemType( classificationOfPlumbingName, classificationOfPlumbingName ) ).ToList() ;

      var concealmentOrExposure = new List<DetailTableItemModel.ComboboxItemType>() { new( ConcealmentOrExposure.隠蔽.GetFieldName(), "False" ), new( ConcealmentOrExposure.露出.GetFieldName(), "True" ) } ;
      var inOrOutDoor = new List<DetailTableItemModel.ComboboxItemType>() { new( InOrOutDoor.屋内.GetFieldName(), "True" ), new( InOrOutDoor.屋外.GetFieldName(), "False" ) } ;

      var changePlumbingInformationModels = new List<ChangePlumbingInformationModel>() ;
      var connectorInfos = new List<ChangePlumbingInformationViewModel.ConnectorInfo>() ;
      foreach ( var (conduit, connector) in conduitAndConnectorDic ) {
        var oldChangePlumbingInformationModel = changePlumbingInformationStorable.ChangePlumbingInformationModelData.SingleOrDefault( c => c.ConduitId == conduit.UniqueId ) ;
        var detailSymbolModel = storageService.Data.DetailSymbolData.FirstOrDefault( s => s.ConduitUniqueId == conduit.UniqueId ) ;
        var plumbingType = oldChangePlumbingInformationModel != null 
          ? oldChangePlumbingInformationModel.PlumbingType 
          : ( detailSymbolModel?.PlumbingType ?? DefaultParentPlumbingType ) ;

        conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var constructionClassification = oldChangePlumbingInformationModel != null 
          ? oldChangePlumbingInformationModel.ClassificationOfPlumbing 
          : classificationOfPlumbingNames.First() ;
        var wireCrossSectionalArea = oldChangePlumbingInformationModel?.WireCrossSectionalArea ?? 0 ;
        var ceedCodeInfo = ElectricalCommandUtil.GetCeedSetCodeOfElement( connector ) ;
        var ceedCode = ceedCodeInfo.FirstOrDefault() ;
        var deviceSymbol = ceedCodeInfo.Count > 1 ? ceedCodeInfo.ElementAt( 1 ) : string.Empty ;
        var modelNumber = ceedCodeInfo.Count > 2 ? ceedCodeInfo.ElementAt( 2 ) : string.Empty ;
        var isInDoor = oldChangePlumbingInformationModel?.IsInDoor ?? true ;
        if ( ! string.IsNullOrEmpty( ceedCode ) ) {
          var ceedModel = ceedModels.FirstOrDefault( c => c.CeedSetCode == ceedCode && c.GeneralDisplayDeviceSymbol == deviceSymbol && c.ModelNumber == modelNumber ) ;
          var registrationOfBoardDataModel = registrationOfBoardDataModels.FirstOrDefault( b => b.AutoControlPanel == ceedCode || b.SignalDestination == ceedCode ) ;
          List<string> materialCodes = new() ;
          if ( registrationOfBoardDataModel == null ) {
            var hiroiSetCdMasterModel = hiroiCdModel.FirstOrDefault( h => h.SetCode == ceedCode ) ;
            if ( hiroiSetCdMasterModel != null ) {
              constructionClassification = hiroiSetCdMasterModel.ConstructionClassification ;
              var ceedModelNumber = hiroiSetCdMasterModel.LengthParentPartModelNumber ;
              var hiroiSetMasterModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) 
                ? csvStorable.HiroiSetMasterEcoModelData 
                : csvStorable.HiroiSetMasterNormalModelData ;
              var hiroiSetMasterModel = hiroiSetMasterModels.FirstOrDefault( h => h.ParentPartModelNumber == ceedModelNumber ) ;
              if ( hiroiSetMasterModel != null ) {
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode1 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode1 ) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode2 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode2) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode3 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode3 ) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode4 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode4 ) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode5 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode5 ) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode6 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode6 ) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode7 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode7 ) ;
                if ( ! string.IsNullOrEmpty( hiroiSetMasterModel.MaterialCode8 ) ) materialCodes.Add( hiroiSetMasterModel.MaterialCode8 ) ;
                wireCrossSectionalArea = GetWireCrossSectionalArea( csvStorable.HiroiMasterModelData, csvStorable.WiresAndCablesModelData, materialCodes ) ;
              }
            }

            if ( ceedModel != null ) {
              isInDoor = ceedModel.Condition != outDoorCondition ;
            }
          }
          else {
            if ( ! string.IsNullOrEmpty( registrationOfBoardDataModel.MaterialCode1 ) ) materialCodes.Add( registrationOfBoardDataModel.MaterialCode1 ) ;
            if ( ! string.IsNullOrEmpty( registrationOfBoardDataModel.MaterialCode2 ) ) materialCodes.Add( registrationOfBoardDataModel.MaterialCode2 ) ;
            wireCrossSectionalArea = GetWireCrossSectionalArea( csvStorable.HiroiMasterModelData, csvStorable.WiresAndCablesModelData, materialCodes ) ;
            deviceSymbol = ceedCode ;
          }
        }

        string plumbingSize ;
        string? plumbingName ;
        var numberOfPlumbing = oldChangePlumbingInformationModel?.NumberOfPlumbing ?? "1" ;
        if ( plumbingType != NoPlumping ) {
          var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          var plumbingSizesOfPlumbingType = conduitsModels.Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          plumbingSize = oldChangePlumbingInformationModel != null ? oldChangePlumbingInformationModel.PlumbingSize : plumbingSizesOfPlumbingType.First() ;
          plumbingName = oldChangePlumbingInformationModel != null ? oldChangePlumbingInformationModel.PlumbingName : conduitsModels.First().Name ;
          if ( oldChangePlumbingInformationModel == null ) {
            var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= wireCrossSectionalArea / percentage ) ?? conduitsModels.Last() ;
            plumbingSize = plumbing.Size.Replace( "mm", "" ) ;
            plumbingName = plumbing.Classification ;
            if ( plumbing == conduitsModels.Last() ) numberOfPlumbing = ( (int) Math.Ceiling( ( wireCrossSectionalArea / percentage ) / double.Parse( plumbing.InnerCrossSectionalArea ) ) ).ToString() ;
          }
        }
        else {
          plumbingSize = NoPlumbingSize ;
          numberOfPlumbing = string.Empty ;
          plumbingName = string.Empty ;
        }

        conduit.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? conduitConstructionItem ) ;
        var constructionItem = string.IsNullOrEmpty( conduitConstructionItem ) ? DefaultConstructionItems : conduitConstructionItem ;

        var isExposure = ( oldChangePlumbingInformationModel?.IsExposure ?? false ) || constructionClassification == CreateDetailTableCommandBase.ConstructionClassificationType.露出.GetFieldName() ;

        var conduitLocation = conduit.Location as LocationCurve ;
        var conduitLine = conduitLocation?.Curve as Line ;
        var conduitDirectionZ = conduitLine?.Direction.Z ;

        var changePlumbingInformationModel = new ChangePlumbingInformationModel( conduit.UniqueId, connector.UniqueId, plumbingType, plumbingSize, numberOfPlumbing, plumbingName, constructionClassification, constructionItem, wireCrossSectionalArea, isExposure, isInDoor, conduitDirectionZ ) ;
        changePlumbingInformationModels.Add( changePlumbingInformationModel ) ;

        var connectorLocation = ( connector.Location as LocationPoint ) ! ;
        var (x, y, z) = connectorLocation.Point ;
        var connectorName = deviceSymbol + " ( X:" + Math.Round( x, 3 ) + ", Y:" + Math.Round( y, 3 ) + ", Z:" + Math.Round( z, 3 ) + " )" ;
        var connectorInfo = new ChangePlumbingInformationViewModel.ConnectorInfo( connectorName, constructionItem!, conduitDirectionZ ) ;
        if ( connectorInfos.FirstOrDefault( c => c.Connector == connectorName && c.ConduitDirectionZ == conduitDirectionZ ) == null )
          connectorInfos.Add( connectorInfo ) ;
      }
      
      var changePlumbingInformationModelsByConnectors = changePlumbingInformationModels.GroupBy( c => ( c.ConnectorId, c.ConduitDirectionZ ) ).Select( g => g.ToList() ).ToList() ;
      var newChangePlumbingInformationModels = new List<ChangePlumbingInformationModel>() ;
      foreach ( var changePlumbingInformationModelsByConnector in changePlumbingInformationModelsByConnectors ) {
        if ( changePlumbingInformationModelsByConnector.Count > 1 ) {
          var wireCrossSectionalArea = changePlumbingInformationModelsByConnector.Sum( c => c.WireCrossSectionalArea ) ;
          changePlumbingInformationModelsByConnector.First().WireCrossSectionalArea = wireCrossSectionalArea ;
          newChangePlumbingInformationModels.Add( changePlumbingInformationModelsByConnector.First() ) ;
        }
        else {
          newChangePlumbingInformationModels.Add( changePlumbingInformationModelsByConnector.First() ) ;
        }
      }

      var viewModel = new ChangePlumbingInformationViewModel( conduitsModelData, newChangePlumbingInformationModels, plumbingTypes, classificationsOfPlumbing, concealmentOrExposure, inOrOutDoor, connectorInfos ) ;
      return viewModel ;
    }

    private double GetWireCrossSectionalArea( List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<string> materialCodes )
    {
      double wireCrossSectionalArea = 0 ;
      foreach ( var materialCode in materialCodes ) {
        var masterModel = hiroiMasterModelData.FirstOrDefault( h => int.Parse( h.Buzaicd ).ToString() == int.Parse( materialCode ).ToString() ) ;
        if ( masterModel == null ) continue ;
        var wireType = masterModel.Type ;
        var wireSize = masterModel.Size1 ;
        var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && masterModel.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && masterModel.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
        if ( wiresAndCablesModel == null ) continue ;
        wireCrossSectionalArea += double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
      }
      
      return wireCrossSectionalArea ;
    }
  }
}