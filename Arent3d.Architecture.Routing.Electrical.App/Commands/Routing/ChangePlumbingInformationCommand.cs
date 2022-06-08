﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
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
          var connectorsSelected = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).Where( e => allElementSelected.Contains( e.Id ) ).ToList() ;
          var conduitAndConnectorDic = GetConduitOfConnector( doc, connectorsSelected ) ;
          if ( ! conduitAndConnectorDic.Any() ) return Result.Cancelled ;

          var changePlumbingInformationStorable = doc.GetChangePlumbingInformationStorable() ;
          var viewModel = CreateChangePlumbingInformationViewModel( doc, conduitAndConnectorDic, changePlumbingInformationStorable ) ;
          var view = new ChangePlumbingInformationDialog { DataContext = viewModel } ;
          view.ShowDialog() ;
          if ( ! ( view.DialogResult ?? false ) ) return Result.Cancelled ;

          foreach ( var item in viewModel.ChangePlumbingInformationModels ) {
            var oldChangePlumbingInformationModel = changePlumbingInformationStorable.ChangePlumbingInformationModelData.SingleOrDefault( c => c.ConduitId == item.ConduitId ) ;
            if ( oldChangePlumbingInformationModel == null ) {
              var changePlumbingInformationModel = new ChangePlumbingInformationModel( item.ConduitId, item.PlumbingType, item.PlumbingSize, item.NumberOfPlumbing, item.ConstructionClassification, item.ConstructionItems, item.WireCrossSectionalArea ) ;
              changePlumbingInformationStorable.ChangePlumbingInformationModelData.Add( changePlumbingInformationModel ) ;
            }
            else {
              oldChangePlumbingInformationModel.PlumbingType = item.PlumbingType ;
              oldChangePlumbingInformationModel.PlumbingSize = item.PlumbingSize ;
              oldChangePlumbingInformationModel.NumberOfPlumbing = item.NumberOfPlumbing ;
              oldChangePlumbingInformationModel.ConstructionClassification = item.ConstructionClassification ;
              oldChangePlumbingInformationModel.ConstructionItems = item.ConstructionItems ;
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

    private Dictionary<Element, Element> GetConduitOfConnector( Document document, List<Element> connectors )
    {
      var conduitAndConnectorDic = new Dictionary<Element, Element>() ;
      foreach ( var connector in connectors ) {
        
      }
      
      return conduitAndConnectorDic ;
    } 
    
    private ChangePlumbingInformationViewModel CreateChangePlumbingInformationViewModel( Document doc, Dictionary<Element, Element> conduitAndConnectorDic, ChangePlumbingInformationStorable changePlumbingInformationStorable )
    {
      const double percentage = 0.32 ;
      var detailSymbolStorable = doc.GetDetailSymbolStorable() ;
      var csvStorable = doc.GetCsvStorable() ;
      var conduitsModelData = csvStorable.ConduitsModelData ;

      var plumbingTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
      var plumbingTypes = ( from conduitTypeName in plumbingTypeNames select new DetailTableModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
      plumbingTypes.Add( new DetailTableModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;
      
      var hiroiCdModel = csvStorable.HiroiSetCdMasterNormalModelData ;
      var constructionClassificationNames = hiroiCdModel.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
      var constructionClassifications = ( from constructionClassificationName in constructionClassificationNames select new DetailTableModel.ComboboxItemType( constructionClassificationName, constructionClassificationName ) ).ToList() ;
      
      var changePlumbingInformationModels = new List<ChangePlumbingInformationModel>() ;
      var conduitIds = new List<DetailTableModel.ComboboxItemType>() ;
      foreach ( var ( conduit, connector) in conduitAndConnectorDic ) {
        var oldChangePlumbingInformationModel = changePlumbingInformationStorable.ChangePlumbingInformationModelData.SingleOrDefault( c => c.ConduitId == conduit.UniqueId ) ;
        var detailSymbolModel = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( s => s.ConduitId == conduit.UniqueId ) ;
        var plumbingType = oldChangePlumbingInformationModel != null 
          ? oldChangePlumbingInformationModel.PlumbingType 
          : ( detailSymbolModel?.PlumbingType ?? DefaultParentPlumbingType ) ;

        conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var constructionClassification = oldChangePlumbingInformationModel != null 
          ? oldChangePlumbingInformationModel.ConstructionClassification 
          : constructionClassificationNames.First() ;
        var wireCrossSectionalArea = oldChangePlumbingInformationModel?.WireCrossSectionalArea ?? 0 ;
        var ( ceedCode, deviceSymbol) = ElectricalCommandUtil.GetCeedCodeAndDeviceSymbolOfElement( connector ) ;
        if ( oldChangePlumbingInformationModel == null && ! string.IsNullOrEmpty( ceedCode ) ) {
          var hiroiSetCdMasterModel = hiroiCdModel.FirstOrDefault( h => h.SetCode == ceedCode ) ;
          if ( hiroiSetCdMasterModel != null ) {
            constructionClassification = hiroiSetCdMasterModel.ConstructionClassification ;
            var ceedModelNumber = hiroiSetCdMasterModel.LengthParentPartModelNumber ;
            var hiroiSetMasterModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) 
              ? csvStorable.HiroiSetMasterEcoModelData 
              : csvStorable.HiroiSetMasterNormalModelData ;
            var hiroiSetMasterModel = hiroiSetMasterModels.FirstOrDefault( h => h.ParentPartModelNumber == ceedModelNumber ) ;
            if ( hiroiSetMasterModel != null ) {
              var materialCode = hiroiSetMasterModel.MaterialCode1 ;
              var masterModel = csvStorable.HiroiMasterModelData.FirstOrDefault( x => int.Parse( x.Buzaicd ).ToString() == int.Parse( materialCode ).ToString() ) ;
              if ( masterModel != null ) {
                var wireType = masterModel.Type ;
                var wireSize = masterModel.Size1 ;
                var wiresAndCablesModel = csvStorable.WiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && masterModel.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && masterModel.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
                if ( wiresAndCablesModel != null )
                  wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
              }
            }
          }
        }
        
        string plumbingSize ;
        var numberOfPlumbing = oldChangePlumbingInformationModel?.NumberOfPlumbing ?? "1" ;
        if ( plumbingType != NoPlumping ) {
          var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          var plumbingSizesOfPlumbingType = conduitsModels.Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          plumbingSize = oldChangePlumbingInformationModel != null ? oldChangePlumbingInformationModel.PlumbingSize : plumbingSizesOfPlumbingType.First() ;
          if ( oldChangePlumbingInformationModel == null ) {
            var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= wireCrossSectionalArea / percentage ) ?? conduitsModels.Last() ;
            plumbingSize = plumbing == null ? plumbingSizesOfPlumbingType.Last() : plumbing.Size.Replace( "mm", "" ) ;
            if ( plumbing == conduitsModels.Last() ) numberOfPlumbing = ( (int) Math.Ceiling( ( wireCrossSectionalArea / percentage ) / double.Parse( plumbing.InnerCrossSectionalArea ) ) ).ToString() ;
          }
        }
        else {
          plumbingSize = NoPlumbingSize ;
          numberOfPlumbing = string.Empty ;
        }

        var constructionItem = oldChangePlumbingInformationModel != null ? oldChangePlumbingInformationModel.ConstructionItems : DefaultConstructionItems ;
        if ( oldChangePlumbingInformationModel == null ) {
          conduit.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? conduitConstructionItem ) ;
          if ( ! string.IsNullOrEmpty( conduitConstructionItem ) ) constructionItem = conduitConstructionItem ;
        }

        var changePlumbingInformationModel = new ChangePlumbingInformationModel( conduit.UniqueId, plumbingType, plumbingSize, numberOfPlumbing, constructionClassification, constructionItem, wireCrossSectionalArea ) ;
        changePlumbingInformationModels.Add( changePlumbingInformationModel ) ;
        
        conduitIds.Add( new DetailTableModel.ComboboxItemType( string.Join( ":", ceedCode, deviceSymbol ), conduit.UniqueId ) );
      }

      var viewModel = new ChangePlumbingInformationViewModel( conduitsModelData, changePlumbingInformationModels, plumbingTypes, constructionClassifications, conduitIds ) ;
      return viewModel ;
    }
  }
}