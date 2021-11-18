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
    private const double distanceBetweenFASUAndVAV = 2.5;
    private const string hightOfFASU = "3100" ;
    private const string hightOfVAV = "3275" ;
    private const string diameterOfVAV = "250" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // Get all the spaces in the document
      IList<Element> spaces = GetAllSpaces( document ) ;

      // Start Transaction
      using ( Transaction tr = new Transaction( document ) ) {
        tr.Start( "Create FASU and VAV Automatically Command" ) ;
        foreach ( var space in spaces ) {
          // Add object to the document
          BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;

          // FASU object
          var positionOfFASU = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
            ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, 0 ) ;
          var fasuInstance = document.AddFASU( positionOfFASU, space.LevelId ) ;
          ElementTransformUtils.RotateElement( document, fasuInstance.Id,
            Line.CreateBound( positionOfFASU, positionOfFASU + XYZ.BasisZ ), Math.PI / 2 ) ;
          fasuInstance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( hightOfFASU ) ;

          // VAV object 
          BoundingBoxXYZ boxOfFASU = fasuInstance.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfFASU == null ) continue ;

          var positionOfVAV = new XYZ( ( boxOfFASU.Max.X + boxOfFASU.Min.X ) / 2,
            ( boxOfFASU.Max.Y + boxOfFASU.Min.Y ) / 2, 0 ) ;
          FamilyInstance vavInstance = document.AddVAV( positionOfVAV, space.LevelId ) ;
          vavInstance.LookupParameter( "ダクト径" ).SetValueString( diameterOfVAV ) ;
          vavInstance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( hightOfVAV ) ;
          ElementTransformUtils.MoveElement( document, vavInstance.Id, new XYZ( distanceBetweenFASUAndVAV, 0, 0 ) ) ;
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