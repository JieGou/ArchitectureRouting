using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Revit ;
using System.Linq ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand", DefaultString = "Create FASU\nAnd VAV" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateFASUAndVAVAutomaticallyCommand : IExternalCommand
  {
    private const double DistanceBetweenFASUAndVAV = 0.25 ;
    private const string DiameterOfVAV = "250" ;
    private const int RootBranchNumber = 0 ;
    private const double MinDistanceSpacesCollinear = 2.5 ;
    private const string VAVAirflowName = "風量" ;

    private class FASUsAndVAVsInSpaceModel
    {
      public List<Element> listOfFASUs = new List<Element>() ;
      public List<Element> listOfVAVs = new List<Element>() ;
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var executor = CreateRoutingExecutor( document, commandData.View ) ;

      try {
        bool success ;
        object? state ;
        ( success, state ) = OperateUI( uiDocument, executor ) ;
        if ( state is string mes ) {
          message = mes ;
        }

        if ( success ) {
          return Result.Succeeded ;
        }

        return Result.Failed ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
    }

    private (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      IList<Element> spaces = GetAllSpaces( uiDocument.Document ).Where( space => space.HasParameter( BranchNumberParameter.BranchNumber ) ).ToArray() ;

      foreach ( var space in spaces ) {
        if ( ! HasBoundingBox( uiDocument.Document, space ) ) {
          return ( false, $"`{space.Name}` have not bounding box." ) ;
        }
      }

      if ( ! RoundDuctTypeExists( uiDocument.Document ) )
        return ( false, "There no RoundDuct family in the document." ) ;

      ConnectorPicker.IPickResult iPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand.PickConnector", null, GetAddInType() ) ;
      if ( iPickResult.PickedConnector != null && CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedConnector, spaces ) == Result.Succeeded ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUとVAVを配置しました。" ) ;
      }

      return ( true, null ) ;
    }

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    private RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    // コネクタが揃うように VAV, FASU の高さを決める
    private static void CalcFASUAndVAVHeight( Connector rootConnector, double fasuInConnectorHeight, double vavInConnectorHeight, double vavOutConnectorHeight, out double heightOfFASU, out double heightOfVAV )
    {
      var baseHeight = rootConnector.Origin.Z ;
      heightOfVAV = ( baseHeight - vavInConnectorHeight ) ;
      heightOfFASU = ( heightOfVAV + vavOutConnectorHeight - fasuInConnectorHeight ) ;
    }

    private static Result CreateFASUAndVAVAutomatically( Document document, Connector pickedConnector, IList<Element> spaces )
    {
      Dictionary<int, List<Element>> branchNumberToSpacesDictionary = new() ;
      foreach ( Element space in spaces ) {
        space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ;
        if ( branchNumberToSpacesDictionary.ContainsKey( branchNumber ) ) {
          branchNumberToSpacesDictionary[ branchNumber ].Add( space ) ;
        }
        else {
          branchNumberToSpacesDictionary.Add( branchNumber, new List<Element>() { space } ) ;
        }
      }

      if ( ! branchNumberToSpacesDictionary.TryGetValue( 0, out var rootSpaces ) ) {
        rootSpaces = new List<Element>() ;
      }

      if ( ! GetFASUAndVAVConnectorInfo( document, out var fasuInCoonectorHeight, out var vavOutConnectorHeight, out var vavUpstreamConnectorHeight, out var vavUpstreamConnectorNormal ) ) return Result.Failed ;
      CalcFASUAndVAVHeight( pickedConnector, fasuInCoonectorHeight, vavUpstreamConnectorHeight, vavOutConnectorHeight, out var heightOfFASU, out var heightOfVAV ) ;

      Dictionary<string, FASUsAndVAVsInSpaceModel> listOfFASUsAndVAVsBySpace = GetListOfFASUsAndVAVsBySpace( document, spaces ) ;
      if ( ! IsPreconditionOfFASUsAndVAVsSatisfied( listOfFASUsAndVAVsBySpace ) ) return Result.Failed ;

      Dictionary<Element, double> rotationAnglesOfFASUsAndVAVs = CalculateRotationAnglesOfFASUsAndVAVs( document, branchNumberToSpacesDictionary, pickedConnector, vavUpstreamConnectorNormal ) ;

      using ( Transaction tr = new(document) ) {
        tr.Start( "Create FASUs and VAVs Automatically" ) ;

        // TODO SpaceGroupごとにループを回す. 一直線に並んでいるグループの方向修正のため
        foreach ( var space in spaces ) {
          var designSupplyAirflow = ( space as Space )?.DesignSupplyAirflow ?? 0 ;
          if ( false == listOfFASUsAndVAVsBySpace.TryGetValue( space.Name, out var listOfFASUsAndVAVsInSpace ) )
            continue ;

          if ( listOfFASUsAndVAVsInSpace.listOfFASUs.Count == 1 && listOfFASUsAndVAVsInSpace.listOfVAVs.Count == 1 ) {
            // 既存のVAVに風量を設定する
            listOfFASUsAndVAVsInSpace.listOfVAVs.First().LookupParameter( VAVAirflowName ).Set( designSupplyAirflow ) ;
            continue ;
          }

          BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;
          var positionOfFASUAndVAV = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2, ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;
          var placeResult = PlaceFASUAndVAV( document, space.LevelId, positionOfFASUAndVAV, heightOfFASU, heightOfVAV, rotationAnglesOfFASUsAndVAVs[ space ] ) ;
          if ( placeResult == null ) continue ; // Failed to place

          var (instanceOfFASU, instanceOfVAV) = placeResult.Value ;

          // VAVに風量を設定する
          instanceOfVAV.LookupParameter( VAVAirflowName ).Set( designSupplyAirflow ) ;

          // この時点でコネクタの向きとは逆を向いている想定
          // コネクタの裏側にあるときは、ここで向きを反転する
          if ( rootSpaces.Contains( space ) && IsVavLocatedBehindConnector( document, instanceOfVAV, pickedConnector ) ) {
            ElementTransformUtils.RotateElements( document, new List<ElementId>() { instanceOfFASU.Id, instanceOfVAV.Id }, Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI ) ;
          }

          // TODO : 一直線にならんでいるグループの方向修正
          var fasuConnector = instanceOfFASU.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.In ) ;
          var vavConnector = instanceOfVAV.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.Out ) ;
          if ( fasuConnector != null && vavConnector != null ) {
            var duct = CreateDuctConnectionFASUAndVAV( document, fasuConnector, vavConnector, space.LevelId ) ;
            document.Regenerate() ;
            if ( duct == null ) continue ;
            // create group of FASUs, VAVs and RoundDuct
            var groupIds = new List<ElementId> { instanceOfFASU.Id, instanceOfVAV.Id, duct.Id } ;
            document.Create.NewGroup( groupIds ) ;
          }
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }

    private static (FamilyInstance instanceOfFASU, FamilyInstance instanceOfVAV)? PlaceFASUAndVAV( Document document, ElementId levelId, XYZ positionOfFASUAndVAV, double heightOfFASU, double heightOfVAV, double rotationAngle )
    {
      var positionOfFASU = new XYZ( positionOfFASUAndVAV.X, positionOfFASUAndVAV.Y, heightOfFASU ) ;
      var instanceOfFASU = document.AddFASU( positionOfFASU, levelId ) ;
      ElementTransformUtils.RotateElement( document, instanceOfFASU.Id, Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI / 2 ) ;

      var positionOfVAV = new XYZ( positionOfFASUAndVAV.X, positionOfFASUAndVAV.Y, heightOfVAV ) ;
      var instanceOfVAV = document.AddVAV( positionOfVAV, levelId ) ;
      instanceOfVAV.LookupParameter( "ダクト径" ).SetValueString( DiameterOfVAV ) ;

      BoundingBoxXYZ boxOfFASU = instanceOfFASU.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfFASU == null ) return null ;
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return null ;

      // Move the VAV to a distance distanceBetweenFASUAndVAV from FASU
      var distanceBetweenFASUCenterAndVAVCenter = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 + ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 2 + DistanceBetweenFASUAndVAV ;
      ElementTransformUtils.MoveElement( document, instanceOfVAV.Id, new XYZ( distanceBetweenFASUCenterAndVAVCenter, 0, 0 ) ) ;

      ElementTransformUtils.RotateElements( document, new List<ElementId>() { instanceOfFASU.Id, instanceOfVAV.Id }, Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), rotationAngle ) ;

      return ( instanceOfFASU, instanceOfVAV ) ;
    }

    private static Dictionary<string, FASUsAndVAVsInSpaceModel> GetListOfFASUsAndVAVsBySpace( Document document, IList<Element> spaces )
    {
      var listOfFASUsAndVAVsBySpace = new Dictionary<string, FASUsAndVAVsInSpaceModel>() ;

      var fasus = document.GetAllFamilyInstances( RoutingFamilyType.FASU_F4_150_200Phi )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F4_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F5_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F6_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F6_150_300Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F7_150_300Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F8_150_250Phi ) )
        .Union( document.GetAllFamilyInstances( RoutingFamilyType.FASU_F8_150_300Phi ) ) ;
      var fasuInstances = fasus as FamilyInstance[] ?? fasus.ToArray() ;
      var vavs = document.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ) ;
      var vavInstances = vavs as FamilyInstance[] ?? vavs.ToArray() ;

      foreach ( var space in spaces ) {
        BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
        if ( boxOfSpace == null ) continue ;

        var listOfFASUsAndVAVsInSpaceModel = new FASUsAndVAVsInSpaceModel() ;

        foreach ( var fasuInstance in fasuInstances ) {
          var fasuPosition = fasuInstance.Location as LocationPoint ;
          if ( fasuPosition == null ) continue ;

          if ( IsInSpace( boxOfSpace, fasuPosition.Point ) )
            listOfFASUsAndVAVsInSpaceModel.listOfFASUs.Add( fasuInstance ) ;
        }

        foreach ( var vavInstance in vavInstances ) {
          var vavPosition = vavInstance.Location as LocationPoint ;
          if ( vavPosition == null ) continue ;

          if ( IsInSpace( boxOfSpace, vavPosition.Point ) )
            listOfFASUsAndVAVsInSpaceModel.listOfVAVs.Add( vavInstance ) ;
        }

        listOfFASUsAndVAVsBySpace.Add( space.Name, listOfFASUsAndVAVsInSpaceModel ) ;
      }

      return listOfFASUsAndVAVsBySpace ;
    }

    private static bool IsPreconditionOfFASUsAndVAVsSatisfied( Dictionary<string, FASUsAndVAVsInSpaceModel> listOfFASUsAndVAVsBySpace )
    {
      if ( listOfFASUsAndVAVsBySpace.Any( x => x.Value.listOfVAVs.Count >= 2 || x.Value.listOfFASUs.Count >= 2 ) ) {
        var invalidSpacesList = listOfFASUsAndVAVsBySpace.Where( x => x.Value.listOfFASUs.Count >= 2 || x.Value.listOfVAVs.Count >= 2 ).Select( x => x.Key.Substring( 0, x.Key.IndexOf( " ", StringComparison.Ordinal ) ) ) ;
        TaskDialog.Show( "FASUとVAVの自動配置", $"同一のSpaceに2つ以上のFASU、VAVが存在しているため、処理に失敗しました。 \n該当Space: {string.Join( ",", invalidSpacesList )}" ) ;
        return false ;
      }

      if ( listOfFASUsAndVAVsBySpace.Any( x => x.Value.listOfVAVs.Count == 0 && x.Value.listOfFASUs.Count == 1 ) ) {
        var invalidSpacesList = listOfFASUsAndVAVsBySpace.Where( x => x.Value.listOfFASUs.Count == 1 && x.Value.listOfVAVs.Count == 0 ).Select( x => x.Key.Substring( 0, x.Key.IndexOf( " ", StringComparison.Ordinal ) ) ) ;
        TaskDialog.Show( "FASUとVAVの自動配置", $"以下のSpaceにFASUのみが配置されているため、処理に失敗しました。\n該当Space: {string.Join( ",", invalidSpacesList )}" ) ;
        return false ;
      }

      if ( listOfFASUsAndVAVsBySpace.Any( x => x.Value.listOfVAVs.Count == 1 && x.Value.listOfFASUs.Count == 0 ) ) {
        var invalidSpacesList = listOfFASUsAndVAVsBySpace.Where( x => x.Value.listOfFASUs.Count == 0 && x.Value.listOfVAVs.Count == 1 ).Select( x => x.Key.Substring( 0, x.Key.IndexOf( " ", StringComparison.Ordinal ) ) ) ;
        TaskDialog.Show( "FASUとVAVの自動配置", $"以下のSpaceにVAVのみが配置されているため、処理に失敗しました。\n該当Space: {string.Join( ",", invalidSpacesList )}" ) ;
        return false ;
      }

      return true ;
    }

    private static bool GetFASUAndVAVConnectorInfo( Document document, out double fasuInConnectorHeight, out double vavOutConnectorHeight, out double vavUpstreamConnectorHeight, out Vector3d vavUpstreamConnectorNormal )
    {
      bool fasuInConnectorExists = false ;
      bool vavInConnectorExists = false ;
      bool vavOutConnectorExists = false ;

      fasuInConnectorHeight = 0 ;
      vavOutConnectorHeight = 0 ;
      vavUpstreamConnectorHeight = 0 ;

      vavUpstreamConnectorNormal = new Vector3d( 1, 0, 0 ) ;

      void GetConnectorHeight( FamilyInstance fi, FlowDirectionType type, out bool connectorExists, out double connectorHeight )
      {
        var targetConnector = fi.GetConnectors().FirstOrDefault( c => c.Direction == type ) ;
        if ( targetConnector != null ) {
          connectorExists = true ;
          connectorHeight = targetConnector.Origin.Z ;
          return ;
        }

        connectorExists = false ;
        connectorHeight = 0 ;
      }

      using ( Transaction tr = new(document) ) {
        tr.Start( "Check the flow direction of FASU and VAV" ) ;

        var instanceOfFASU = document.AddFASU( new XYZ( 0, 0, 0 ), ElementId.InvalidElementId ) ;
        GetConnectorHeight( instanceOfFASU, FlowDirectionType.In, out fasuInConnectorExists, out fasuInConnectorHeight ) ;

        var instanceOfVAV = document.AddVAV( new XYZ( 0, 0, 0 ), ElementId.InvalidElementId ) ;
        GetConnectorHeight( instanceOfVAV, FlowDirectionType.Out, out vavOutConnectorExists, out vavOutConnectorHeight ) ;

        var vavInConnector = instanceOfVAV.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.In ) ;
        if ( vavInConnector != null ) {
          vavInConnectorExists = true ;
          vavUpstreamConnectorHeight = vavInConnector.Origin.Z ;
          vavUpstreamConnectorNormal = vavInConnector.CoordinateSystem.BasisZ.To3dPoint().normalized ;
        }

        tr.RollBack() ;
      }

      if ( ! fasuInConnectorExists ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUの流れの方向[イン]が設定されていないため、処理に失敗しました。" ) ;
        return false ;
      }

      if ( ! vavInConnectorExists ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "VAVの流れの方向[イン]が設定されていないため、処理に失敗しました。" ) ;
        return false ;
      }

      if ( ! vavOutConnectorExists ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "VAVの流れの方向[アウト]が設定されていないため、処理に失敗しました。" ) ;
        return false ;
      }

      return true ;
    }

    private static bool IsInSpace( BoundingBoxXYZ spaceBox, XYZ position )
    {
      return spaceBox.ToBox3d().Contains( position.To3dPoint(), 0.0 ) ;
    }

    private static Dictionary<Element, double> CalculateRotationAnglesOfFASUsAndVAVs( Document document, Dictionary<int, List<Element>> branchNumberDict, Connector rootConnector, Vector3d upstreamConnectorNormal )
    {
      var rotationAnglesOfFASUsAndVAVs = new Dictionary<Element, double>() ;

      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      var orthogonalToConnectorNormal = new Vector3d( rootConnectorNormal.y, -rootConnectorNormal.x, 0 ) ;

      foreach ( var branchNumber in branchNumberDict.Keys ) {
        List<Element> targetSpaces = branchNumberDict[ branchNumber ] ;

        if ( branchNumber == RootBranchNumber ) {
          var rotation = GetRotationForRootSpaces( rootConnector, upstreamConnectorNormal ) ;
          foreach ( var targetSpace in targetSpaces ) {
            rotationAnglesOfFASUsAndVAVs.Add( targetSpace, rotation ) ;
          }

          continue ;
        }

        if ( AreSpacesCollinear( document, targetSpaces, orthogonalToConnectorNormal ) ) {
          var rotation = GetRotationForCollinearSpaces( document, rootConnector, targetSpaces, upstreamConnectorNormal ) ;
          foreach ( var targetSpace in targetSpaces ) {
            rotationAnglesOfFASUsAndVAVs[ targetSpace ] = rotation ;
          }

          continue ;
        }

        var spaceBoxes = targetSpaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
        var spacesCenter = spaceBoxes.UnionBounds()!.Value.Center ;

        foreach ( var space in targetSpaces ) {
          var spaceCenter = space.get_BoundingBox( document.ActiveView ).ToBox3d().Center ;
          rotationAnglesOfFASUsAndVAVs[ space ] = GetRotationForNonCollinearSpace( rootConnector, spacesCenter, spaceCenter, upstreamConnectorNormal ) ;
        }
      }

      return rotationAnglesOfFASUsAndVAVs ;
    }

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    private static bool HasBoundingBox( Document document, Element element )
    {
      return null != element.get_BoundingBox( document.ActiveView ) ;
    }

    private static Duct? CreateDuctConnectionFASUAndVAV( Document document, Connector connectorOfFASU, Connector connectorOfVAV, ElementId levelId )
    {
      var collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ).WhereElementIsElementType().AsEnumerable().OfType<DuctType>() ;
      var ductTypes = collector.Where( e => e.Shape == ConnectorProfileType.Round ).ToArray() ;
      var ductType = ductTypes.FirstOrDefault( e => e.PreferredJunctionType == JunctionType.Tee ) ?? ductTypes.FirstOrDefault() ;
      return ductType != null ? Duct.Create( document, ductType.Id, levelId, connectorOfVAV, connectorOfFASU ) : null ;
    }

    private static bool RoundDuctTypeExists( Document document )
    {
      var collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ).AsEnumerable().OfType<DuctType>() ;
      return collector.Any( e => e.Shape == ConnectorProfileType.Round ) ;
    }

    #region SubFunctionsForRotation

    private static double ConvertDegreeToRadian( double degreeAngle )
    {
      return degreeAngle * Math.PI / 180 ;
    }

    private static double CalcRadianAngle2D( Vector3d from, Vector3d to )
    {
      var degree = Vector3d.SignedAngle( from, to, new Vector3d( 0, 0, 1 ) ) ;
      if ( degree != 0 ) return ConvertDegreeToRadian( degree ) ;
      return from == to ? 0 : Math.PI ;
    }

    private static double GetRotationForRootSpaces( Connector rootConnector, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      if ( upstreamConnectorNormal == rootConnectorNormal ) return ConvertDegreeToRadian( 180 ) ;

      return CalcRadianAngle2D( upstreamConnectorNormal, -rootConnectorNormal ) ;
    }

    private static double GetRotationForCollinearSpaces( Document document, Connector rootConnector, IReadOnlyList<Element> spaces, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorOrigin = rootConnector.Origin.To3dPoint() ;
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;

      var spaceBoxes = spaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
      var centerOfSpaces = spaceBoxes.UnionBounds()!.Value.Center ;

      // RootConnectorの法線方向基準で、RootConnectorより奥にある場合は、上流につなげるConnectorの向きを法線方向とは逆向きにする.
      var sign = Vector3d.Dot( centerOfSpaces - rootConnectorOrigin, rootConnectorNormal ) > 0 ? -1 : 1 ;
      return CalcRadianAngle2D( upstreamConnectorNormal, sign * rootConnectorNormal ) ;
    }

    private static double GetRotationForNonCollinearSpace( Connector rootConnector, Vector3d spaceGroupCenter, Vector3d spaceCenter, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      var sign = Vector3d.Dot( spaceGroupCenter - spaceCenter, rootConnectorNormal ) > 0 ? 1 : -1 ;
      return CalcRadianAngle2D( upstreamConnectorNormal, sign * rootConnectorNormal ) ;
    }

    private static bool AreSpacesCollinear( Document document, IReadOnlyList<Element> spaces, Vector3d checkTargetDir2D )
    {
      var orthogonalToTargetDir = new Vector3d( checkTargetDir2D.y, -checkTargetDir2D.x, 0.0 ) ;

      var spaceBoxes = spaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
      var centerOfSpaces = spaceBoxes.UnionBounds()!.Value.Center ;

      return spaceBoxes.Select( box => box.Center ).All( center => Math.Abs( Vector3d.Dot( centerOfSpaces - center, orthogonalToTargetDir ) ) < MinDistanceSpacesCollinear ) ;
    }

    private static bool IsVavLocatedBehindConnector( Document document, Element instanceOfVAV, Connector instanceOfConnector )
    {
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;

      var connectorPosition = instanceOfConnector.Origin.To3dPoint() ;
      var connectorNormal = instanceOfConnector.CoordinateSystem.BasisZ.To3dDirection() ;

      // コネクタの向いている方向の成分で比較したときに、VAVのBoxの角が1つでもコネクタ位置よりも小さければ後方とみなす.
      return boxOfVAV.ToBox3d().Vertices().Any( boxCorner => Vector3d.Dot( boxCorner - connectorPosition, connectorNormal ) < 0 ) ;
    }

    #endregion
  }
}