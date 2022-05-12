using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class SymbolInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      //Get selected objects, check if the first is SymbolInformation or not.
      //1. If that one is SymbolInformation => show dialog 
      //2. If that one isn't SymbolInformation => show message
      //If there isn't selected => create new base on click location => show dialog

      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var symbolInformationStorable = document.GetSymbolInformationStorable() ;
        var symbolInformations = symbolInformationStorable.AllSymbolInformationModelData ;
        var level = uiDocument.ActiveView.GenLevel ;
        var heightOfConnector = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        SymbolInformationModel model ;
        FamilyInstance? symbolInformationInstance = null ;
        XYZ xyz = XYZ.Zero ;

        return document.Transaction( "Electrical.App.Commands.Routing.SymbolInformationCommand", _ =>
        {
          // xyz = uiDocument.Selection.PickPoint( "SymbolInformationの配置場所を選択して下さい。" ) ;
          // var viewPlan = document.GetAllElements<ViewPlan>().FirstOrDefault( x => ! x.IsTemplate && x.ViewType == ViewType.FloorPlan && level.Id == x.GenLevel.Id ) ;
          // if ( null == viewPlan )
          //   return Result.Failed ;
          // GenerateSymbolStar(viewPlan, xyz, 3 );
          if ( uiDocument.Selection.GetElementIds().Count > 0 ) {
            var elementId = uiDocument.Selection.GetElementIds().First() ;
            var symbolInformation = symbolInformations.FirstOrDefault( x => x.Id == elementId.ToString() ) ;
            //pickedObject is SymbolInformationModel
            if ( null != symbolInformation ) {
              model = symbolInformation ;
              var symbolInformationSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ) ?? throw new InvalidOperationException() ;
              symbolInformationInstance = document.GetAllFamilyInstances( symbolInformationSymbols ).FirstOrDefault( x => x.Id.ToString() == model.Id ) ;
            }
            //pickedObject ISN'T SymbolInformationModel
            else {
              var element = document.GetElement( elementId ) ;
              if ( null != element.Location ) {
                xyz = element.Location is LocationPoint pPoint ? pPoint.Point : XYZ.Zero ;
              }
          
              symbolInformationInstance = GenerateSymbolInformation( uiDocument, level, new XYZ( xyz.X, xyz.Y, heightOfConnector ) ) ;
              model = new SymbolInformationModel { Id = symbolInformationInstance.Id.ToString() } ;
              symbolInformations.Add( model ) ;
            }
          }
          else {
            xyz = uiDocument.Selection.PickPoint( "SymbolInformationの配置場所を選択して下さい。" ) ;
            symbolInformationInstance = GenerateSymbolInformation( uiDocument, level, new XYZ( xyz.X, xyz.Y, heightOfConnector ) ) ;
            model = new SymbolInformationModel { Id = symbolInformationInstance.Id.ToString() } ;
            symbolInformations.Add( model ) ;
          }
          
          var viewModel = new SymbolInformationViewModel( document, model ) ;
          var dialog = new SymbolInformationDialog( viewModel ) ;
          var ceedDetailStorable = document.GetCeedDetailStorable() ;
          
          if ( dialog.ShowDialog() == true ) {
            //Save symbol setting
            var symbolHeightParameter = symbolInformationInstance?.LookupParameter( "Symbol Height" ) ;
            symbolHeightParameter?.Set( model.Height.MillimetersToRevitUnits() ) ; 
            symbolInformationStorable.Save() ;
            
            //Save ceedDetails 
            //Delete old data
            ceedDetailStorable.AllCeedDetailModelData.RemoveAll( x => x.ParentId == model.Id ) ;
            //Add new data
            ceedDetailStorable.AllCeedDetailModelData.AddRange( viewModel.CeedDetailList ) ;
            ceedDetailStorable.Save();
          }
          else {
            //document.Delete( new ElementId( int.Parse( viewModel.SymbolInformation!.Id ) ) ) ;
          }

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Cancelled ;
      }
    }

    private FamilyInstance GenerateSymbolInformation( UIDocument uiDocument, Level level, XYZ xyz )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
    }

    /// <summary>
    /// Calculate position of 5 Points, then draw 5 lines
    /// </summary>
    /// <param name="viewPlan"></param>
    /// <param name="centerPoint"></param>
    /// <param name="height"></param>
    private void GenerateSymbolStar(View viewPlan, XYZ centerPoint, double height )
    {
      var lineStyle = GetLineStyle( viewPlan.Document, "SubCategoryForCylindricalShaft", new Color( 0, 250, 0 ), 2 ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      double radiusInside = height.MillimetersToRevitUnits() * 350 / ( 1 + Math.Cos( 36 ) ) ;
      double heightTop = radiusInside * Math.Sin( 18 ) ;
      double heightBottom = radiusInside * Math.Cos( 36 ) ;
      double lengthTop = radiusInside * Math.Cos( 18 ) ;
      double lengthBottom = radiusInside * Math.Sin( 36 ) ;
       
      XYZ p0 = new XYZ( centerPoint.X,  centerPoint.Y + radiusInside, centerPoint.Z ) ;
      XYZ p1 = new XYZ( centerPoint.X + lengthTop,  centerPoint.Y + heightTop, centerPoint.Z ) ;
      XYZ p2 = new XYZ( centerPoint.X + lengthBottom,  centerPoint.Y - heightBottom, centerPoint.Z ) ;
      XYZ p3 = new XYZ( centerPoint.X - lengthBottom,  centerPoint.Y - heightBottom, centerPoint.Z ) ;
      XYZ p4 = new XYZ( centerPoint.X - lengthTop,  centerPoint.Y + heightTop, centerPoint.Z ) ;
       
      CreateDetailLine( viewPlan, lineStyle, Line.CreateBound(p0, p2) ) ; 
      //CreateDetailLine( viewPlan, lineStyle, Line.CreateBound(p0, p3) ) ; 
      //CreateDetailLine( viewPlan, lineStyle, Line.CreateBound(p1, p4) ) ; 
      CreateDetailLine( viewPlan, lineStyle, Line.CreateBound(p1, p3) ) ; 
      //CreateDetailLine( viewPlan, lineStyle, Line.CreateBound(p2, p4) ) ;  
    }
    
    private static void CreateDetailLine( View viewPlan, Element lineStyle, Curve curve )
    {
      var detailLineOne = viewPlan.Document.Create.NewDetailCurve( viewPlan, curve ) ;
      detailLineOne.LineStyle = lineStyle ;
    }
    
    private static Category GetLineStyle( Document document, string subCategoryName, Color color, int lineWeight )
    {
      var categories = document.Settings.Categories ;
      var category = document.Settings.Categories.get_Item( BuiltInCategory.OST_GenericAnnotation ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        subCategory.LineColor = color ;
        subCategory.SetLineWeight( lineWeight, GraphicsStyleType.Projection ) ;
      }
      else {
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;
      }

      return subCategory ;
    }
  }
}