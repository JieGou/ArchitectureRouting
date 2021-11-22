using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using System ;
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
  public class CreateFASUAndVAVAutomaticallyCommand : PickFASUAndVAVAutomaticallyCommandBase
  {
    private const double distanceBetweenFASUAndVAV = 0.25;
    private const string heightOfFASU = "3100" ;
    private const string heightOfVAV = "3275" ;
    private const string diameterOfVAV = "250" ;
    private const int rootBranchNumber = 0 ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      ConnectorPicker.IPickResult iPickResult =
        ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Common ", null, GetAddInType() ) ;
      if ( CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedElement ) == Result.Succeeded ) {
        TaskDialog.Show( "自動生成", "FASUとVAVの自動生成に成功" ) ;
      }

      return ( true, null ) ;
    }
    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) =>
      AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    private static Result CreateFASUAndVAVAutomatically( Document document, Element element )
    {
      // Get all the spaces in the document
      IList<Element> spaces = GetAllSpaces( document ) ;
      
      // Get branch number in spaces
      IList<int> listAllOfBranchNumber = spaces.Select( space =>
        space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ? branchNumber : -1 ).ToArray() ;
      
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
          double distanceBetweenFASUCenterAndVAVCenter = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 +
                                                         ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 2 +
                                                         distanceBetweenFASUAndVAV ;
          ElementTransformUtils.MoveElement( document, instanceOfVAV.Id, new XYZ( distanceBetweenFASUCenterAndVAVCenter, 0, 0 ) ) ;

          // Rotate FASU and VAV
          List<ElementId> idOfFASUAndVAV = new List<ElementId>() ;
          idOfFASUAndVAV.Add( instanceOfFASU.Id ) ;
          idOfFASUAndVAV.Add( instanceOfVAV.Id ) ;
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
      foreach ( var handleBranchNumber in listAllOfBranchNumber.Select( ( value, index ) => new { value, index } ) ) {
        List<int> branchNumbers = GetBranchNumbers( listAllOfBranchNumber, handleBranchNumber.index ) ;
        if ( branchNumbers.Count == 0 ) continue ;
        
        // Separate handling for rootBranchNumber (default value == 0)
        if ( handleBranchNumber.index == rootBranchNumber ) {
          foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
            XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
            listAllRotationOfSpaces[ branchNumber.value ] =
              GetRotationSpace( ( element.Location as LocationPoint )!.Point, centerOfSpace, axisOfRotation ) ;
          }

          continue ;
        }

        // Get center of spaces group
        XYZ centerPointOfSpacesGroup = GetCenterSpacesGroup( document, spaces, branchNumbers ) ;

        // Calculate rotation angle of FASU and VAV in each space
        foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
          XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
          listAllRotationOfSpaces[ branchNumber.value ] = GetRotationSpace( centerPointOfSpacesGroup, centerOfSpace, axisOfRotation) ;
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
    
    private static List<int> GetBranchNumbers( IList<int> listAllBranchNumber, int valueOfBranchNumber )
    {
      List<int> groupOfBranchNumberSpaces = new List<int>() ;
      foreach ( var branchNumberOfSpace in listAllBranchNumber.Select( ( value, index ) => new { value, index } ) ) {
        if ( branchNumberOfSpace.value == valueOfBranchNumber ) {
          groupOfBranchNumberSpaces.Add( branchNumberOfSpace.index ) ;
        }
      }

      return groupOfBranchNumberSpaces ;
    }

    private static XYZ GetCenterSpacesGroup(Document document, IList<Element> spaces, List<int> branchNumbers)
    {
      XYZ maxOfSpaces = new XYZ(), minOfSpaces = new XYZ() ;
      foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
        XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
        if (branchNumber.index == 0){
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

      XYZ  centerPointOfSpacesGroup = new XYZ( ( maxOfSpaces.X + minOfSpaces.X ) / 2,
                                               ( maxOfSpaces.Y + minOfSpaces.Y ) / 2,
                                               ( maxOfSpaces.Z + minOfSpaces.Z ) / 2 ) ;
      return  centerPointOfSpacesGroup ;
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
          return 0.5 * Math.PI;
        }
      }
    }
  }
}