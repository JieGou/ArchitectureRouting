using System;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic;
using Arent3d.Architecture.Routing.FASU ;
using Arent3d.Architecture.Routing.VAV ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using System.Linq;
using Arent3d.Revit;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure ;

using Arent3d.Revit.I18n ;
using Arent3d.Utility;


namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateAutoFasuGuideCommand", DefaultString = "Create Auto\nFASU" )]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateAutoFasuGuideCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            //Get all the spaces in the document
            IList<Element> spaces = GetSpaces(document);
            
            // Get fasu height
            double heightFasu = 0;
            bool bHeightFasu = GetHeightFasu(document, ref heightFasu);

            // Start Transaction
            using (Transaction tr = new Transaction(document))
            {
                tr.Start( "Create Auto Fasu and Vav Guide" );
                foreach (var space in spaces)
                {
                    // Add object to the document
                    BoundingBoxXYZ boxSpace = space.get_BoundingBox(document.ActiveView);
                    if (boxSpace != null)
                    {
                        // Fasu object
                        if (!bHeightFasu) heightFasu = (boxSpace.Max.Z + boxSpace.Min.Z) / 2;
                        var locationFasu = new XYZ((boxSpace.Max.X + boxSpace.Min.X) / 2, (boxSpace.Max.Y + boxSpace.Min.Y) / 2, heightFasu);
                        var fasuInstance = document.AddFASU(FASUType.F8_150_250Phi, locationFasu, null, Math.PI/2);

                        // Vav object 
                        BoundingBoxXYZ boxFasu = fasuInstance.get_BoundingBox(document.ActiveView);
                        if (boxFasu != null)
                        {
                            if (!bHeightFasu) heightFasu = (boxFasu.Max.Z + boxFasu.Min.Z) / 2;
                            var locVav = new XYZ((boxFasu.Max.X + boxFasu.Min.X) / 2, (boxFasu.Max.Y + boxFasu.Min.Y) / 2, heightFasu);
                            //var locationPoint = ( fasuInstance.Location as LocationPoint )! ;
                            double distance = (boxFasu.Max.X - boxFasu.Min.X) / 2;
                            FamilyInstance vavInstance = document.AddVAV(VAVType.TTE_VAV_Maru, locVav, null, distance);
                        }
                    }
                } 
                tr.Commit();
            }
            
            return Result.Succeeded;
        }
        private IList<Element> GetSpaces(Document document)
        {
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaces);
            FilteredElementCollector collector = new FilteredElementCollector(document);
            IList<Element> spaces = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            return spaces;
        }
        private bool GetHeightFasu(Document document, ref double height)
        {
            bool brc = false;
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_DuctAccessory);
            FilteredElementCollector collector = new FilteredElementCollector(document);
            IList<Element> ducts = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            foreach (var duct in ducts)
            {
                if (duct.Name.IndexOf("FASU", 0) != -1)
                {
                    var locationPoint = ( duct.Location as LocationPoint )! ;
                    height = locationPoint.Point.Z;
                    brc = true;
                    break;
                }
            }

            return brc;
        }
        private static void SetVavWidth( FamilyInstance familyInstance, double xWidth)
        {
            var document = familyInstance.Document ;
            SetValue( familyInstance, "Revit.Property.Duct.Size".GetDocumentStringByKeyOrDefault( document, null ), xWidth ) ;
        }
        private static void SetValue( FamilyInstance familyInstance, string parameterName, double value )
        {
            if ( familyInstance.LookupParameter( parameterName ) is not { } parameter ) return ;
            if ( StorageType.Double != parameter.StorageType ) return ;

            parameter.Set( value ) ;
        }
    }
}