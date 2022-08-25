using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.UI ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowCeedModelsCommandBase : IExternalCommand
  {
    public const string DeviceSymbolTextNoteTypeName = "Left_2.5mm_DeviceSymbolText" ;

    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    protected abstract string FullClass { get ; }
    protected abstract string TabName { get ; }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      const string switch2DSymbol = "2Dシンボル切り替え" ;
      const string symbolMagnification = "シンボル倍率" ;
      const string grade3 = "グレード3" ;
      
      var uiDocument = commandData.Application.ActiveUIDocument ;
      if ( uiDocument.ActiveView is not ViewPlan ) {
        TaskDialog.Show( "Arent", "This view is not the view plan!" ) ;
        return Result.Cancelled ;
      }
      
      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( uiDocument.Document ) ;
      var defaultConstructionItem = uiDocument.Document.GetDefaultConstructionItem() ;
      
      var viewModel = new CeedViewModel( uiDocument.Document ) ;
      var dialog = new CeedModelDialog( viewModel ) ;
      
      dialog.ShowDialog() ;
      if ( ! ( dialog.DialogResult ?? false ) ) 
        return Result.Cancelled ;
      
      if ( string.IsNullOrEmpty( viewModel.SelectedDeviceSymbol ) ) 
        return Result.Succeeded ;

      var result = uiDocument.Document.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
      {
        var level = uiDocument.Document.ActiveView.GenLevel ;
        var heightOfConnector = uiDocument.Document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        var connector = GenerateConnector( uiDocument, 0, 0, heightOfConnector, level, viewModel.SelectedFloorPlanType ?? string.Empty ) ;

        var ( placePoint, direction ) = PickPoint( uiDocument, connector ) ;
        if ( null == placePoint )
          return Result.Cancelled ;

        var condition = "屋外" ; // デフォルトの条件
        var isValidPoint = IsValidPoint( uiDocument, placePoint, viewModel, ref condition ) ;
        if ( ! isValidPoint )
          return Result.Failed ;
        
        if ( !viewModel.OriginCeedModels.Any(cmd => cmd.Condition == condition && cmd.GeneralDisplayDeviceSymbol == viewModel.SelectedDeviceSymbol) ) {
          TaskDialog.Show( "Arent", $"We can not find any ceedmodel \"{viewModel.SelectedDeviceSymbol}\" match with this room \"{condition}\"。" ) ;
          return Result.Cancelled ;
        }

        ElementTransformUtils.MoveElement( uiDocument.Document, connector.Id, placePoint - new XYZ( 0, 0, heightOfConnector ) ) ;
        if ( null != direction ) {
          var line = Line.CreateBound( placePoint, Transform.CreateTranslation( XYZ.BasisZ ).OfPoint( placePoint ) ) ;
          ElementTransformUtils.RotateElement(uiDocument.Document, connector.Id, line, XYZ.BasisY.AngleTo(direction));
        }
        
        var ceedCode = string.Join( ":", viewModel.SelectedCeedCode, viewModel.SelectedDeviceSymbol, viewModel.SelectedModelNum ) ;
        connector.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;
        connector.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, defaultConstructionItem ) ;
        connector.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
          
        var deviceSymbol = viewModel.SelectedDeviceSymbol ?? string.Empty ;
        var text = StringWidthUtils.IsHalfWidth( deviceSymbol ) ? StringWidthUtils.ToFullWidth( deviceSymbol ) : deviceSymbol ;
        connector.SetProperty(ElectricalRoutingElementParameter.SymbolContent, text);

        var deviceSymbolTagType = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolContentTag ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        IndependentTag.Create( uiDocument.Document, deviceSymbolTagType.Id, uiDocument.ActiveView.Id, new Reference( connector ), false, TagOrientation.Horizontal, new XYZ(placePoint.X, placePoint.Y + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * uiDocument.ActiveView.Scale, placePoint.Z) ) ;
        
        if ( connector.HasParameter( switch2DSymbol ) ) 
          connector.SetProperty( switch2DSymbol, true ) ;
        
        if ( connector.HasParameter( symbolMagnification ) ) 
          connector.SetProperty( symbolMagnification, defaultSymbolMagnification ) ;
        
        if ( connector.HasParameter( grade3 ) ) 
          connector.SetProperty( grade3, uiDocument.Document.GetDefaultSettingStorable().GradeSettingData.GradeMode == 3 );

        return Result.Succeeded ;
      } ) ;

      return result ;
    }

    private static bool IsValidPoint(UIDocument uiDocument, XYZ point, CeedViewModel viewModel, ref string? condition)
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.Room ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var filter = new FamilyInstanceFilter( uiDocument.Document, symbol.Id ) ;
      var rooms = new FilteredElementCollector( uiDocument.Document ).WherePasses( filter ).OfType<FamilyInstance>().Where(x =>
      {
        var bb = x.get_BoundingBox( null ) ;
        var ol = new Outline( bb.Min, bb.Max ) ;
        return ol.Contains( point, GeometryHelper.Tolerance ) ;
      }).ToList() ;

      switch ( rooms.Count ) {
        case 0 :
          if ( viewModel.IsShowCondition ) {
            condition = viewModel.SelectedCondition ;
          }
          else {
            TaskDialog.Show( "Arent", "部屋の外で電気シンボルを作成することができません。部屋の中の場所を指定してください！" ) ;
            return false ;
          }
          break;
        case > 1 when CreateRoomCommandBase.TryGetConditions( uiDocument.Document, out var conditions ) && conditions.Any() :
          var vm = new ArentRoomViewModel { Conditions = conditions } ;
          var view = new ArentRoomView { DataContext = vm } ;
          view.ShowDialog() ;
          if ( ! vm.IsCreate )
            return false ;

          if ( viewModel.IsShowCondition && viewModel.SelectedCondition != vm.SelectedCondition ) {
            TaskDialog.Show( "Arent", "指定した条件が部屋の条件と一致していないので、再度ご確認ください。" ) ;
            return false ;
          }
          
          condition = vm.SelectedCondition ;
          break ;
        case > 1 :
          TaskDialog.Show( "Arent", "指定された条件が見つかりませんでした。" ) ;
          return false ;
        default :
        {
          if ( rooms.First().TryGetProperty( ElectricalRoutingElementParameter.RoomCondition, out string? value ) && !string.IsNullOrEmpty(value)) {
            if ( viewModel.IsShowCondition && viewModel.SelectedCondition != value ) {
              TaskDialog.Show( "Arent", "指定した条件が部屋の条件と一致していないので、再度ご確認ください。" ) ;
              return false ;
            }
            condition = value ;
          }
          break ;
        }
      }

      return true ;
    }

    private (XYZ? PlacePoint, XYZ? Direction) PickPoint( UIDocument uiDocument, Element symbolInstance )
    {
      var dlg = new ModelessOkCancelDialog() ;
      dlg.AlignToView(uiDocument.GetActiveUIView());
      dlg.Show();
      dlg.FocusRevit();
      
      TabPlaceExternal? tabPlaceExternal = null ;
      XYZ? placePoint = null ;
      XYZ? direction = null ;
      try {
        if ( ! symbolInstance.HasParameter( "W" ) ) {
          dlg.Close();
          return ( placePoint, direction ) ;
        }
        
        tabPlaceExternal = new TabPlaceExternal( uiDocument.Application, symbolInstance.GetPropertyDouble("W") * 0.5, dlg ) ;
        while ( true ) {
          if ( null == tabPlaceExternal.FirstPoint ) {
            var (x, y, z) = uiDocument.Selection.PickPoint() ;
            tabPlaceExternal.FirstPoint = new XYZ( x, y, z ) ;
          }
          else if ( null == tabPlaceExternal.SecondPoint ) {
            var secondPoint = uiDocument.Selection.PickPoint() ;
            if ( tabPlaceExternal.FirstPoint.DistanceTo( secondPoint ) <= uiDocument.Application.Application.ShortCurveTolerance )
              continue;
            
            tabPlaceExternal.SecondPoint = secondPoint ;
            uiDocument.RefreshActiveView();
          }
          else {
            uiDocument.Selection.PickObject(ObjectType.Nothing) ;
          }
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        switch ( dlg.IsCancel ) {
          case false when null != tabPlaceExternal :
          {
            placePoint = tabPlaceExternal.PlacePoint ;
            if(null != tabPlaceExternal.SecondPoint && null != tabPlaceExternal.FirstPoint)
              direction = ( tabPlaceExternal.SecondPoint - tabPlaceExternal.FirstPoint ).Normalize() ;
            break ;
          }
          case true :
          {
            uiDocument.Document.Delete( symbolInstance.Id ) ;
            break ;
          }
        }
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "PECC2", exception.Message ) ;
      }
      finally {
        dlg.Close();
        tabPlaceExternal?.Dispose();
      }

      return ( placePoint, direction ) ;
    }

    private FamilyInstance GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, string floorPlanType )
    {
      FamilyInstance instance;
      if ( ! string.IsNullOrEmpty( floorPlanType ) ) {
        var connectorOneSideFamilyTypeNames = ( (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ).Select( f => f.GetFieldName() ).ToHashSet() ;
        if ( connectorOneSideFamilyTypeNames.Contains( floorPlanType ) ) {
          var connectorOneSideFamilyType = GetConnectorFamilyType( floorPlanType ) ;
          var symbol = uiDocument.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
          instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
          SetIsEcoMode( uiDocument, instance );
          return instance ;
        }

        if ( new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == floorPlanType ) is Family family ) {
          foreach ( var familySymbolId in family.GetFamilySymbolIds() ) {
            var symbol = uiDocument.Document.GetElementById<FamilySymbol>( familySymbolId ) ?? throw new InvalidOperationException() ;
            instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
            SetIsEcoMode( uiDocument, instance );
            return instance ;
          }
        }
      }

      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      instance = routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      SetIsEcoMode( uiDocument, instance );
      return instance ;
    }

    private static ConnectorOneSideFamilyType GetConnectorFamilyType( string floorPlanType )
    {
      var connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      if ( string.IsNullOrEmpty( floorPlanType ) ) return connectorOneSideFamilyType ;
      foreach ( var item in (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ) {
        if ( floorPlanType == item.GetFieldName() ) connectorOneSideFamilyType = item ;
      }

      return connectorOneSideFamilyType ;
    }

    private static void SetIsEcoMode(UIDocument uiDocument, FamilyInstance instance)
    { 
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, uiDocument.Document.GetDefaultSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
    }
  }
}