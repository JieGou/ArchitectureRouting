using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Documents ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Architecture.Routing.Extensions ;
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
          var element = selection.PickObject( ObjectType.Element, StraightConduitSelectionFilter.Instance, "Select conduit." ) ;
          var conduit = doc.GetElement( element.ElementId ) ;
          if ( conduit == null ) return Result.Cancelled ;

          var viewModel = CreateChangePlumbingInformationViewModel( doc, conduit ) ;
          var view = new ChangePlumbingInformationDialog { DataContext = viewModel } ;
          view.ShowDialog() ;
          if ( ! ( view.DialogResult ?? false ) ) return Result.Cancelled ;

          var changePlumbingInformationStorable = doc.GetChangePlumbingInformationStorable() ;
          var oldChangePlumbingInformationModel = changePlumbingInformationStorable.ChangePlumbingInformationModelData.SingleOrDefault( c => c.ConduitId == conduit.UniqueId ) ;
          if ( oldChangePlumbingInformationModel == null ) {
            var changePlumbingInformationModel = new ChangePlumbingInformationModel( conduit.UniqueId, viewModel.PlumbingType, viewModel.PlumbingSize, viewModel.NumberOfPlumbing, viewModel.ConstructionClassification, viewModel.ConstructionItem ) ;
            changePlumbingInformationStorable.ChangePlumbingInformationModelData.Add( changePlumbingInformationModel ) ;
          }
          else {
            oldChangePlumbingInformationModel.PlumbingType = viewModel.PlumbingType ;
            oldChangePlumbingInformationModel.PlumbingSize = viewModel.PlumbingSize ;
            oldChangePlumbingInformationModel.NumberOfPlumbing = viewModel.NumberOfPlumbing ;
            oldChangePlumbingInformationModel.ConstructionClassification = viewModel.ConstructionClassification ;
            oldChangePlumbingInformationModel.ConstructionItems = viewModel.ConstructionItem ;
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

    private ChangePlumbingInformationViewModel CreateChangePlumbingInformationViewModel( Document doc, Element conduit )
    {
      const double percentage = 0.32 ;
      var detailSymbolStorable = doc.GetDetailSymbolStorable() ;
      var detailTableStorable = doc.GetDetailTableStorable() ;
      var csvStorable = doc.GetCsvStorable() ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var cnsStorable = doc.GetCnsSettingStorable() ;
      
      var detailSymbolModel = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( s => s.ConduitId == conduit.UniqueId ) ;
      var detailTableModel = detailTableStorable.DetailTableModelData.FirstOrDefault( d => d.RouteName == conduit.GetRouteName() ) ;
      
      var plumbingType = detailSymbolModel?.PlumbingType ?? DefaultParentPlumbingType ;
      var plumbingTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
      var plumbingTypes = ( from conduitTypeName in plumbingTypeNames select new DetailTableModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
      plumbingTypes.Add( new DetailTableModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;

      var plumbingSizes = new List<DetailTableModel.ComboboxItemType>() ;
      string plumbingSize ;
      var numberOfPlumbing = 1 ;
      if ( plumbingType != NoPlumping ) {
        var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
        plumbingSizes = ( from plumbingSizeName in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSizeName, plumbingSizeName ) ).ToList() ;
        plumbingSize = plumbingSizesOfPlumbingType.First() ;
        if ( detailTableModel != null ) {
          var size = plumbingSizesOfPlumbingType.FirstOrDefault( s => double.Parse( s ) >= detailTableModel.WireCrossSectionalArea / percentage ) ;
          plumbingSize = string.IsNullOrEmpty( size ) ? plumbingSizesOfPlumbingType.Last() : size ! ;
          if ( string.IsNullOrEmpty( size ) ) numberOfPlumbing = (int) Math.Ceiling( detailTableModel.WireCrossSectionalArea / percentage / double.Parse( plumbingSize ) ) ;
        }
      }
      else {
        plumbingSizes.Add( new DetailTableModel.ComboboxItemType( NoPlumbingSize, NoPlumbingSize ) ) ;
        plumbingSize = NoPlumbingSize ;
      }
      
      var numbersOfPlumbing = new List<DetailTableModel.ComboboxItemType>() ;
      for ( var i = 1 ; i <= 10 ; i++ ) {
        numbersOfPlumbing.Add( new DetailTableModel.ComboboxItemType( i.ToString(), i.ToString() ) ) ;
      }

      conduit.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
      var hiroiCdModel = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? csvStorable.HiroiSetCdMasterEcoModelData : csvStorable.HiroiSetCdMasterNormalModelData ;
      var constructionClassificationNames = hiroiCdModel.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
      var constructionClassifications = ( from constructionClassificationName in constructionClassificationNames select new DetailTableModel.ComboboxItemType( constructionClassificationName, constructionClassificationName ) ).ToList() ;
      var constructionClassification = constructionClassificationNames.First() ;
      List<Element> allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
       var toConnector = ElectricalCommandUtil.GetConnectorOfRoute( doc, allConnectors, conduit.GetRouteName() ! ) ;
       if ( toConnector != null ) {
         var ceedCode = ElectricalCommandUtil.GetCeedSetCodeOfElement( toConnector ) ;
         if ( ! string.IsNullOrEmpty( ceedCode ) ) {
           constructionClassification = hiroiCdModel.FirstOrDefault( h => h.SetCode == ceedCode )?.ConstructionClassification ?? constructionClassificationNames.First() ;
         }
      }

      conduit.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? conduitConstructionItem ) ;
      var constructionItem = string.IsNullOrEmpty( conduitConstructionItem ) ? DefaultConstructionItems : conduitConstructionItem ! ;
      var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
      var constructionItems = constructionItemNames.Any() ? ( from constructionItemName in constructionItemNames select new DetailTableModel.ComboboxItemType( constructionItemName, constructionItemName ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() { new( DefaultConstructionItems, DefaultConstructionItems ) } ;
      
      var viewModel = new ChangePlumbingInformationViewModel( conduitsModelData, plumbingType, plumbingSize, numberOfPlumbing, constructionClassification, constructionItem, plumbingTypes, plumbingSizes, numbersOfPlumbing, constructionClassifications, constructionItems ) ;
      return viewModel ;
    }
  }
}