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
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // Get all the spaces in the document
      IList<Element> spaces = GetAllSpaces( document ) ;

      // Get FASU height
      double heightOfFASU = GetHeightFASU( document, "Common 45 deg" ) ?? 385 ;

      // Start Transaction
      using ( Transaction tr = new Transaction( document ) ) {
        tr.Start( "Create FASU and VAV Automatically Guide Command" ) ;
        foreach ( var space in spaces ) {
          // Add object to the document
          BoundingBoxXYZ boxOfSpace = space.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfSpace == null ) continue ;

          // FASU object
          var locationOfFASU = new XYZ( ( boxOfSpace.Max.X + boxOfSpace.Min.X ) / 2,
            ( boxOfSpace.Max.Y + boxOfSpace.Min.Y ) / 2, heightOfFASU ) ;
          var fasuInstance = document.AddFASU( locationOfFASU, space.LevelId ) ;
          ElementTransformUtils.RotateElement( document, fasuInstance.Id,
            Line.CreateBound( locationOfFASU, locationOfFASU + XYZ.BasisZ ), Math.PI / 2 ) ;
          fasuInstance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).SetValueString( "3100" ) ;

          // VAV object 
          BoundingBoxXYZ boxOfFASU = fasuInstance.get_BoundingBox( document.ActiveView ) ;
          if ( boxOfFASU == null ) continue ;

          var locationOfVAV = new XYZ( ( boxOfFASU.Max.X + boxOfFASU.Min.X ) / 2,
            ( boxOfFASU.Max.Y + boxOfFASU.Min.Y ) / 2, heightOfFASU ) ;
          double distance = ( boxOfFASU.Max.X - boxOfFASU.Min.X ) / 2 ;
          FamilyInstance vavInstance = document.AddVAV( locationOfVAV, space.LevelId ) ;
          vavInstance.LookupParameter( "ダクト径" ).SetValueString( "250" ) ;
          BoundingBoxXYZ boxOfVAV = vavInstance.get_BoundingBox( document.ActiveView ) ;
          distance += ( boxOfVAV.Max.X - boxOfVAV.Min.X ) / 4 ;
          ElementTransformUtils.MoveElement( document, vavInstance.Id, new XYZ( distance, 0, 0 ) ) ;
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

    private static double? GetHeightFASU( Document document, string nameOfFASU )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_DuctFitting) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> ducts = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      foreach ( var duct in ducts ) {
        if ( duct.Name.IndexOf( nameOfFASU, 0, StringComparison.Ordinal ) == -1 ) continue ;
        var locationPoint = ( duct.Location as LocationPoint )! ;
        return locationPoint.Point.Z ;
      }

      return null ;
    }
  }
}