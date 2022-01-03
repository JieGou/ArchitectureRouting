using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  internal class FASUAndVAVMaintainerForTTE
  {
    private const double MinDistanceSpacesCollinear = 2.5 ;

    private IReadOnlyCollection<MaintainerGroup> _groups = null! ;
    private FASUAndVAVCreator _creator = null! ;

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
        VAVBoundingBox = _vavInstance?.get_BoundingBox( _vavInstance.Document.ActiveView )?.ToBox3d() ;

        ToBeGrouping = _fasuInstance == null ;
        ToBeConnected = _fasuInstance == null ;
      }

      public Box3d SpaceBoundingBox { get ; }
      public Vector3d SpaceBoundingBoxCenter { get ; }
      public Vector3d Position { get ; set ; }
      public Vector3d VAVUpstreamDirection { get ; set ; }
      public bool FASUAndVAVOriginaryExist { get ; }
      public Box3d? VAVBoundingBox { get ; private set ; }
      public bool ToBeGrouping { get ; private set ; }
      public bool ToBeConnected { get ; private set ; }

      public void CreateFASUAndVAV( FASUAndVAVCreator creator, double upstreamConnectorHeight )
      {
        var airflow = TTEUtil.ConvertDesignSupplyAirflowFromInternalUnits( ( _space as Space )?.DesignSupplyAirflow ?? 0 ) ;
        ( _fasuInstance, _vavInstance ) = creator.Create( Position.ToXYZPoint(), VAVUpstreamDirection.ToXYZDirection(), upstreamConnectorHeight, airflow, _space.LevelId ) ;
        VAVBoundingBox = _vavInstance?.get_BoundingBox( _vavInstance.Document.ActiveView )?.ToBox3d() ;
      }

      public bool AreFASUAndVAVUpdateNeeded( FASUAndVAVCreator creator )
      {
        if ( _vavInstance == null ) return false ;
        var airflow = TTEUtil.ConvertDesignSupplyAirflowFromInternalUnits( ( _space as Space )?.DesignSupplyAirflow ?? 0 ) ;
        return ! creator.IsVAVDiameterAndAirflowSet( _vavInstance, airflow ) ;
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
          if ( current == end ) return result ;

          var nextCandidates = current.Owner.GetConnectors().Where( connector => connector != current ).ToArray() ;
          if ( nextCandidates.Length == 0 ) return result ; // 途切れているケース
          if ( nextCandidates.Length != 1 ) return new List<ElementId>() ;
          current = nextCandidates.First() ;
        }

        return new List<ElementId>() ; // limitでたどりきれなかった
      }

      public void UpdateFASUAndVAV( FASUAndVAVCreator creator )
      {
        var airflow = TTEUtil.ConvertDesignSupplyAirflowFromInternalUnits( ( _space as Space )?.DesignSupplyAirflow ?? 0 ) ;
        var fasuUpstreamConnector = _fasuInstance!.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
        if ( fasuUpstreamConnector == null ) return ;
        var vavDownstreamConnector = _vavInstance!.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.Out ) ;
        if ( vavDownstreamConnector == null ) return ;

        // TODO 既存グループに追加しないと行けないケースがあるため、とりあえずはグルーピングなし.
        // ToBeGrouping = _fasuInstance!.GroupId != ElementId.InvalidElementId ;

        var ductElementIds = GetDuctElementIdsBetween2Connectors( fasuUpstreamConnector, vavDownstreamConnector ) ;
        _fasuInstance!.Document.Delete( ductElementIds ) ;

        _fasuInstance!.ChangeTypeId( creator.GetFASUTypeId( airflow ) ) ;
        creator.UpdateVAVDiameter( _vavInstance!, airflow ) ;

        ToBeConnected = true ;
      }

      public void ExecutePostProcess( bool needReverseDirection )
      {
        if ( _fasuInstance == null || _vavInstance == null ) return ;
        if ( FASUAndVAVOriginaryExist ) return ;

        if ( needReverseDirection ) {
          var rotateCenter = Position.ToXYZPoint() ;
          ElementTransformUtils.RotateElements( _fasuInstance.Document, new List<ElementId>() { _fasuInstance.Id, _vavInstance.Id }, Line.CreateBound( rotateCenter, rotateCenter + XYZ.BasisZ ), Math.PI ) ;
        }

        var fasuUpstreamConnector = _fasuInstance.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
        var vavDownstreamConnector = _vavInstance.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.Out ) ;

        if ( fasuUpstreamConnector == null || vavDownstreamConnector == null ) return ;

        var duct = ConnectByRoundDuct( _fasuInstance.Document, vavDownstreamConnector, fasuUpstreamConnector, _fasuInstance.LevelId ) ;

        // TODO Regenerateの回数は減らしたいので外側で
        _fasuInstance.Document.Regenerate() ;
        _fasuInstance.Document.Create.NewGroup( new ElementId[] { _fasuInstance.Id, _vavInstance.Id, duct!.Id } ) ;
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

      public void CreateFASUAndVAV( FASUAndVAVCreator creator, double upstreamConnectorHeight )
      {
        foreach ( var maintainer in Maintainers ) {
          if ( maintainer.FASUAndVAVOriginaryExist ) {
            if ( maintainer.AreFASUAndVAVUpdateNeeded( creator ) ) maintainer.UpdateFASUAndVAV( creator ) ;
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

    private static (bool Successed, IReadOnlyCollection<Element> multipleInstanceSpaces) CreateSpaceToFamilyInstanceDictionary( IEnumerable<Element> targetSpaces, IEnumerable<FamilyInstance> targetInstances, out Dictionary<Element, FamilyInstance?> result )
    {
      var spaceBoxPairs = new List<(Element, Box3d)>() ;
      result = new Dictionary<Element, FamilyInstance?>() ;
      foreach ( var targetSpace in targetSpaces ) {
        spaceBoxPairs.Add( ( targetSpace, targetSpace.get_BoundingBox( targetSpace.Document.ActiveView ).ToBox3d() ) ) ;
        result.Add( targetSpace, null! ) ;
      }

      var multipleInstanceSpace = new List<Element>() ;

      foreach ( var familyInstance in targetInstances ) {
        //var space = familyInstance.Space ;
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

    private static (bool Error, string ErrorMessage) CreateMaintainersGroupedByBranchNumber( Document document, out Dictionary<int, List<Maintainer>> result )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;

      var targetSpaces = collector.WherePasses( filter ).WhereElementIsNotElementType().Where( space => space.HasParameter( BranchNumberParameter.BranchNumber ) ).ToArray() ;

      result = new Dictionary<int, List<Maintainer>>() ;

      var (successed, multipleInstanceSpaces) = CreateSpaceToFamilyInstanceDictionary( targetSpaces, GetAllFASUs( document ), out var spaceToFASU ) ;
      if ( ! successed ) {
        return ( true, "" ) ;
      }

      ( successed, multipleInstanceSpaces ) = CreateSpaceToFamilyInstanceDictionary( targetSpaces, document.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ), out var spaceToVAV ) ;
      if ( ! successed ) {
        return ( true, "" ) ;
      }

      // TODO fasu, vav の片方だけあるケースのチェック

      foreach ( var space in targetSpaces ) {
        space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ;

        var maintainer = new Maintainer( space, spaceToFASU[ space ]!, spaceToVAV[ space ] ) ;
        if ( result.TryGetValue( branchNumber, out var list ) ) {
          list.Add( maintainer ) ;
        }
        else {
          result.Add( branchNumber, new List<Maintainer> { maintainer } ) ;
        }
      }

      return ( false, string.Empty ) ;
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

    public (bool Error, string ErrorMessage) Setup( Document document, Vector3d rootConnectorNormal )
    {
      _creator = new FASUAndVAVCreator() ;
      var (error, errorMessage) = _creator.Setup( document ) ;
      if ( error ) return ( true, errorMessage ) ;

      ( error, errorMessage ) = CreateMaintainersGroupedByBranchNumber( document, out var branchNumberToGroup ) ;
      if ( error ) return ( true, errorMessage ) ;

      _groups = branchNumberToGroup.Select( branchNumSpacePair => CreateMaintainerGroup( rootConnectorNormal, branchNumSpacePair.Key, branchNumSpacePair.Value ) ).ToArray() ;
      foreach ( var maintainerGroup in _groups ) {
        maintainerGroup.DecideTemporaryRotation( rootConnectorNormal ) ;
      }

      return ( false, "" ) ;
    }

    public void Execute( Vector3d rootConnectorPosition, Vector3d rootConnectorDirection, double vavUpstreamConnectorHeight )
    {
      if ( _groups == null ) return ;
      foreach ( var maintainerGroup in _groups ) {
        maintainerGroup.CreateFASUAndVAV( _creator, vavUpstreamConnectorHeight ) ;
      }

      foreach ( var maintainerGroup in _groups ) {
        maintainerGroup.ExecutePostProcess( rootConnectorPosition, rootConnectorDirection ) ;
      }
    }
  }
}