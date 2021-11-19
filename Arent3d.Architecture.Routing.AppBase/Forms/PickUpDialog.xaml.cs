using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpDialog : Window
  {
    private readonly Document _document ;
    private List<CeedModel> _ceeDModels ;
    private List<Element> _allElements ;
    public PickUpDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _ceeDModels = new List<CeedModel>() ;
      _allElements = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).Distinct().ToList() ;
      var ceeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable != null ) _ceeDModels = ceeDStorable.CeedModelData ;
    }

    private void Load_AllData( object sender, RoutedEventArgs e )
    {
      var contentDisplayDialog = new ContentDisplayDialog( ) ;
      contentDisplayDialog.ShowDialog() ;
    }
    
    private void Load_AirConditioningPipingData( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }
    
    private void Load_SatellitePlumbingData( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }
    
    private void Load_ConduitData( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }
    
    private void Load_ElectricityData( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }
    
    private void Load_OtherData( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private List<PickUpModel> GetPickUpData()
    {
      List<PickUpModel> pickUpModels = new List<PickUpModel>() ;
      var number = 1 ;
      foreach ( var element in _allElements ) {
        var item = _document.GetAllElements<Level>().FirstOrDefault( level => level.Id == element.LevelId )?.Name ;
        var floor = string.Empty ;
        var constructionItems = string.Empty ;
        var facility = string.Empty ;
        var productName = string.Empty ;
        var use = string.Empty ;
        var construction = string.Empty ;
        var modelNumber = string.Empty ;
        var specification = string.Empty ;
        var specification2 = string.Empty ;
        var ceeDSetCode = GetCeeDSetCodeOfElement( element ) ;
        if ( _ceeDModels.Any() && ! string.IsNullOrEmpty( ceeDSetCode ) ) {
          var ceeDModel = _ceeDModels.FirstOrDefault( x => x.CeeDSetCode == ceeDSetCode ) ;
          if ( ceeDModel != null ) {
            modelNumber = ceeDModel.CeeDModelNumber ;
            specification = ceeDModel.CeeDSetCode ;
            specification2 = ceeDModel.CeeDSetCode ;
          }
        }
        
        var size = string.Empty ;
        double quantity = 0 ;
        var tani = string.Empty ;
        PickUpModel pickUpModel = new PickUpModel( number, item, floor, constructionItems, facility, productName, use, construction, modelNumber, specification, specification2, size, quantity, tani ) ;
        pickUpModels.Add( pickUpModel );
        number++ ;
      }
      return pickUpModels ;
    }

    private string GetCeeDSetCodeOfElement( Element element )
    {
      var elementId = 0 ;
      switch ( element ) {
        case ConnectorElement :
          elementId = element.GroupId.IntegerValue ;
          break ;
        case Conduit conduit :
        {
          var toConnector = conduit.GetConnectors().Last() ;
          var connectorElement = _document.GetAllElements<Element>().FirstOrDefault( e => e.Id.IntegerValue == toConnector.Id ) ;
          elementId = connectorElement!.GroupId.IntegerValue ;
          break ;
        }
      }
      var textId = _document.GetAllElements<Group>().FirstOrDefault( g => g.Id.IntegerValue == elementId )?.GroupId ;
      var ceeDSetCode = _document.GetAllElements<TextNote>().FirstOrDefault( t => t.Id == textId )?.Text ;
      return ceeDSetCode ?? string.Empty ;
    }
  }
}