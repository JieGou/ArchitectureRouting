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
using System.Linq ;

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

    private enum RotationAxis
    {
      XAxis,
      YAxis
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
      ConnectorPicker.IPickResult iPickResult =
        ConnectorPicker.GetConnector( uiDocument, routingExecutor, true,
          "Dialog.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand.PickConnector", null, GetAddInType() ) ;
      if ( iPickResult.PickedConnector != null && CreateFASUAndVAVAutomatically( uiDocument.Document, iPickResult.PickedConnector ) == Result.Succeeded ) {
        TaskDialog.Show( "FASUとVAVの自動配置", "FASUとVAVを配置しました。" ) ;
      }

      return ( true, null ) ;
    }

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    private RoutingExecutor CreateRoutingExecutor( Document document, View view ) =>
      AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    private static Result CreateFASUAndVAVAutomatically( Document document, Connector pickedConnector )
    {
      // Get all the spaces in the document
      IList<Element> spaces = GetAllSpaces( document ) ;

      // Get branch number　property of spaces
      IList<int> listOfAllBranchNumbers = spaces.Select( space =>
          space.TryGetProperty( BranchNumberParameter.BranchNumber, out int branchNumber ) ? branchNumber : -1 )
        .ToArray() ;

      // Calculate direction for FASU and VAV inside the space
      List<double> FASUAndVAVDirectionInformations = CalculateDirectionForFASUsAndVAVs( document, spaces,
        listOfAllBranchNumbers, pickedConnector ) ;

      // Start Transaction
      using ( Transaction tr = new(document) ) {
        tr.Start( "Create FASUs and VAVs Automatically" ) ;
        foreach ( var space in spaces.Select( ( value, index ) => new { value, index } ) ) {
          // Add object to the document
          BoundingBoxXYZ boxOfSpace = space.value.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;

          var positionOfFASUAndVAV = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
            ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;

          // Add FASU to document
          var instanceOfFASU = document.AddFASU( positionOfFASUAndVAV, space.value.LevelId ) ;
          ElementTransformUtils.RotateElement( document, instanceOfFASU.Id,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI / 2 ) ;
          instanceOfFASU.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( HeightOfFASU ) ;

          // Add VAV to document
          var instanceOfVAV = document.AddVAV( positionOfFASUAndVAV, space.value.LevelId ) ;
          instanceOfVAV.LookupParameter( "ダクト径" ).SetValueString( DiameterOfVAV ) ;
          instanceOfVAV.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( HeightOfVAV ) ;

          // Get BoundingBox of FASU and VAV
          BoundingBoxXYZ boxOfFASU = instanceOfFASU.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfFASU == null ) continue ;
          BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfVAV == null ) continue ;

          // Move the VAV to a distance distanceBetweenFASUAndVAV from FASU
          double distanceBetweenFASUCenterAndVAVCenter ;
          distanceBetweenFASUCenterAndVAVCenter = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 +
                                                  ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 2 +
                                                  DistanceBetweenFASUAndVAV ;
          ElementTransformUtils.MoveElement( document, instanceOfVAV.Id,
            new XYZ( distanceBetweenFASUCenterAndVAVCenter, 0, 0 ) ) ;

          // Check the condition if the rotation touches the connector or not?
          if ( CheckVAVTouchingConnector( document, instanceOfVAV, pickedConnector, positionOfFASUAndVAV,
            FASUAndVAVDirectionInformations[ space.index ] ) ) continue ;

          // Rotate FASU and VAV
          List<ElementId> idOfFASUAndVAV = new List<ElementId>() ;
          idOfFASUAndVAV.Add( instanceOfFASU.Id ) ;
          idOfFASUAndVAV.Add( instanceOfVAV.Id ) ;
          ElementTransformUtils.RotateElements( document, idOfFASUAndVAV,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ),
            FASUAndVAVDirectionInformations[ space.index ] ) ;
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }

    private static List<double> CalculateDirectionForFASUsAndVAVs( Document document, IList<Element> spaces,
      IList<int> listAllOfBranchNumber , Connector pickedConnector )
    {
      // Initialize to initial value
      List<double> directionForFASUsAndVAVs = new List<double>() ;
      for ( int i = 0 ; i < spaces.Count ; i++ ) {
        directionForFASUsAndVAVs.Add( 0 ) ;
      }

      // Determine AHU coincides with the axis Ox or Oy
      var rotationAxis = GetRotationAxis( pickedConnector ) ;

      // Process by group BranchNumber
      foreach ( var handleBranchNumber in listAllOfBranchNumber.Select( ( value, index ) => new { value, index } ) ) {
        List<int> branchNumbers = GetBranchNumbers( listAllOfBranchNumber, handleBranchNumber.index ) ;
        if ( branchNumbers.Count == 0 ) continue ;

        // Separate handling for rootBranchNumber (default value == 0)
        if ( handleBranchNumber.index == RootBranchNumber ) {
          foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
            XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
            directionForFASUsAndVAVs[ branchNumber.value ] =
              GetDirectionForFASUAndVAV( pickedConnector.Origin, centerOfSpace, rotationAxis ) ;
          }

          continue ;
        }

        // Get center of spaces group
        XYZ centerPointOfSpacesGroup = GetCenterPointOfSpacesGroup( document, spaces, branchNumbers ) ;
        
        // Are the spaces collinear
        bool areTheSpacesCollinear =
          GetRotationAngleSpacesCollinear( document, spaces, branchNumbers, centerPointOfSpacesGroup, rotationAxis ) ;

        // Calculate rotation angle of FASU and VAV in each space
        foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
          if ( areTheSpacesCollinear ) {
            directionForFASUsAndVAVs[ branchNumber.value ] = GetDirectionForFASUAndVAV( pickedConnector.Origin,
              centerPointOfSpacesGroup, rotationAxis ) ;
          }
          else {
            XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
            directionForFASUsAndVAVs[ branchNumber.value ] =
              GetDirectionForFASUAndVAV( centerPointOfSpacesGroup, centerOfSpace, rotationAxis ) ;
          }
        }
      }

      return directionForFASUsAndVAVs ;
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

    private static XYZ GetCenterPointOfSpacesGroup( Document document, IList<Element> spaces, List<int> branchNumbers )
    {
      XYZ maxOfSpaces = new XYZ(), minOfSpaces = new XYZ() ;
      foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
        XYZ centerOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
        if ( branchNumber.index == 0 ) {
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

      XYZ centerPointOfSpacesGroup = new XYZ( ( maxOfSpaces.X + minOfSpaces.X ) / 2,
        ( maxOfSpaces.Y + minOfSpaces.Y ) / 2,
        ( maxOfSpaces.Z + minOfSpaces.Z ) / 2 ) ;
      return centerPointOfSpacesGroup ;
    }

    private static XYZ GetCenterSpace( Document document, Element instanceOfSpace )
    {
      BoundingBoxXYZ boxOfSpace = instanceOfSpace.get_BoundingBox( document.ActiveView ) ;
      return new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
        ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2,
        ( boxOfSpace.Max.Z + boxOfSpace.Min.Z ) / 2 ) ;
    }

    private static RotationAxis GetRotationAxis( Connector pickedConnector )
    {
      var rotation = ( pickedConnector.Owner.Location as LocationPoint )!.Rotation ;
      if ( Math.Abs( Math.Cos( rotation ) ) >= Math.Cos( Math.PI / 4 ) ) {
        return RotationAxis.XAxis ;
      }
      else {
        return RotationAxis.YAxis ;
      }
    }

    private static double GetDirectionForFASUAndVAV( XYZ centerPointOfSpacesGroup, XYZ centerOfSpace,
      RotationAxis axisOfRotation )
    {
      if ( axisOfRotation == RotationAxis.XAxis ) {
        if ( centerOfSpace.X <= centerPointOfSpacesGroup.X ) {
          return 0 ;
        }
        return Math.PI ;
      }

      if ( centerOfSpace.Y <= centerPointOfSpacesGroup.Y ) {
        return 0.5 * Math.PI ;
      }
      return 1.5 * Math.PI ;
    }

    private static bool GetRotationAngleSpacesCollinear( Document document, IList<Element> spaces,
      List<int> branchNumbers, XYZ centerPointOfSpacesGroup, RotationAxis rotationAxis )
    {
      if ( branchNumbers.Count == 0 ) return false;
      foreach ( var branchNumber in branchNumbers.Select( ( value, index ) => new { value, index } ) ) {
        XYZ centerPointOfSpace = GetCenterSpace( document, spaces[ branchNumber.value ] ) ;
        if ( rotationAxis == RotationAxis.XAxis ) {
          if ( Math.Abs( centerPointOfSpacesGroup.X - centerPointOfSpace.X ) > MinDistanceSpacesCollinear ) {
            return false;
          }
        }
        else {
          if ( Math.Abs( centerPointOfSpacesGroup.Y - centerPointOfSpace.Y ) > MinDistanceSpacesCollinear ) {
            return false ;
          }
        }
      }
      return true ;
    }

    private static bool CheckVAVTouchingConnector( Document document, Element instanceOfVAV,
      Connector instanceOfConnector, XYZ pointOfRotation, double rotationAngle )
    {
      // Get BoundingBox of VAV
      BoundingBoxXYZ boxOfVAV = instanceOfVAV.get_BoundingBox( document.ActiveView ) ;
      if ( boxOfVAV == null ) return false ;

      // Determine connector (AHU) coincides with the axis Ox or Oy
      RotationAxis axisOfRotation = GetRotationAxis( instanceOfConnector ) ;
      ( double pointOfMinBoxVAV, double pointOfMaxBoxVAV ) =
        GetMinMaxBoxRotatingObject( boxOfVAV, pointOfRotation, rotationAngle, axisOfRotation ) ;

      if ( axisOfRotation == 0 ) {
        if ( instanceOfConnector.Origin.X >= pointOfMinBoxVAV && instanceOfConnector.Origin.X <= pointOfMaxBoxVAV ) {
          return true ;
        }
      }
      else {
        if ( instanceOfConnector.Origin.Y >= pointOfMinBoxVAV && instanceOfConnector.Origin.Y <= pointOfMaxBoxVAV ) {
          return true ;
        }
      }

      return false ;
    }

    private static (double, double) GetMinMaxBoxRotatingObject( BoundingBoxXYZ boxOfRotatingObject, XYZ pointOfRotation,
      double rotationAngle, RotationAxis axisOfRotation )
    {
      if ( axisOfRotation == RotationAxis.XAxis ) {
        if ( Math.Cos( rotationAngle ) > 0 ) {
          return ( boxOfRotatingObject.Min.X, boxOfRotatingObject.Max.X ) ;
        }
        else {
          return ( pointOfRotation.X - Math.Abs( pointOfRotation.X - boxOfRotatingObject.Max.X ),
            pointOfRotation.X + Math.Abs( pointOfRotation.X - boxOfRotatingObject.Min.X ) ) ;
        }
      }
      else {
        if ( Math.Sin( rotationAngle ) > 0 ) {
          return ( boxOfRotatingObject.Min.Y, boxOfRotatingObject.Max.Y ) ;
        }
        else {
          return ( pointOfRotation.Y - Math.Abs( pointOfRotation.Y - boxOfRotatingObject.Max.Y ),
            pointOfRotation.Y + Math.Abs( pointOfRotation.Y - boxOfRotatingObject.Min.Y ) ) ;
        }
      }
    }
  }
}