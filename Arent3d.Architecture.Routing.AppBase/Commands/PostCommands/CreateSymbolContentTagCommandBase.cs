using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.AppBase.Updater ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MoreLinq.Extensions ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public class SymbolContentTagCommandParameter
  {
    public CeedViewModel CeedViewModel { get ; }

    public SymbolContentTagCommandParameter( CeedViewModel ceedViewModel )
    {
      CeedViewModel = ceedViewModel ;
    }
  }

  public class CreateSymbolContentTagCommandBase : RoutingExternalAppCommandBaseWithParam<SymbolContentTagCommandParameter>
  {
    protected override string GetTransactionName() => "TransactionName.Commands.PostCommands.CreateSymbolContentTagCommand".GetAppStringByKeyOrDefault( "Create connector" ) ;

    [DllImport( "User32.dll", EntryPoint = "SendMessage" )]
    public static extern int SendMessage( IntPtr hWnd, int Msg, int wParam, int lParam ) ;

    protected override ExecutionResult Execute( SymbolContentTagCommandParameter param, Document document, TransactionWrapper transaction )
    {
      try {
        if ( document.ActiveView is not ViewPlan ) {
          TaskDialog.Show( "Arent", "This view is not the view plan!" ) ;
          return ExecutionResult.Cancelled ;
        }

        if ( param.CeedViewModel.SelectedCeedCode == null )
          return ExecutionResult.Cancelled ;

        var level = document.ActiveView.GenLevel ;
        var heightOfConnector = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        var connector = GenerateConnector( 0, 0, heightOfConnector, level, param.CeedViewModel.SelectedFloorPlanType ?? string.Empty ) ;
        SetParameter( connector, param ) ;
        document.Regenerate() ;

        var uiDocument = new UIDocument( document ) ;
        
        FocusToActiveView( uiDocument ) ;
        var (placePoint, direction, firstPoint) = PickPoint( uiDocument, connector ) ;
        if ( null == placePoint )
          return ExecutionResult.Cancelled ;
        
        var condition = "屋外" ; // デフォルトの条件
        var isValidPoint = IsValidPoint( uiDocument, placePoint, param.CeedViewModel, ref condition ) ;
        if ( ! isValidPoint )
          return ExecutionResult.Cancelled ;
        
        if ( ! param.CeedViewModel.OriginCeedModels.Any( cmd => cmd.Condition == condition && cmd.GeneralDisplayDeviceSymbol == param.CeedViewModel.SelectedDeviceSymbol ) ) {
          TaskDialog.Show( "Arent", $"We can not find any ceedmodel \"{param.CeedViewModel.SelectedDeviceSymbol}\" match with this room \"{condition}\"。" ) ;
          return ExecutionResult.Cancelled ;
        }
        
        ElementTransformUtils.MoveElement( document, connector.Id, new XYZ( placePoint.X, placePoint.Y, heightOfConnector ) - new XYZ( 0, 0, heightOfConnector ) ) ;
        if ( null != direction && null != firstPoint ) {
          var line = Line.CreateBound( placePoint, Transform.CreateTranslation( XYZ.BasisZ ).OfPoint( placePoint ) ) ;
          ElementTransformUtils.RotateElement( document, connector.Id, line, TabPlaceExternal.GetAngle( direction, firstPoint, placePoint ) ) ;
        }
        
        var deviceSymbol = param.CeedViewModel.SelectedDeviceSymbol ?? string.Empty ;
        if ( ! string.IsNullOrEmpty( deviceSymbol ) ) {
          var text = StringWidthUtils.IsHalfWidth( deviceSymbol ) ? StringWidthUtils.ToFullWidth( deviceSymbol ) : deviceSymbol ;
          connector.SetProperty( ElectricalRoutingElementParameter.SymbolContent, text ) ;
        
          var symbolContentTag = connector.Category.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures ? ElectricalRoutingFamilyType.ElectricalFixtureContentTag : ElectricalRoutingFamilyType.ElectricalEquipmentContentTag ;
          var deviceSymbolTagType = document.GetFamilySymbols( symbolContentTag ).FirstOrDefault( x => x.LookupParameter( "Is Hide Quantity" ).AsInteger() == 1 ) ?? throw new InvalidOperationException() ;
          IndependentTag.Create( document, deviceSymbolTagType.Id, document.ActiveView.Id, new Reference( connector ), false, TagOrientation.Horizontal,
            new XYZ( placePoint.X, placePoint.Y + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * document.ActiveView.Scale, placePoint.Z ) ) ;
        
          var connectorUpdater = new ConnectorUpdater( document.Application.ActiveAddInId ) ;
          UpdaterRegistry.RegisterUpdater( connectorUpdater, document ) ;
          var sharedParameter = SharedParameterElement.Lookup( document, ElectricalRoutingElementParameter.Quantity.GetParameterGuid() ) ;
          UpdaterRegistry.AddTrigger( connectorUpdater.GetUpdaterId(), document, new List<ElementId> { connector.Id }, Element.GetChangeTypeParameter( sharedParameter.Id ) ) ;
        }
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
        return ExecutionResult.Failed ;
      }

      return ExecutionResult.Succeeded ;
    }

    private static void SetParameter( FamilyInstance connector, SymbolContentTagCommandParameter param )
    {
      var ceedCode = string.Join( ":", param.CeedViewModel.SelectedCeedCode, param.CeedViewModel.SelectedDeviceSymbol, param.CeedViewModel.SelectedModelNum ) ;
      connector.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;

      var defaultConstructionItem = connector.Document.GetDefaultConstructionItem() ;
      connector.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, defaultConstructionItem ) ;

      connector.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
      connector.SetProperty( ElectricalRoutingElementParameter.Quantity, 1 ) ;

      const string switch2DSymbol = "2Dシンボル切り替え" ;
      if ( connector.HasParameter( switch2DSymbol ) )
        connector.SetProperty( switch2DSymbol, true ) ;

      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( connector.Document ) ;
      const string symbolMagnification = "シンボル倍率" ;
      if ( connector.HasParameter( symbolMagnification ) )
        connector.SetProperty( symbolMagnification, defaultSymbolMagnification ) ;
      
      if ( ! connector.HasParameter( DefaultSettingCommandBase.Grade3FieldName ) ) 
        return ;
      
      var dataStorage = connector.Document.FindOrCreateDataStorage<DisplaySettingModel>( false ) ;
      var displaySettingStorageService = new StorageService<DataStorage, DisplaySettingModel>( dataStorage ) ;
      var isGrade3 = displaySettingStorageService.Data.GradeOption == displaySettingStorageService.Data.GradeOptions[ 0 ] ;
      if ( ! displaySettingStorageService.Data.IsSaved )
        isGrade3 = DefaultSettingCommandBase.GradeFrom3To7Collection.Contains( connector.Document.GetDefaultSettingStorable().GradeSettingData.GradeMode) ;
      connector.SetProperty( DefaultSettingCommandBase.Grade3FieldName, isGrade3 ) ;
    }

    private static void FocusToActiveView( UIDocument uiDocument )
    {
      const int WM_MDIGETACTIVE = 0x0229 ;
      const int WM_MDIACTIVATE = 0x0222 ;
      var childHwnd = SendMessage( uiDocument.Application.MainWindowHandle, WM_MDIGETACTIVE, 0, 0 ) ;
      SendMessage( uiDocument.Application.MainWindowHandle, WM_MDIACTIVATE, childHwnd, 0 ) ;
    }

    private static FamilyInstance GenerateConnector( double originX, double originY, double originZ, Level level, string floorPlanType )
    {
      FamilyInstance instance ;
      if ( ! string.IsNullOrEmpty( floorPlanType ) ) {
        var connectorOneSideFamilyTypeNames = ToHashSetExtension.ToHashSet( ( (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ).Select( f => f.GetFieldName() ) ) ;
        if ( connectorOneSideFamilyTypeNames.Contains( floorPlanType ) ) {
          var connectorOneSideFamilyType = GetConnectorFamilyType( floorPlanType ) ;
          var symbol = level.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( level.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ConnectorOneSide ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
          instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
          SetIsEcoMode( instance ) ;
          return instance ;
        }

        if ( new FilteredElementCollector( level.Document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == floorPlanType ) is Family family ) {
          foreach ( var familySymbolId in family.GetFamilySymbolIds() ) {
            var symbol = level.Document.GetElementById<FamilySymbol>( familySymbolId ) ?? throw new InvalidOperationException() ;
            instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
            SetIsEcoMode( instance ) ;
            return instance ;
          }
        }
      }

      var routingSymbol = level.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ConnectorOneSide ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      instance = routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      SetIsEcoMode( instance ) ;
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

    private static void SetIsEcoMode( FamilyInstance instance )
    {
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, instance.Document.GetDefaultSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
    }

    private static bool IsValidPoint( UIDocument uiDocument, XYZ point, CeedViewModel ceedViewModel, ref string? condition )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.Room ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var filter = new FamilyInstanceFilter( uiDocument.Document, symbol.Id ) ;
      var rooms = new FilteredElementCollector( uiDocument.Document ).WherePasses( filter ).OfType<FamilyInstance>().Where( x =>
      {
        var bb = x.get_BoundingBox( null ) ;
        var ol = new Outline( bb.Min, bb.Max ) ;
        return ol.Contains( point, GeometryHelper.Tolerance ) ;
      } ).ToList() ;

      switch ( rooms.Count ) {
        case 0 :
          if ( ceedViewModel.IsShowCondition ) {
            condition = ceedViewModel.SelectedCondition ;
          }
          else {
            TaskDialog.Show( "Arent", "部屋の外で電気シンボルを作成することができません。部屋の中の場所を指定してください！" ) ;
            return false ;
          }

          break ;
        case > 1 when CreateRoomCommandBase.TryGetConditions( uiDocument.Document, out var conditions ) && conditions.Any() :
          var vm = new ArentRoomViewModel { Conditions = conditions } ;
          var view = new ArentRoomView { DataContext = vm } ;
          view.ShowDialog() ;
          if ( ! vm.IsCreate )
            return false ;

          if ( ceedViewModel.IsShowCondition && ceedViewModel.SelectedCondition != vm.SelectedCondition ) {
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
          if ( rooms.First().TryGetProperty( ElectricalRoutingElementParameter.RoomCondition, out string? value ) && ! string.IsNullOrEmpty( value ) ) {
            if ( ceedViewModel.IsShowCondition && ceedViewModel.SelectedCondition != value ) {
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

    private static (XYZ? PlacePoint, XYZ? Direction, XYZ? FirstPoint) PickPoint( UIDocument uiDocument, FamilyInstance symbolInstance )
    {
      var dlg = new ModelessOkCancelDialog() ;
      dlg.AlignToView( uiDocument.GetActiveUIView() ) ;
      dlg.Show() ;
      dlg.FocusRevit() ;

      var curves = GetCurvesAtTopFaceFromElement( symbolInstance ) ;
      var locationPoint = ( (LocationPoint) symbolInstance.Location ).Point ;

      TabPlaceExternal? tabPlaceExternal = null ;
      XYZ? placePoint = null ;
      XYZ? direction = null ;
      XYZ? firstPoint = null ;
      try {
        tabPlaceExternal = new TabPlaceExternal( uiDocument.Application, curves, locationPoint, dlg ) ;
        while ( true ) {
          if ( null == tabPlaceExternal.FirstPoint ) {
            var (x, y, z) = uiDocument.Selection.PickPoint() ;
            tabPlaceExternal.FirstPoint = new XYZ( x, y, z ) ;
          }
          else if ( null == tabPlaceExternal.SecondPoint ) {
            var secondPoint = uiDocument.Selection.PickPoint() ;
            if ( tabPlaceExternal.FirstPoint.DistanceTo( secondPoint ) <= uiDocument.Application.Application.ShortCurveTolerance )
              continue ;

            tabPlaceExternal.SecondPoint = secondPoint ;
            uiDocument.RefreshActiveView() ;
          }
          else {
            uiDocument.Selection.PickObject( ObjectType.Nothing ) ;
          }
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        switch ( dlg.IsCancel ) {
          case false when null != tabPlaceExternal :
          {
            placePoint = tabPlaceExternal.PlacePoint ;
            if ( null != tabPlaceExternal.SecondPoint && null != tabPlaceExternal.FirstPoint ) {
              direction = ( new XYZ( tabPlaceExternal.SecondPoint.X, tabPlaceExternal.SecondPoint.Y, tabPlaceExternal.FirstPoint.Z ) - tabPlaceExternal.FirstPoint ).Normalize() ;
              firstPoint = tabPlaceExternal.FirstPoint ;
            }

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
        dlg.Close() ;
        tabPlaceExternal?.Dispose() ;
      }

      return ( placePoint, direction, firstPoint ) ;
    }

    private static List<Curve> GetCurvesAtTopFaceFromElement( Element connector )
    {
      var options = new Options { View = connector.Document.ActiveView} ;
      var geometries = GeometryHelper.GetGeometryObjectsFromElementInstance( connector, options ).EnumerateAll() ;
      var curves = geometries.OfType<Curve>().Select( x => x.Clone() ).ToList() ;

      foreach ( var solid in geometries.OfType<Solid>() ) {
        foreach ( var planarFace in solid.Faces.OfType<PlanarFace>() ) {
          if ( Math.Abs( Math.Abs( planarFace.FaceNormal.DotProduct( XYZ.BasisZ ) ) - 1 ) > GeometryHelper.Tolerance )
            continue ;

          foreach ( EdgeArray edgeArray in planarFace.EdgeLoops )
          foreach ( Edge edge in edgeArray )
            curves.Add( edge.AsCurve().Clone() ) ;
        }
      }

      return curves ;
    }
  }
}