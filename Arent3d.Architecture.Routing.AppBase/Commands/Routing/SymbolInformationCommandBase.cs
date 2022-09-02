using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MoreLinq ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class SymbolInformationCommandBase : IExternalCommand
  {
    private FamilySymbol? _symbolStarType ;
    private FamilySymbol? _symbolCircleType ;
    
    private const int LineWeight = 4 ;
    private const string ParameterName = "Symbol Height" ;
    
    private static double Offset => 1d.MillimetersToRevitUnits() ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        if ( uiDocument.ActiveView is not ViewPlan viewPlan )
          return Result.Cancelled ;
        
        var storable = uiDocument.Document.GetSymbolInformationStorable() ;
        var symbolStorages = storable.AllSymbolInformationModelData ;
        var heightSymbol = uiDocument.Document.GetHeightSettingStorable()[ viewPlan.GenLevel ].HeightOfConnectors.MillimetersToRevitUnits() ;
        _symbolStarType = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        _symbolCircleType = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolCircle ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        
        SymbolInformationModel? symbolModel = null ;
        FamilyInstance? symbolInstance = null ;
        IndependentTag? tag = null ;
        XYZ? point = null ;

        if ( uiDocument.Selection.GetElementIds().Count > 0 ) {
          symbolInstance = uiDocument.Selection.GetElementIds().Select( x => uiDocument.Document.GetElement( x ) ).OfType<FamilyInstance>()
            .FirstOrDefault(x => x.Name == _symbolStarType.Name || x.Name == _symbolCircleType.Name) ;

          if ( symbolInstance?.Location is LocationPoint locationPoint) {
            point = locationPoint.Point ;
            symbolModel = symbolStorages.SingleOrDefault( x => x.SymbolUniqueId == symbolInstance.UniqueId ) ;
            if(!string.IsNullOrEmpty(symbolModel?.TagUniqueId))
              tag = uiDocument.Document.GetElement(symbolModel!.TagUniqueId) as IndependentTag;
          }
        }
        else {
          try {
            point = uiDocument.Selection.PickPoint( "SymbolInformationの配置場所を選択して下さい。" ) ;
          }
          catch ( OperationCanceledException ) {
            return Result.Cancelled ;
          }
        }

        var viewModel = new SymbolInformationViewModel( uiDocument.Document, symbolModel ) ;
        var dialog = new SymbolInformationDialog( viewModel ) ;

        if ( dialog.ShowDialog() != true || null == point ) 
          return Result.Failed ;
        
        using Transaction transaction = new(uiDocument.Document, "Create Symbol Information") ;
        transaction.Start() ;

        ( symbolInstance, tag ) = CreateOrEditSymbolInstance( uiDocument.Document, symbolInstance, tag, new XYZ(point.X, point.Y, heightSymbol), viewModel ) ;
        symbolModel = SaveData( storable, symbolInstance, tag, viewModel ) ;

        var ceedDetailStorable = uiDocument.Document.GetCeedDetailStorable() ;
        ceedDetailStorable.AllCeedDetailModelData.RemoveAll( x => x.ParentId == symbolModel.SymbolUniqueId ) ;
        viewModel.CeedDetailList.ForEach(x => x.ParentId = symbolModel.SymbolUniqueId);
        ceedDetailStorable.AllCeedDetailModelData.AddRange( viewModel.CeedDetailList ) ;
        ceedDetailStorable.Save() ;

        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Cancelled ;
      }
    }

    private static SymbolInformationModel SaveData(SymbolInformationStorable storable, FamilyInstance symbolInstance, IndependentTag tag, SymbolInformationViewModel viewModel)
    {
      var symbolModel = storable.AllSymbolInformationModelData.SingleOrDefault( x => x.SymbolUniqueId == symbolInstance.UniqueId ) ;
      if ( null == symbolModel ) {
        symbolModel = new SymbolInformationModel( symbolInstance.UniqueId, tag.UniqueId, $"{viewModel.SelectedSymbolKind}", $"{viewModel.SelectedSymbolCoordinate}", viewModel.SymbolInformation.Height, viewModel.SymbolInformation.Percent,
          viewModel.SymbolInformation.Color, viewModel.SymbolInformation.IsShowText, viewModel.SymbolInformation.Description, viewModel.SymbolInformation.CharacterHeight, symbolInstance.Document.ActiveView.GenLevel.Name ) ;
        storable.AllSymbolInformationModelData.Add(symbolModel);
      }
      else {
        symbolModel.SymbolUniqueId = symbolInstance.UniqueId ;
        symbolModel.TagUniqueId = tag.UniqueId ;
        symbolModel.SymbolKind =  $"{viewModel.SelectedSymbolKind}" ;
        symbolModel.SymbolCoordinate = $"{viewModel.SelectedSymbolCoordinate}" ;
        symbolModel.Height = viewModel.SymbolInformation.Height ;
        symbolModel.Percent = viewModel.SymbolInformation.Percent ;
        symbolModel.Color = viewModel.SymbolInformation.Color ;
        symbolModel.Floor = symbolInstance.Document.ActiveView.GenLevel.Name ;
        symbolModel.Description = viewModel.SymbolInformation.Description ;
        symbolModel.CharacterHeight = viewModel.SymbolInformation.CharacterHeight ;
        symbolModel.IsShowText = viewModel.SymbolInformation.IsShowText ;
      }
      storable.Save();

      return symbolModel ;
    }

    private (FamilyInstance SymbolInstance, IndependentTag Tag) CreateOrEditSymbolInstance(Document document, FamilyInstance? symbolInstance, IndependentTag? tag, XYZ point,  SymbolInformationViewModel viewModel)
    {
      var familySymbol = GetFamilySymbol( viewModel.SelectedSymbolKind ) ;
      if ( null == symbolInstance ) {
        symbolInstance = familySymbol.Instantiate( point, document.ActiveView.GenLevel, StructuralType.NonStructural ) ;
        document.Regenerate();
      }

      if ( symbolInstance.Symbol.FamilyName != familySymbol.Name ) {
        symbolInstance.Symbol = familySymbol ;
        document.Regenerate();
      }

      if ( symbolInstance.LookupParameter( ParameterName ) is { } parameter && Math.Abs( parameter.AsDouble() - viewModel.SymbolInformation.Height.MillimetersToRevitUnits() / 2 ) > GeometryHelper.Tolerance ) {
        parameter.Set( viewModel.SymbolInformation.Height.MillimetersToRevitUnits() / 2 ) ;
        document.Regenerate();
      }

      var overrideGraphic = document.ActiveView.GetElementOverrides( symbolInstance.Id ) ;
      var color = SymbolColor.DictSymbolColor[ viewModel.SymbolInformation.Color ] ;
      if ( !overrideGraphic.ProjectionLineColor.IsValid || color.Red != overrideGraphic.ProjectionLineColor.Red || color.Blue != overrideGraphic.ProjectionLineColor.Blue || color.Green != overrideGraphic.ProjectionLineColor.Green ) {
        overrideGraphic.SetProjectionLineColor( color ) ;
        document.ActiveView.SetElementOverrides(symbolInstance.Id, overrideGraphic);
      }

      if ( overrideGraphic.ProjectionLineWeight != LineWeight ) {
        overrideGraphic.SetProjectionLineWeight( LineWeight ) ;
        document.ActiveView.SetElementOverrides(symbolInstance.Id, overrideGraphic);
      }

      if(symbolInstance.HasParameter( ElectricalRoutingElementParameter.Text ))
        symbolInstance.SetProperty(ElectricalRoutingElementParameter.Text, viewModel.SymbolInformation.Description);
      
      tag = CreateOrEditTag( symbolInstance, tag, viewModel ) ;
      return (symbolInstance, tag) ;
    }

    private static IndependentTag CreateOrEditTag( FamilyInstance symbolInstance, IndependentTag? tag, SymbolInformationViewModel viewModel )
    {
      if ( null == tag ) {
        var tagType = symbolInstance.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolInformationTag ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        var symbolLocation = ( (LocationPoint) symbolInstance.Location ).Point ;
        tag = IndependentTag.Create( symbolInstance.Document, tagType.Id, symbolInstance.Document.ActiveView.Id, new Reference( symbolInstance ), false, TagOrientation.Horizontal, symbolLocation ) ;
        symbolInstance.Document.Regenerate();
      }
      
      EditTag( tag, viewModel ) ;
      if(viewModel.SymbolInformation.IsShowText)
        MoveTag( symbolInstance, tag, viewModel.SelectedSymbolCoordinate ) ;

      return tag ;
    }

    private static void EditTag(IndependentTag tag, SymbolInformationViewModel viewModel )
    {
      var familySymbolTag = tag.GetValidTypes().Select( x => tag.Document.GetElement( x ) ).OfType<FamilySymbol>().SingleOrDefault( x => x.Name == $"{viewModel.SymbolInformation.CharacterHeight}mm" ) ;
      if ( null != familySymbolTag && tag.Document.GetElement(tag.GetTypeId()).Name != familySymbolTag.Name) {
        tag.ChangeTypeId( familySymbolTag.Id ) ;
        tag.Document.Regenerate();
      }
      
      var activeView = tag.Document.ActiveView ;
      var isHidden = tag.IsHidden( activeView ) ;
      if ( ! isHidden == viewModel.SymbolInformation.IsShowText ) 
        return ;
      
      if(viewModel.SymbolInformation.IsShowText)
        activeView.UnhideElements(new List<ElementId>{ tag.Id });
      else
        activeView.HideElements(new List<ElementId>{ tag.Id });
      
      tag.Document.Regenerate();
    }

    private static void MoveTag( FamilyInstance symbolInstance, IndependentTag tag, SymbolCoordinate symbolCoordinate )
    {
      var symbolBox = symbolInstance.get_BoundingBox( null ) ;
      var symbolLocation = ( (LocationPoint) symbolInstance.Location ).Point ;
      var symbolWidth = symbolBox.Max.X - symbolBox.Min.X ;
      var symbolHeight = symbolBox.Max.Y - symbolBox.Min.Y ;
      
      var tagBox = tag.get_BoundingBox( symbolInstance.Document.ActiveView ) ;
      var tagLocation = tag.TagHeadPosition ;
      var tagWidth = tagBox.Max.X - tagBox.Min.X ;
      var tagHeight = tagBox.Max.Y - tagBox.Min.Y ;

      var toPoint = XYZ.Zero ;
      var scale = symbolInstance.Document.ActiveView.Scale ;
      switch ( symbolCoordinate ) {
        case SymbolCoordinate.上 :
        {
          var transform = Transform.CreateTranslation( XYZ.BasisY * ( 0.5 * symbolHeight + Offset * scale + 0.5 * tagHeight ) ) ;
          toPoint = transform.OfPoint( symbolLocation ) ;
          break;
        }
        case SymbolCoordinate.左:
        {
          var transform = Transform.CreateTranslation( -XYZ.BasisX * ( 0.5 * symbolWidth + Offset * scale + 0.5 * tagWidth ) ) ;
          toPoint = transform.OfPoint( symbolLocation ) ;
          break;
        }
        case SymbolCoordinate.中心:
        {
          toPoint = symbolLocation ;
          break;
        }
        case SymbolCoordinate.右:
        {
          var transform = Transform.CreateTranslation( XYZ.BasisX * ( 0.5 * symbolWidth + Offset * scale + 0.5 * tagWidth ) ) ;
          toPoint = transform.OfPoint( symbolLocation ) ;
          break;
        }
        case SymbolCoordinate.下:
        {
          var transform = Transform.CreateTranslation( -XYZ.BasisY * ( 0.5 * symbolHeight + Offset * scale + 0.5 * tagHeight ) ) ;
          toPoint = transform.OfPoint( symbolLocation ) ;
          break;
        }
      }
      
      ElementTransformUtils.MoveElement(symbolInstance.Document, tag.Id, toPoint - tagLocation );
    }
    
    private FamilySymbol GetFamilySymbol( SymbolKindEnum symbolKind )
    {
      return symbolKind switch
      {
        SymbolKindEnum.星 => _symbolStarType!,
        _ => _symbolCircleType!
      } ;
    }

  }
}