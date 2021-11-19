using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using System ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using System.Linq;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand",
    DefaultString = "Create FASU\nAnd VAV" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateFASUAndVAVAutomaticallyCommand : PickRoutingCommandBase
  {
    private const double distanceBetweenFASUAndVAV = 0.25;
    private const string heightOfFASU = "3100" ;
    private const string heightOfVAV = "3275" ;
    private const string diameterOfVAV = "250" ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      ConnectorPicker.IPickResult iPickResult =
        ConnectorPicker.GetConnector( uiDocument, routingExecutor, "Common ", null, GetAddInType() ) ;
      if ( CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedElement ) == Result.Succeeded ) {
        TaskDialog.Show( "自動生成", "FASUとVAVの自動生成に成功" ) ;
      }

      return ( true, "Result.Succeeded" ) ;
    }

    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand" ;
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) =>
      AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments
      ) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult,
        ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty,
        MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom )
    {
      return ( PickCommandUtil.CreateRouteEndPoint( newPickResult ), null ) ;
    }

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document,
      Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      if ( RouteMEPSystem.GetSystemType( document, connector ) is not { } defaultSystemType ) return null ;

      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, defaultSystemType ) ;

      return new DialogInitValues( classificationInfo, defaultSystemType, curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) =>
      systemType?.Name ?? curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType(
      MEPSystemType? systemType )
    {
      if ( null == systemType ) return null ;
      return MEPSystemClassificationInfo.From( systemType! ) ;
    }
    
    private static Result CreateFASUAndVAVAutomatically( Document document, Element element )
    {
      // Get all the spaces in the document
      IList<Element> spaces = GetAllSpaces( document ) ;
      
      // Get branch number in spaces
      List<int> listAllOfBranchNumber = new List<int>() ;
      foreach ( var space in spaces ) {
        if ( space.HasParameter( BranchNumberParameter.BranchNumber ) ) {
          if ( space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) )
            listAllOfBranchNumber.Add( branchNumber ) ;
          else {
            listAllOfBranchNumber.Add( -1 ) ;
          }
        }
        else {
          listAllOfBranchNumber.Add( -1 ) ;
        }
      }

      // Calculation rotation of all spaces
      List<double> listAllRotationOfSpaces = CalculationSpaceRotation(document, spaces, listAllOfBranchNumber, element);

      // Start Transaction
      using ( Transaction tr = new Transaction( document ) ) {
        tr.Start( "Create FASUs and VAVs Automatically Command" ) ;
        foreach ( var space in spaces.Select( ( value, index ) => new { value, index } ) ) {
          // Add object to the document
          BoundingBoxXYZ boxOfSpace = space.value.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;
          
          // Get position FASU and VAV
          var positionOfFASUAndVAV = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
            ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;
          
          // Add FASU to document
          var instanceOfFASU = document.AddFASU( positionOfFASUAndVAV, space.value.LevelId ) ;
          ElementTransformUtils.RotateElement( document, instanceOfFASU.Id,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI / 2 ) ;
          instanceOfFASU.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( heightOfFASU ) ;
          
          // Add VAV to document
          var instanceOfVAV = document.AddVAV( positionOfFASUAndVAV, space.value.LevelId ) ;
          instanceOfVAV.LookupParameter( "ダクト径" ).SetValueString( diameterOfVAV ) ;
          instanceOfVAV.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( heightOfVAV ) ;
          
          // Get BoundingBox of FASU and VAV
          BoundingBoxXYZ boxOfFASU = instanceOfFASU.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfFASU == null ) continue ;
          BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfVAV == null ) continue ;
          
          // Move VAV
          double totalOfBoxHalfWidth = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 + ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 2 ;
          ElementTransformUtils.MoveElement( document, instanceOfVAV.Id, new XYZ( totalOfBoxHalfWidth + distanceBetweenFASUAndVAV, 0, 0 ) ) ;

          // Rotate FASU and VAV
          List<ElementId> idOfFASUAndVAV = new List<ElementId>();
          idOfFASUAndVAV.Add(instanceOfFASU.Id);
          idOfFASUAndVAV.Add(instanceOfVAV.Id);
          ElementTransformUtils.RotateElements(document, idOfFASUAndVAV, Line.CreateBound(positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ), listAllRotationOfSpaces[space.index]);
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }
    private static List<double> CalculationSpaceRotation( Document document, IList<Element> spaces, IList<int> listAllOfBranchNumber, Element element )
    {
      // Initialize to initial value
      List<double> listAllRotationOfSpaces = new List<double>() ;
      for ( int i = 0 ; i < spaces.Count ; i++ ) {
        listAllRotationOfSpaces.Add( 0 ) ;
      }

      // Determine AHU coincides with the axis Ox or Oy
      var rotationCommon = ( element.Location as LocationPoint )!.Rotation ;
      int axisOfRotation = GetAxisRotation( rotationCommon ) ;

      // Process by group BranchNumber
      foreach ( var branchNumber in listAllOfBranchNumber.Select( ( value, index ) => new { value, index } ) ) {
        List<int> groupOfBranchNumberSpaces = GetBranchNumberSpaces( listAllOfBranchNumber, branchNumber.index ) ;
        if ( groupOfBranchNumberSpaces.Count == 0 ) continue ;
        
        // Separate handling for BranchNumber = 0
        if ( branchNumber.index == 0 ) {
          foreach ( var branchNumberSpace in groupOfBranchNumberSpaces.Select( ( value, index ) => new { value, index } ) ) {
            XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumberSpace.value ] ) ;
            listAllRotationOfSpaces[ branchNumberSpace.value ] =
              GetRotationSpace( ( element.Location as LocationPoint )!.Point, centerOfSpace, axisOfRotation ) ;
          }

          continue ;
        }

        // Get min-max of group BranchNumber
        XYZ maxOfSpaces = new XYZ(), minOfSpaces = new XYZ() ;
        foreach ( var branchNumberSpace in groupOfBranchNumberSpaces.Select( ( value, index ) => new { value, index } ) ) {
          XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumberSpace.value ] ) ;
          if (branchNumberSpace.index == 0){
            maxOfSpaces = centerOfSpace ;
            minOfSpaces = centerOfSpace ;
          }
          else {
            if ( maxOfSpaces.X < centerOfSpace.X )
              maxOfSpaces = new XYZ( centerOfSpace.X, maxOfSpaces.Y, maxOfSpaces.Z ) ;
            if ( maxOfSpaces.Y < centerOfSpace.Y )
              maxOfSpaces = new XYZ( maxOfSpaces.X, centerOfSpace.Y, maxOfSpaces.Z ) ;
            if ( maxOfSpaces.Z < centerOfSpace.Z )
              maxOfSpaces = new XYZ( maxOfSpaces.X, maxOfSpaces.Y, centerOfSpace.Z ) ;
            if ( minOfSpaces.X > centerOfSpace.X )
              minOfSpaces = new XYZ( centerOfSpace.X, minOfSpaces.Y, minOfSpaces.Z ) ;
            if ( minOfSpaces.Y > centerOfSpace.Y )
              minOfSpaces = new XYZ( minOfSpaces.X, centerOfSpace.Y, minOfSpaces.Z ) ;
            if ( minOfSpaces.Z > centerOfSpace.Z )
              minOfSpaces = new XYZ( minOfSpaces.X, minOfSpaces.Y, centerOfSpace.Z ) ;
          }
        }

        XYZ centerOfGroupSpaces = new XYZ( ( maxOfSpaces.X + minOfSpaces.X ) / 2, ( maxOfSpaces.Y + minOfSpaces.Y ) / 2,
          ( maxOfSpaces.Z + minOfSpaces.Z ) / 2 ) ;

        // Calculate rotation angle of FASU and VAV in each space
        foreach ( var branchNumberSpace in groupOfBranchNumberSpaces.Select( ( value, index ) => new { value, index } ) ) {
          XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumberSpace.value ] ) ;
          listAllRotationOfSpaces[ branchNumberSpace.value ] = GetRotationSpace( centerOfGroupSpaces, centerOfSpace, axisOfRotation) ;
        }
      }

      return listAllRotationOfSpaces ;
    }

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }
    
    private static List<int> GetBranchNumberSpaces( IList<int> listAllBranchNumber, int valueOfBranchNumber )
    {
      List<int> groupOfBranchNumberSpaces = new List<int>() ;
      foreach ( var branchNumberOfSpace in listAllBranchNumber.Select( ( value, index ) => new { value, index } ) ) {
        if ( branchNumberOfSpace.value == valueOfBranchNumber ) {
          groupOfBranchNumberSpaces.Add( branchNumberOfSpace.index ) ;
        }
      }

      return groupOfBranchNumberSpaces ;
    }

    private static XYZ GetCenterSpace( Document document, Element space )
    {
      BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
      return new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2, 
                      ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2,
                      ( boxOfSpace.Max.Z + boxOfSpace.Min.Z ) / 2 ) ;
    }

    private static int GetAxisRotation( double rotation )
    {
      if ( Math.Abs( Math.Cos( rotation ) ) >= Math.Cos( Math.PI / 4 ) ) {
        return 0 ;
      }
      else {
        return 1 ;
      }
    }
    
    private static double GetRotationSpace( XYZ centerOfGroupSpaces, XYZ centerOfSpace, int axisOfRotation)
    {
      if ( axisOfRotation == 0 ) {
        if ( centerOfGroupSpaces.X <= centerOfSpace.X ) {
          return Math.PI ;
        }
        else {
          return 0 ;
        }
      }
      else {
        if ( centerOfGroupSpaces.Y <= centerOfSpace.Y ) {
          return 1.5 * Math.PI ;
        }
        else {
          return Math.PI / 2 ;
        }
      }
    }
  }
}