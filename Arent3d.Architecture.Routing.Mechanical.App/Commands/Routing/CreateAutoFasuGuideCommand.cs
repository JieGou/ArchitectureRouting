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
                        var locationFasu = new XYZ((boxSpace.Max.X + boxSpace.Min.X) / 2, (boxSpace.Max.Y + boxSpace.Min.Y) / 2, (boxSpace.Max.Z + boxSpace.Min.Z) / 2);
                        var familyInstance = document.AddFASU(FASUType.F8_150_250Phi, locationFasu, null, Math.PI/2);

                        // Vav object 
                        BoundingBoxXYZ boxFasu = familyInstance.get_BoundingBox(document.ActiveView);
                        if (boxFasu != null)
                        {
                            var locVav = new XYZ((boxFasu.Max.X + boxFasu.Min.X) / 2, (boxFasu.Max.Y + boxFasu.Min.Y) / 2, (boxFasu.Max.Z + boxFasu.Min.Z) / 2);
                            double distance = (boxFasu.Max.X - boxFasu.Min.X) / 2;
                            document.AddVAV(VAVType.TTE_VAV_Maru, locVav, null, distance);
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
    }
}