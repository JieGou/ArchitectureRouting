using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using ImageType = Arent3d.Revit.UI.ImageType;
using System;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Mechanical.App.Commands.Routing.CreateFASUAndVAVAutomaticallyGuideCommand", DefaultString = "Create Auto\nFASU")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateFASUAndVAVAutomaticallyGuideCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            // Get all the spaces in the document
            IList<Element> spaces = GetAllSpaces(document);

            // Get FASU height
            double heightOfFASU = 0;
            bool bHeightOfFASU = GetHeightFASU(document, "Common 45 deg", ref heightOfFASU);
            if (!bHeightOfFASU) heightOfFASU = 385;

                // Start Transaction
            using (Transaction tr = new Transaction(document))
            {
                tr.Start("Create FASU and VAV Automatically Guide Command");
                foreach (var space in spaces)
                {
                    // Add object to the document
                    BoundingBoxXYZ boxSpace = space.get_BoundingBox(document.ActiveView);
                    if (boxSpace == null) continue;
                    
                    // FASU object
                    var locationFASU = new XYZ((boxSpace.Max.X + boxSpace.Min.X) / 2,
                        (boxSpace.Max.Y + boxSpace.Min.Y) / 2, heightOfFASU);
                    var fasuInstance = document.AddFASU(locationFASU, space.LevelId);
                    ElementTransformUtils.RotateElement(document, fasuInstance.Id,
                        Line.CreateBound(locationFASU, locationFASU + XYZ.BasisZ), Math.PI / 2);
                    fasuInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).SetValueString("3100");

                    // VAV object 
                    BoundingBoxXYZ boxFASU = fasuInstance.get_BoundingBox(document.ActiveView);
                    if (boxFASU == null) continue;
                    
                    var locVAV = new XYZ((boxFASU.Max.X + boxFASU.Min.X) / 2,
                        (boxFASU.Max.Y + boxFASU.Min.Y) / 2, heightOfFASU);
                    double distance = (boxFASU.Max.X - boxFASU.Min.X) / 2;
                    FamilyInstance vavInstance = document.AddVAV(locVAV, space.LevelId);
                    vavInstance.LookupParameter("ダクト径").SetValueString("250");
                    BoundingBoxXYZ box = vavInstance.get_BoundingBox(document.ActiveView);
                    distance += (box.Max.X - box.Min.X) / 4;
                    ElementTransformUtils.MoveElement(document, vavInstance.Id, new XYZ(distance, 0, 0));
                }

                tr.Commit();
            }
            return Result.Succeeded;
        }
        private static IList<Element> GetAllSpaces(Document document)
        {
            ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces);
            FilteredElementCollector collector = new(document);
            IList<Element> spaces = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            return spaces;
        }
        private static bool GetHeightFASU(Document document, string nameFASU, ref double heightOfFASU)
        {
            ElementCategoryFilter filter = new(BuiltInCategory.OST_DuctFitting);
            FilteredElementCollector collector = new(document);
            IList<Element> ducts = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            foreach (var duct in ducts)
            {
                if (duct.Name.IndexOf(nameFASU, 0, StringComparison.Ordinal) == -1) continue;
                var locationPoint = (duct.Location as LocationPoint)!;
                heightOfFASU = locationPoint.Point.Z;
                return true;
            }
            return false;
        }
    }
}