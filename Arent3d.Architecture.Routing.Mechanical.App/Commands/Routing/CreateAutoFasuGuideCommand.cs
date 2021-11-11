using System;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic;
using Arent3d.Architecture.Routing.FASU ;
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
                    BoundingBoxXYZ box = space.get_BoundingBox( document.ActiveView );
                    if (box != null)
                    {
                        var loc = new XYZ((box.Max.X + box.Min.X) / 2, (box.Max.Y + box.Min.Y) / 2, (box.Max.Z + box.Min.Z) / 2);
                        document.AddFASU(FASUType.F8_150_250Phi, loc, null);
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