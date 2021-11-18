using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using System ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyCommand",
    DefaultString = "Create FASU\nAnd VAV" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateFASUAndVAVAutomaticallyCommand : IExternalCommand
  {
    private const double distanceBetweenFASUAndVAV = 0.25;
    private const string heightOfFASU = "3100" ;
    private const string heightOfVAV = "3275" ;
    private const string diameterOfVAV = "250" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // Get all the spaces in the document
      IList<Element> spaces = GetAllSpaces( document ) ;

      // Start Transaction
      using ( Transaction tr = new Transaction( document ) ) {
        tr.Start( "Create FASUs and VAVs Automatically Command" ) ;
        foreach ( var space in spaces ) {
          // Add object to the document
          BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;
          
          // Get position FASU and VAV
          var positionOfFASUAndVAV = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
            ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;

          // Add FASU to document
          var instanceOfFASU = document.AddFASU( positionOfFASUAndVAV, space.LevelId ) ;
          ElementTransformUtils.RotateElement( document, instanceOfFASU.Id,
            Line.CreateBound( positionOfFASUAndVAV, positionOfFASUAndVAV + XYZ.BasisZ ), Math.PI / 2 ) ;
          instanceOfFASU.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( heightOfFASU ) ;
          
          // Add VAV to document
          var instanceOfVAV = document.AddVAV( positionOfFASUAndVAV, space.LevelId ) ;
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
        }

        tr.Commit() ;
      }

      return Result.Succeeded ;
    }

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }
  }
}