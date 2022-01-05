using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  internal class FASUAndVAVMaintainerForTTE
  {
    private const double MinDistanceSpacesCollinear = 2.5 ;

    private IReadOnlyCollection<MaintainerGroup> _groups = Array.Empty<MaintainerGroup>() ;
    private FASUAndVAVCreatorForTTE _creator = null! ;

    private class Maintainer
    {
      private Element _space ;
      private FamilyInstance? _fasuInstance ;
      private FamilyInstance? _vavInstance ;

      public Maintainer( Element space, FamilyInstance? fasuInstance, FamilyInstance? vavInstance )
      {
        _space = space ;
        _fasuInstance = fasuInstance ;
        _vavInstance = vavInstance ;
        SpaceBoundingBox = space.get_BoundingBox( space.Document.ActiveView ).ToBox3d() ;
        SpaceBoundingBoxCenter = SpaceBoundingBox.Center ;
        Position = fasuInstance?.Location is LocationPoint lp ? lp.Point.To3dPoint() : SpaceBoundingBoxCenter ;
        VAVUpstreamDirection = new Vector3d( 1, 0, 0 ) ;
        FASUAndVAVOriginaryExist = fasuInstance != null && vavInstance != null ;
        FASUBoundingBox = _fasuInstance?.get_BoundingBox( _fasuInstance.Document.ActiveView )?.ToBox3d() ;
        VAVBoundingBox = _vavInstance?.get_BoundingBox( _vavInstance.Document.ActiveView )?.ToBox3d() ;

        ToBeConnectedAndGrouped = _fasuInstance == null ;
      }

      public Box3d SpaceBoundingBox { get ; }
      public Vector3d SpaceBoundingBoxCenter { get ; }
      public Vector3d Position { get ; set ; }
      public Vector3d VAVUpstreamDirection { get ; set ; }
      public bool FASUAndVAVOriginaryExist { get ; }
      public Box3d? VAVBoundingBox { get ; private set ; }
      private Box3d? FASUBoundingBox { get ; set ; }
      private Document Document => _space.Document ;
      private double Airflow => TTEUtil.ConvertDesignSupplyAirflowFromInternalUnits( ( _space as Space )?.DesignSupplyAirflow ?? 0 ) ;

      public Vector3d? VAVUpstreamConnectorPosition
      {
        get
        {
          var upstreamConnector = _vavInstance?.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.In ) ;
          if ( upstreamConnector == null ) return null ;
          return upstreamConnector.Origin.To3dPoint() ;
        }
      }

      private bool IsMoveable( Vector3d moveVec )
      {
        var moveVec2d = moveVec.To2d() ;
        var spaceBox2d = SpaceBoundingBox.ToBox2d() ;

        var newPosition2d = Position.To2d() + moveVec2d ;
        if ( ! spaceBox2d.IsInclude( newPosition2d, 0.0 ) ) return false ;

        // FASU, VAVがあるときは、Boxがはみ出さないかもチェックする
        if ( FASUBoundingBox != null ) {
          var fasuBox2d = FASUBoundingBox.Value.ToBox2d() ;
          if ( ! spaceBox2d.IsInclude( fasuBox2d.Min + moveVec2d, 0.0 )
               || ! spaceBox2d.IsInclude( fasuBox2d.Max + moveVec2d, 0.0 ) ) return false ;
        }

        if ( VAVBoundingBox != null ) {
          var vavBox2d = VAVBoundingBox.Value.ToBox2d() ;
          if ( ! spaceBox2d.IsInclude( vavBox2d.Min + moveVec2d, 0.0 )
               || ! spaceBox2d.IsInclude( vavBox2d.Max + moveVec2d, 0.0 ) ) return false ;
        }

        return true ;
      }

      public void TryToMove( Vector3d vec )
      {
        if ( ! IsMoveable( vec ) ) return ;

        Position += vec ;

        var target = new List<ElementId>() ;
        if ( _fasuInstance != null ) target.Add( _fasuInstance.Id ) ;
        if ( _vavInstance != null ) target.Add( _vavInstance.Id ) ;
        if ( ! target.Any() ) return ;

        ElementTransformUtils.MoveElements( Document, target, vec.ToXYZPoint() ) ;
      }

      private bool ToBeConnectedAndGrouped { get ; set ; }

      public void CreateFASUAndVAV( FASUAndVAVCreatorForTTE creator, double upstreamConnectorHeight )
      {
        ( _fasuInstance, _vavInstance ) = creator.Create( Position.ToXYZPoint(), VAVUpstreamDirection.ToXYZDirection(), upstreamConnectorHeight, Airflow, _space.LevelId ) ;
        FASUBoundingBox = _fasuInstance?.get_BoundingBox( _fasuInstance.Document.ActiveView )?.ToBox3d() ;
        VAVBoundingBox = _vavInstance?.get_BoundingBox( _vavInstance.Document.ActiveView )?.ToBox3d() ;
      }

      private static ICollection<ElementId> GetDuctElementIdsBetween2Connectors( Connector start, Connector end )
      {
        const int limit = 10 ;

        var result = new List<ElementId>() ;

        var firstCandidates = start.GetConnectedConnectors().ToArray() ;
        if ( firstCandidates.Length != 1 ) return result ;

        var current = firstCandidates.First() ;

        result.Add( current.Owner.Id ) ;

        for ( var i = 0 ; i < limit ; ++i ) {
          if ( current.Owner.Id == end.Owner.Id && current.Id == end.Id ) return result ;

          var oppositeConnectors = current.Owner.GetConnectors().Where( connector => connector.Id != current.Id ).ToArray() ;
          if ( oppositeConnectors.Length == 0 ) return result ; // 途切れているケース
          if ( oppositeConnectors.Length != 1 ) return Array.Empty<ElementId>() ;

          var nextConnectors = oppositeConnectors.First().GetConnectedConnectors().ToArray() ;
          if ( nextConnectors.Length != 1 ) return Array.Empty<ElementId>() ;

          current = nextConnectors.First() ;
        }

        return new List<ElementId>() ; // limitでたどりきれなかった
      }

      public void UpdateFASUAndVAVIfRequired( FASUAndVAVCreatorForTTE creator )
      {
        if ( _fasuInstance == null || _vavInstance == null ) return ;

        if ( creator.IsVAVAirflowUpdateRequired( _vavInstance, Airflow ) ) creator.UpdateVAVAirflow( _vavInstance, Airflow ) ;
        if ( ! creator.IsVAVDiameterUpdateRequired( _vavInstance, Airflow ) ) return ; // ダクト径変更がなければFASUも調整不要

        var fasuUpstreamConnector = _fasuInstance.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
        if ( fasuUpstreamConnector == null ) return ;
        var vavDownstreamConnector = _vavInstance.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.Out ) ;
        if ( vavDownstreamConnector == null ) return ;

        var groupId = _fasuInstance!.GroupId ;

        if ( groupId != ElementId.InvalidElementId ) {
          var collector = new FilteredElementCollector( Document ) ;
          var group = collector.OfClass( typeof( Group ) ).FirstOrDefault( group => group.Id == groupId ) as Group ;
          group?.UngroupMembers() ;
        }

        var ductElementIds = GetDuctElementIdsBetween2Connectors( fasuUpstreamConnector, vavDownstreamConnector ) ;
        Document.Delete( ductElementIds ) ;

        Document.Regenerate() ;

        _fasuInstance.ChangeTypeId( creator.GetFASUTypeId( Airflow ) ) ;
        creator.UpdateVAVDiameter( _vavInstance!, Airflow ) ;

        ToBeConnectedAndGrouped = true ;
      }

      private static void SetSupplyAirDuctType( Duct duct )
      {
        var param = duct.get_Parameter( BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM ) ;

        var currentSystemTypeId = param.AsElementId() ;
        var ductSystemTypes = new FilteredElementCollector( duct.Document ).OfCategory( BuiltInCategory.OST_DuctSystem ).OfType<MechanicalSystemType>().ToList() ;

        var currentSystemType = ductSystemTypes.FirstOrDefault( type => type.Id == currentSystemTypeId ) ;
        if ( currentSystemType is { SystemClassification: MEPSystemClassification.SupplyAir } ) return ;

        var type = ductSystemTypes.FirstOrDefault( type => type.SystemClassification == MEPSystemClassification.SupplyAir ) ;
        if ( type != null ) {
          param.Set( type.Id ) ;
        }
      }

      public void ExecutePostProcess( bool needReverseDirection )
      {
        if ( _fasuInstance == null || _vavInstance == null ) return ;

        if ( ! FASUAndVAVOriginaryExist && needReverseDirection ) {
          var rotateCenter = Position.ToXYZPoint() ;
          ElementTransformUtils.RotateElements( _fasuInstance.Document, new List<ElementId>() { _fasuInstance.Id, _vavInstance.Id }, Line.CreateBound( rotateCenter, rotateCenter + XYZ.BasisZ ), Math.PI ) ;
        }

        var fasuUpstreamConnector = _fasuInstance.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
        var vavDownstreamConnector = _vavInstance.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.Out ) ;

        if ( fasuUpstreamConnector == null || vavDownstreamConnector == null ) return ;

        if ( ! ToBeConnectedAndGrouped ) return ;

        var duct = ConnectByRoundDuct( _fasuInstance.Document, vavDownstreamConnector, fasuUpstreamConnector, _fasuInstance.LevelId ) ;
        if ( duct == null ) return ; // 接続できなかった場合はグループ化も行わない

        SetSupplyAirDuctType( duct ) ;

        // TODO Regenerateの回数は減らしたいので外側で
        _fasuInstance.Document.Regenerate() ;
        _fasuInstance.Document.Create.NewGroup( new[] { _fasuInstance.Id, _vavInstance.Id, duct.Id } ) ;
      }

      private static Duct? ConnectByRoundDuct( Document document, Connector fromConnector, Connector toConnector, ElementId levelId )
      {
        var collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ).WhereElementIsElementType().AsEnumerable().OfType<DuctType>() ;
        var ductTypes = collector.Where( e => e.Shape == ConnectorProfileType.Round ).ToArray() ;
        var ductType = ductTypes.FirstOrDefault( e => e.PreferredJunctionType == JunctionType.Tee ) ?? ductTypes.FirstOrDefault() ;
        return ductType != null ? Duct.Create( document, ductType.Id, levelId, fromConnector, toConnector ) : null ;
      }
    }

    abstract class MaintainerGroup
    {
      protected IReadOnlyCollection<Maintainer> Maintainers ;

      protected MaintainerGroup( IReadOnlyCollection<Maintainer> maintainers )
      {
        Maintainers = maintainers ;
      }

      public abstract void DecideTemporaryRotation( Vector3d rootDirection ) ;
      public abstract void ExecutePostProcess( Vector3d rootPosition, Vector3d rootDirection ) ;

      public void CreateFASUAndVAV( FASUAndVAVCreatorForTTE creator, double upstreamConnectorHeight )
      {
        foreach ( var maintainer in Maintainers ) {
          if ( maintainer.FASUAndVAVOriginaryExist ) {
            maintainer.UpdateFASUAndVAVIfRequired( creator ) ;
          }
          else {
            maintainer.CreateFASUAndVAV( creator, upstreamConnectorHeight ) ;
          }
        }
      }

      protected static bool IsVAVLocatedBehindConnector( Vector3d rootPosition, Vector3d rootDirection, Box3d vavBox )
      {
        // コネクタの向いている方向の成分で比較したときに、VAVのBoxの角が1つでもコネクタ位置よりも小さければ後方とみなす.
        return vavBox.Vertices().Any( boxCorner => Vector3d.Dot( boxCorner - rootPosition, rootDirection ) < 0 ) ;
      }
    }

    private class RootGroup : MaintainerGroup
    {
      public RootGroup( IReadOnlyCollection<Maintainer> maintainers ) : base( maintainers )
      {
      }

      public override void DecideTemporaryRotation( Vector3d rootDirection )
      {
        foreach ( var maintainer in Maintainers.Where( maintainer => ! maintainer.FASUAndVAVOriginaryExist ) ) {
          maintainer.VAVUpstreamDirection = -rootDirection ;
        }
      }

      public override void ExecutePostProcess( Vector3d rootPosition, Vector3d rootDirection )
      {
        // rootDirection方向に一番とおいものを、rootPositionと並ぶように移動
        var farthestMaintainer = Maintainers.MaxBy( maintainer => Vector3d.Dot( maintainer.Position - rootPosition, rootDirection ) ) ;
        if ( ! farthestMaintainer!.FASUAndVAVOriginaryExist && farthestMaintainer.VAVUpstreamConnectorPosition is { } upstreamPosition ) {
          var originalDiffVec = upstreamPosition - rootPosition ;
          var moveVec = -( originalDiffVec - Vector3d.Dot( originalDiffVec, rootDirection ) * rootDirection ) ;
          farthestMaintainer.TryToMove( moveVec ) ;
        }

        foreach ( var maintainer in Maintainers ) {
          if ( maintainer.FASUAndVAVOriginaryExist || ! maintainer.VAVBoundingBox.HasValue ) {
            maintainer.ExecutePostProcess( false ) ;
            continue ;
          }

          maintainer.ExecutePostProcess( IsVAVLocatedBehindConnector( rootPosition, rootDirection, maintainer.VAVBoundingBox.Value ) ) ;
        }
      }
    }

    private class CollinearGroup : MaintainerGroup
    {
      public CollinearGroup( IReadOnlyCollection<Maintainer> maintainers ) : base( maintainers )
      {
      }

      public override void DecideTemporaryRotation( Vector3d rootDirection )
      {
        foreach ( var maintainer in Maintainers.Where( maintainer => ! maintainer.FASUAndVAVOriginaryExist ) ) {
          maintainer.VAVUpstreamDirection = -rootDirection ;
        }
      }

      public override void ExecutePostProcess( Vector3d rootPosition, Vector3d rootDirection )
      {
        var boundingBoxes = Maintainers.Select( maintainer => maintainer.VAVBoundingBox ).ToArray() ;

        var reverse = false ;
        if ( boundingBoxes.All( box => box.HasValue ) ) {
          reverse = IsVAVLocatedBehindConnector( rootPosition, rootDirection, boundingBoxes.UnionBounds()!.Value ) ;
        }

        foreach ( var maintainer in Maintainers ) {
          maintainer.ExecutePostProcess( reverse ) ;
        }
      }
    }

    private class NonCollinearGroup : MaintainerGroup
    {
      public NonCollinearGroup( IReadOnlyCollection<Maintainer> maintainers ) : base( maintainers )
      {
      }

      public override void DecideTemporaryRotation( Vector3d rootDirection )
      {
        var unionBoxCenter = Maintainers.Select( maintainer => maintainer.SpaceBoundingBox ).UnionBounds()!.Value.Center ;

        foreach ( var maintainer in Maintainers.Where( maintainer => ! maintainer.FASUAndVAVOriginaryExist ) ) {
          var sign = Vector3d.Dot( maintainer.Position - unionBoxCenter, rootDirection ) > 0 ? -1 : 1 ;
          maintainer.VAVUpstreamDirection = sign * rootDirection ;
        }
      }

      public override void ExecutePostProcess( Vector3d rootPosition, Vector3d rootDirection )
      {
        foreach ( var maintainer in Maintainers ) {
          maintainer.ExecutePostProcess( false ) ;
        }
      }
    }

    private static IReadOnlyCollection<FamilyInstance> GetAllFASUs( Document document )
    {
      var fasus = document.GetAllFamilyInstances( RoutingFamilyType.FASU_F4_150_200Phi )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F4_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F5_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F6_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F6_150_300Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F7_150_300Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F8_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F8_150_300Phi ) ) ;
      return fasus as FamilyInstance[] ?? fasus.ToArray() ;
    }

    private static Element? GetSpaceWhichContainsPosition( IEnumerable<(Element space, Box3d box)> spaceBoxPairs, Vector3d position )
    {
      foreach ( var spaceBoxPair in spaceBoxPairs ) {
        if ( spaceBoxPair.box.Contains( position, 0.0 ) ) return spaceBoxPair.space ;
      }

      return null ;
    }

    private static (bool Success, IReadOnlyCollection<Element> multipleInstanceSpaces) CreateSpaceToFamilyInstanceDictionary( IEnumerable<Element> targetSpaces, IEnumerable<FamilyInstance> targetInstances, out Dictionary<Element, FamilyInstance?> result )
    {
      var spaceBoxPairs = new List<(Element, Box3d)>() ;
      result = new Dictionary<Element, FamilyInstance?>() ;
      foreach ( var targetSpace in targetSpaces ) {
        spaceBoxPairs.Add( ( targetSpace, targetSpace.get_BoundingBox( targetSpace.Document.ActiveView ).ToBox3d() ) ) ;
        result.Add( targetSpace, null! ) ;
      }

      var multipleInstanceSpace = new List<Element>() ;

      foreach ( var familyInstance in targetInstances ) {
        var locationPoint = (LocationPoint)familyInstance.Location ;
        var space = GetSpaceWhichContainsPosition( spaceBoxPairs, locationPoint.Point.To3dPoint() ) ;

        if ( space == null ) continue ;

        if ( result.TryGetValue( space, out var existed ) ) {
          if ( existed != null ) {
            multipleInstanceSpace.Add( space ) ;
            continue ;
          }

          result[ space ] = familyInstance ;
        }
      }

      return ( multipleInstanceSpace.Count == 0, multipleInstanceSpace ) ;
    }

    private static string CreateErrorMessageAboutElements( string errorMessage, IEnumerable<Element> elements )
    {
      string message = errorMessage + Environment.NewLine ;
      foreach ( var element in elements ) {
        message += Environment.NewLine + element.Name ;
      }

      return message ;
    }

    private static (bool Success, string ErrorMessage) CheckOnlyOneOfFASUAndVAVExistInSpace( IReadOnlyDictionary<Element, FamilyInstance?> spaceToFASU, IReadOnlyDictionary<Element, FamilyInstance?> spaceToVAV )
    {
      var errorSpaces = new List<Element>() ;
      foreach ( var spaceFASUPair in spaceToFASU ) {
        if ( spaceFASUPair.Value == null ) continue ;
        if ( ! spaceToVAV.TryGetValue( spaceFASUPair.Key, out var vavInstance ) ) continue ;
        if ( vavInstance != null ) continue ;

        errorSpaces.Add( spaceFASUPair.Key ) ;
      }

      if ( errorSpaces.Any() ) {
        return ( false, CreateErrorMessageAboutElements( "FASU exists in the space, but VAV not.", errorSpaces ) ) ;
      }

      foreach ( var spaceVAVPair in spaceToVAV ) {
        if ( spaceVAVPair.Value == null ) continue ;
        if ( ! spaceToFASU.TryGetValue( spaceVAVPair.Key, out var fasuInstance ) ) continue ;
        if ( fasuInstance != null ) continue ;

        errorSpaces.Add( spaceVAVPair.Key ) ;
      }

      if ( errorSpaces.Any() ) {
        return ( false, CreateErrorMessageAboutElements( "VAV exists in the space, but FASU not.", errorSpaces ) ) ;
      }

      return ( true, string.Empty ) ;
    }

    private static bool HasValidBranchNumber( Element space )
    {
      if ( ! space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ) return false ;
      return branchNumber >= 0 ;
    }
    
    private static (bool Success, string ErrorMessage) CreateMaintainersGroupedByBranchNumber( Document document, out Dictionary<int, List<Maintainer>> result )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;

      var targetSpaces = collector.WherePasses( filter ).WhereElementIsNotElementType().Where( HasValidBranchNumber ).ToArray() ;

      result = new Dictionary<int, List<Maintainer>>() ;

      foreach ( var space in targetSpaces ) {
        if ( space.get_BoundingBox( document.ActiveView ) == null ) return ( false, $"{space.Name} doesn't have bounding box." ) ;
      }

      var (success, multipleInstanceSpaces) = CreateSpaceToFamilyInstanceDictionary( targetSpaces, GetAllFASUs( document ), out var spaceToFASU ) ;
      if ( ! success ) return ( false, CreateErrorMessageAboutElements( "Multiple FASUs exist in the space.", multipleInstanceSpaces ) ) ;

      ( success, multipleInstanceSpaces ) = CreateSpaceToFamilyInstanceDictionary( targetSpaces, document.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ), out var spaceToVAV ) ;
      if ( ! success ) return ( false, CreateErrorMessageAboutElements( "Multiple VAVs exist in the space.", multipleInstanceSpaces ) ) ;

      string errorMessage ;
      ( success, errorMessage ) = CheckOnlyOneOfFASUAndVAVExistInSpace( spaceToFASU, spaceToVAV ) ;
      if ( ! success ) return ( false, errorMessage ) ;

      foreach ( var space in targetSpaces ) {
        space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ;

        var maintainer = new Maintainer( space, spaceToFASU[ space ], spaceToVAV[ space ] ) ;
        if ( result.TryGetValue( branchNumber, out var list ) ) {
          list.Add( maintainer ) ;
        }
        else {
          result.Add( branchNumber, new List<Maintainer> { maintainer } ) ;
        }
      }

      return ( true, string.Empty ) ;
    }

    private static MaintainerGroup CreateMaintainerGroup( Vector3d rootConnectorNormal, int branchNumber, IReadOnlyCollection<Maintainer> maintainers )
    {
      if ( branchNumber == 0 ) return new RootGroup( maintainers ) ;

      var unionBoxCenter = maintainers.Select( maintainer => maintainer.SpaceBoundingBox ).UnionBounds()!.Value.Center ;
      if ( maintainers.All( maintainer => Vector3d.Dot( unionBoxCenter - maintainer.SpaceBoundingBoxCenter, rootConnectorNormal ) < MinDistanceSpacesCollinear ) ) {
        return new CollinearGroup( maintainers ) ;
      }

      return new NonCollinearGroup( maintainers ) ;
    }

    public (bool Success, string ErrorMessage) Setup( Document document, Vector3d rootConnectorNormal )
    {
      _creator = new FASUAndVAVCreatorForTTE() ;
      var (success, errorMessage) = _creator.Setup( document ) ;
      if ( ! success ) return ( false, errorMessage ) ;

      ( success, errorMessage ) = CreateMaintainersGroupedByBranchNumber( document, out var branchNumberToGroup ) ;
      if ( ! success ) return ( false, errorMessage ) ;

      _groups = branchNumberToGroup.Select( branchNumSpacePair => CreateMaintainerGroup( rootConnectorNormal, branchNumSpacePair.Key, branchNumSpacePair.Value ) ).ToArray() ;
      foreach ( var maintainerGroup in _groups ) {
        maintainerGroup.DecideTemporaryRotation( rootConnectorNormal ) ;
      }

      return ( true, "" ) ;
    }

    public void Execute( Vector3d rootConnectorPosition, Vector3d rootConnectorDirection, double vavUpstreamConnectorHeight )
    {
      foreach ( var maintainerGroup in _groups ) {
        maintainerGroup.CreateFASUAndVAV( _creator, vavUpstreamConnectorHeight ) ;
      }

      foreach ( var maintainerGroup in _groups ) {
        maintainerGroup.ExecutePostProcess( rootConnectorPosition, rootConnectorDirection ) ;
      }
    }
  }
}