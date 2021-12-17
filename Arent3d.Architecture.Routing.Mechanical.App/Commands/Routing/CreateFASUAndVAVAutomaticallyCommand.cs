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
  [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand",
    DefaultString = "Create FASU\nAnd VAV" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateFASUAndVAVAutomaticallyCommand : IExternalCommand
  {
    private const double DistanceBetweenFASUAndVAV = 0.25 ;
    private const string HeightOfFASU = "3100" ;
    private const string HeightOfVAV = "3275" ;
    private const string DiameterOfVAV = "250" ;
    private const int RootBranchNumber = 0 ;
    private const double MinDistanceSpacesCollinear = 2.5 ;
    private const string RoundDuctUniqueId = "dee0da15-198f-4f79-aa08-3ce71203da82-00c0cdcf" ;

    private class NumberOfFASUAndVAVModel
    {
      public int numberOfFASU ;
      public int numberOfVAV ;
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
      IList<Element> spaces = GetAllSpaces( uiDocument.Document )
        .Where( space => space.HasParameter( BranchNumberParameter.BranchNumber ) ).ToArray() ;

      foreach ( var space in spaces ) {
        if ( ! HasBoundingBox( uiDocument.Document, space ) ) {
          return ( false, $"`{space.Name}` have not bounding box." ) ;
        }
      }

      if ( ! RoundDuctTypeExists( uiDocument.Document ) )
        return ( false, $"There no family with UniqueID `{RoundDuctUniqueId}` in the document." ) ;

      ConnectorPicker.IPickResult iPickResult =
        ConnectorPicker.GetConnector( uiDocument, routingExecutor, true,
          "Dialog.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand.PickConnector", null, GetAddInType() ) ;
      if ( iPickResult.PickedConnector != null &&
           CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedConnector, spaces ) ==
           Result.Succeeded ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUとVAVを配置しました。" ) ;
      }

      return ( true, null ) ;
    }

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    private RoutingExecutor CreateRoutingExecutor( Document document, View view ) =>
      AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    private static Result CreateFASUAndVAVAutomatically( Document document, Connector pickedConnector,
      IList<Element> spaces )
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

      if ( ! branchNumberToSpacesDictionary.TryGetValue( 0, out var rootSpaces ) )
      {
        rootSpaces = new List<Element>() ;
      }

      if ( ! IsPreconditionOfFlowDirectionOfFASUAndVAVSatisfied( document, out var vavUpstreamConnectorNormal ) ) return Result.Failed ;

      Dictionary<string, NumberOfFASUAndVAVModel> numberOfFASUsAndVAVsInSpacesDictionary = CountFASUsAndVAVsBySpace( document, spaces ) ;
      if ( ! IsPreconditionOfFASUsAndVAVsSatisfied( numberOfFASUsAndVAVsInSpacesDictionary ) ) return Result.Failed ;
      
      Dictionary<Element, double> rotationAnglesOfFASUsAndVAVs = CalculateRotationAnglesOfFASUsAndVAVs( document,
        branchNumberToSpacesDictionary, pickedConnector, vavUpstreamConnectorNormal ) ;
      
      var parentSpaces = spaces.Where( s => s.GetSpaceBranchNumber() == RootBranchNumber ).ToList() ;
      parentSpaces.Sort( ( a, b ) => CompareDistanceBasisZ( pickedConnector, a, b, false ) ) ;
      var rootSpace = parentSpaces.LastOrDefault() ;
      
      using ( Transaction tr = new(document) ) {
        tr.Start( "Create FASUs and VAVs Automatically" ) ;
        
        // TODO SpaceGroupごとにループを回す. 一直線に並んでいるグループの方向修正のため
        foreach ( var space in spaces ) {
          if ( false == numberOfFASUsAndVAVsInSpacesDictionary.TryGetValue( space.Name, out var numberOfFASUAndVAV ) ) continue ;
          if ( numberOfFASUAndVAV.numberOfFASU == 1 && numberOfFASUAndVAV.numberOfVAV == 1 ) continue ;
          
          BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue;

          var positionOfFASUAndVAV = space == rootSpace ? new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2, pickedConnector.Origin.Y, 0 ) : new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2, ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;
          var placeResult = PlaceFASUAndVAV( document, space.LevelId, positionOfFASUAndVAV, rotationAnglesOfFASUsAndVAVs[ space ] ) ;
          if ( placeResult == null ) continue ;// Failed to place

          var ( instanceOfFASU, instanceOfVAV) = placeResult.Value ;
          
          // この時点でコネクタの向きとは逆を向いている想定
          // コネクタの裏側にあるときは、ここで向きを反転する
          if ( rootSpaces.Contains( space ) && IsVavLocatedBehindConnector( document, instanceOfVAV, pickedConnector ) ) {
            ElementTransformUtils.RotateElements( document, new List<ElementId>(){instanceOfFASU.Id,instanceOfVAV.Id},
              Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI ) ;
          }

          // TODO : 一直線にならんでいるグループの方向修正

          var fasuConnector = instanceOfFASU.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.In ) ;
          var vavConnector = instanceOfVAV.GetConnectors().FirstOrDefault( c => c.Direction == FlowDirectionType.Out ) ;
          if ( fasuConnector != null && vavConnector != null )
            CreateDuctConnectionFASUAndVAV( document, fasuConnector, vavConnector, space.LevelId ) ;
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }
    
    private static int CompareDistanceBasisZ( IConnector rootConnector, Element a, Element b, bool isRotate90 )
    {
      if ( a.Location is not LocationPoint aPos || b.Location is not LocationPoint bPos ) return default ;

      return DistanceFromRoot( rootConnector, aPos, isRotate90 ).CompareTo( DistanceFromRoot( rootConnector, bPos, isRotate90 ) ) ;
    }

    private static double DistanceFromRoot( IConnector rootConnector, LocationPoint targetConnectorPos, bool isRotate90 )
    {
      var rootConnectorPosXyz = rootConnector.Origin ;
      var rootConnectorPos2d = rootConnectorPosXyz.To3dPoint().To2d() ;
      var targetConnector = targetConnectorPos.Point.To3dPoint().To2d() ;

      var rootConnectorBasisZ = rootConnector.CoordinateSystem.BasisZ.To3dPoint().To2d() ;
      var calculateDir = isRotate90 ? new Vector2d( -rootConnectorBasisZ.y, rootConnectorBasisZ.x ) : rootConnectorBasisZ ;
      var rootToVavVector = targetConnector - rootConnectorPos2d ;
      var angle = GetAngleBetweenVector( calculateDir, rootToVavVector ) ;

      return Math.Abs( Math.Cos( angle ) * rootToVavVector.magnitude ) ;
    }

    // Get the angle between two vectors
    private static double GetAngleBetweenVector( Vector2d rootVec, Vector2d otherVector )
    {
      // return the angle (in radian)
      return Math.Acos( Vector2d.Dot( rootVec, otherVector ) / ( rootVec.magnitude * otherVector.magnitude ) ) ;
    }
    
    private static (FamilyInstance instanceOfFASU, FamilyInstance instanceOfVAV)? PlaceFASUAndVAV(Document document, ElementId levelId, XYZ positionOfFASUAndVAV, double rotationAngle)
    {
      var instanceOfFASU = document.AddFASU( positionOfFASUAndVAV, levelId ) ;
      ElementTransformUtils.RotateElement( document, instanceOfFASU.Id,
        Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI / 2 ) ;
      instanceOfFASU.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( HeightOfFASU ) ;

      var instanceOfVAV = document.AddVAV( positionOfFASUAndVAV, levelId ) ;
      instanceOfVAV.LookupParameter( "ダクト径" ).SetValueString( DiameterOfVAV ) ;
      instanceOfVAV.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( HeightOfVAV ) ;

      BoundingBoxXYZ boxOfFASU = instanceOfFASU.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfFASU == null ) return null ;
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return null ;

      // Move the VAV to a distance distanceBetweenFASUAndVAV from FASU
      var distanceBetweenFASUCenterAndVAVCenter = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 + ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 2 + DistanceBetweenFASUAndVAV ;
      ElementTransformUtils.MoveElement( document, instanceOfVAV.Id, new XYZ( distanceBetweenFASUCenterAndVAVCenter, 0, 0 ) ) ;
      
      ElementTransformUtils.RotateElements( document, new List<ElementId>(){instanceOfFASU.Id,instanceOfVAV.Id},
        Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ),
        rotationAngle ) ;

      return ( instanceOfFASU, instanceOfVAV );
    }
    
    private static Dictionary<string, NumberOfFASUAndVAVModel> CountFASUsAndVAVsBySpace ( Document document, IList<Element> spaces )
    {
      var numberOfFASUsAndVAVsInSpacesDictionary = new Dictionary<string, NumberOfFASUAndVAVModel>() ;
      
      var fasus = document.GetAllFamilyInstances( RoutingFamilyType.FASU_F4_150_200Phi )
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F4_150_250Phi ))
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F5_150_250Phi ))
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F6_150_250Phi ))
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F6_150_300Phi ))
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F7_150_300Phi ))
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F8_150_250Phi ))
        .Union(document.GetAllFamilyInstances( RoutingFamilyType.FASU_F8_150_300Phi )) ;
      var fasuInstances = fasus as FamilyInstance[] ?? fasus.ToArray() ;
      var vavs = document.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ) ;
      var vavInstances = vavs as FamilyInstance[] ?? vavs.ToArray() ;
      
      foreach ( var space in spaces )
      {
        BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
        if ( boxOfSpace == null ) continue ;
        
        var numberOfFASUs = 0 ;
        foreach ( var fasuInstance in fasuInstances ) {
          var fasuPosition = fasuInstance.Location as LocationPoint ;
          if ( fasuPosition == null ) continue ;

          if ( IsInSpace( boxOfSpace, fasuPosition.Point ) ) numberOfFASUs++ ;
        }
        
        var numberOfVAVs = 0 ;
        foreach ( var vavInstance in vavInstances ) {
          var vavPosition = vavInstance.Location as LocationPoint ;
          if ( vavPosition == null ) continue ;

          if ( IsInSpace( boxOfSpace, vavPosition.Point ) ) numberOfVAVs++ ;
        }
        
        var numberOfFASUAndVAV = new NumberOfFASUAndVAVModel
        {
          numberOfFASU = numberOfFASUs,
          numberOfVAV = numberOfVAVs
        } ;
        numberOfFASUsAndVAVsInSpacesDictionary.Add( space.Name, numberOfFASUAndVAV ) ;
      }

      return numberOfFASUsAndVAVsInSpacesDictionary ;
    }
    
    private static bool IsPreconditionOfFASUsAndVAVsSatisfied( Dictionary<string, NumberOfFASUAndVAVModel> numberOfFASUsAndVAVsInSpacesDictionary )
    {
      if ( numberOfFASUsAndVAVsInSpacesDictionary.Any( x=> x.Value.numberOfVAV >= 2 || x.Value.numberOfFASU >= 2 ))
      {
        var invalidSpacesList = numberOfFASUsAndVAVsInSpacesDictionary.Where(x => x.Value.numberOfFASU >= 2 || x.Value.numberOfVAV >= 2 )
          .Select(x => x.Key.Substring(0, x.Key.IndexOf(" ", StringComparison.Ordinal))) ;
        TaskDialog.Show( "FASUとVAVの自動配置", $"同一のSpaceに2つ以上のFASU、VAVが存在しているため、処理に失敗しました。 \n該当Space: {string.Join(",", invalidSpacesList)}") ;
        return false ;
      }

      if ( numberOfFASUsAndVAVsInSpacesDictionary.Any(x => x.Value.numberOfVAV == 0 && x.Value.numberOfFASU == 1 ))
      {
        var invalidSpacesList = numberOfFASUsAndVAVsInSpacesDictionary.Where(x => x.Value.numberOfFASU == 1 && x.Value.numberOfVAV == 0 )
          .Select(x => x.Key.Substring(0, x.Key.IndexOf(" ", StringComparison.Ordinal))) ;
        TaskDialog.Show( "FASUとVAVの自動配置", $"以下のSpaceにFASUのみが配置されているため、処理に失敗しました。\n該当Space: {string.Join(",", invalidSpacesList)}") ;
        return false ;
      }
      
      if ( numberOfFASUsAndVAVsInSpacesDictionary.Any(x => x.Value.numberOfVAV == 1 && x.Value.numberOfFASU == 0 ))
      {
        var invalidSpacesList = numberOfFASUsAndVAVsInSpacesDictionary.Where(x => x.Value.numberOfFASU == 0 && x.Value.numberOfVAV == 1 )
          .Select(x => x.Key.Substring(0, x.Key.IndexOf(" ", StringComparison.Ordinal))) ;
        TaskDialog.Show( "FASUとVAVの自動配置", $"以下のSpaceにVAVのみが配置されているため、処理に失敗しました。\n該当Space: {string.Join(",", invalidSpacesList)}") ;
        return false ;
      }

      return true;
    }

    private static bool IsPreconditionOfFlowDirectionOfFASUAndVAVSatisfied( Document document, out Vector3d vavUpstreamConnectorNormal )
    {
      bool fasuInConnectorExists ;
      bool vavInConnectorExists ;
      bool vavOutConnectorExists ;
      vavUpstreamConnectorNormal = new Vector3d( 1, 0, 0 ) ;

      using ( Transaction tr = new(document) ) {
        tr.Start( "Check the flow direction of FASU and VAV" ) ;

        var instanceOfFASU = document.AddFASU( new XYZ( 0, 0, 0 ), ElementId.InvalidElementId ) ;
        var connectorOfFASUInstance = instanceOfFASU.GetConnectors() ;
        fasuInConnectorExists = connectorOfFASUInstance.Any( c => c.Direction == FlowDirectionType.In ) ;

        var instanceOfVAV = document.AddVAV( new XYZ( 0, 0, 0 ), ElementId.InvalidElementId ) ;
        var connectorOfVAVInstance = instanceOfVAV.GetConnectors() ;
        vavInConnectorExists = connectorOfVAVInstance.Any( c => c.Direction == FlowDirectionType.In ) ;
        vavOutConnectorExists = connectorOfVAVInstance.Any( c => c.Direction == FlowDirectionType.Out ) ;
        var inConnectorOfVavInstance = connectorOfVAVInstance.FirstOrDefault( c => c.Direction == FlowDirectionType.In ) ;
        if ( inConnectorOfVavInstance != null )
          vavUpstreamConnectorNormal = inConnectorOfVavInstance.CoordinateSystem.BasisZ.To3dPoint().normalized ;

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
    
    private static Dictionary<Element, double> CalculateRotationAnglesOfFASUsAndVAVs( Document document,
      Dictionary<int, List<Element>> branchNumberDict, Connector rootConnector, Vector3d upstreamConnectorNormal )
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
          rotationAnglesOfFASUsAndVAVs[space] = GetRotationForNonCollinearSpace( rootConnector, spacesCenter, spaceCenter, upstreamConnectorNormal ) ;
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

    private static void CreateDuctConnectionFASUAndVAV( Document document, Connector connectorOfFASU,
      Connector connectorOfVAV, ElementId levelId )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) )
        .WhereElementIsElementType() ;
      var ductType = collector.First( e => e.UniqueId == RoundDuctUniqueId ) ;
      Duct.Create( document, ductType.Id, levelId, connectorOfVAV, connectorOfFASU ) ;
    }

    private static bool RoundDuctTypeExists( Document document )
    {
      FilteredElementCollector collector = new FilteredElementCollector( document ).OfClass( typeof( DuctType ) ) ;
      return collector.Any( e => e.UniqueId == RoundDuctUniqueId ) ;
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

    private static double GetRotationForCollinearSpaces( Document document, Connector rootConnector,
      IReadOnlyList<Element> spaces, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorOrigin = rootConnector.Origin.To3dPoint() ;
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;

      var spaceBoxes = spaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
      var centerOfSpaces = spaceBoxes.UnionBounds()!.Value.Center ;

      // RootConnectorの法線方向基準で、RootConnectorより奥にある場合は、上流につなげるConnectorの向きを法線方向とは逆向きにする.
      var sign = Vector3d.Dot( centerOfSpaces - rootConnectorOrigin, rootConnectorNormal ) > 0 ? -1 : 1 ;
      return CalcRadianAngle2D( upstreamConnectorNormal, sign * rootConnectorNormal ) ;
    }

    private static double GetRotationForNonCollinearSpace( Connector rootConnector,
      Vector3d spaceGroupCenter, Vector3d spaceCenter, Vector3d upstreamConnectorNormal )
    {
      var rootConnectorNormal = rootConnector.CoordinateSystem.BasisZ.To3dDirection() ;
      var sign = Vector3d.Dot( spaceGroupCenter - spaceCenter, rootConnectorNormal ) > 0 ? 1 : -1 ;
      return CalcRadianAngle2D( upstreamConnectorNormal, sign * rootConnectorNormal ) ;
    }

    private static bool AreSpacesCollinear( Document document, IReadOnlyList<Element> spaces,
      Vector3d checkTargetDir2D )
    {
      var orthogonalToTargetDir = new Vector3d( checkTargetDir2D.y, -checkTargetDir2D.x, 0.0 ) ;

      var spaceBoxes = spaces.Select( space => space.get_BoundingBox( document.ActiveView ).ToBox3d() ).ToArray() ;
      var centerOfSpaces = spaceBoxes.UnionBounds()!.Value.Center ;

      return spaceBoxes.Select( box => box.Center )
        .All( center => Math.Abs( Vector3d.Dot( centerOfSpaces - center, orthogonalToTargetDir ) ) <
                        MinDistanceSpacesCollinear ) ;
    }

    private static bool IsVavLocatedBehindConnector( Document document, Element instanceOfVAV,
      Connector instanceOfConnector )
    {
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;

      var connectorPosition = instanceOfConnector.Origin.To3dPoint() ;
      var connectorNormal = instanceOfConnector.CoordinateSystem.BasisZ.To3dDirection() ;

      // コネクタの向いている方向の成分で比較したときに、VAVのBoxの角が1つでもコネクタ位置よりも小さければ後方とみなす.
      return boxOfVAV.ToBox3d().Vertices()
        .Any( boxCorner => Vector3d.Dot( boxCorner - connectorPosition, connectorNormal ) < 0 ) ;
    }

    #endregion
  }
}