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
    [DisplayNameKey("Mechanical.App.Commands.Routing.CreateAutoFasuGuideCommand", DefaultString = "Create Auto\nFASU")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateAutoFasuGuideCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            // Get all the spaces in the document
            IList<Element> spaces = GetAllSpaces(document);

            // Get fasu height
            double heightFasu = 0;
            bool bHeightFasu = GetHeightFasu(document, "Common 45 deg", ref heightFasu);

            // Start Transaction
            using (Transaction tr = new Transaction(document))
            {
                tr.Start("Create Auto Fasu and Vav Guide");
                foreach (var space in spaces)
                {
                    // Add object to the document
                    BoundingBoxXYZ boxSpace = space.get_BoundingBox(document.ActiveView);
                    if (boxSpace != null)
                    {
                        // Fasu object
                        if (!bHeightFasu) heightFasu = (boxSpace.Max.Z + boxSpace.Min.Z) / 2;
                        var locationFasu = new XYZ((boxSpace.Max.X + boxSpace.Min.X) / 2,
                            (boxSpace.Max.Y + boxSpace.Min.Y) / 2, heightFasu);
                        var fasuInstance = document.AddFasu(locationFasu, null);
                        ElementTransformUtils.RotateElement(document, fasuInstance.Id,
                            Line.CreateBound(locationFasu, locationFasu + XYZ.BasisZ), Math.PI / 2);
                        fasuInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).SetValueString("3100");

                        // Vav object 
                        BoundingBoxXYZ boxFasu = fasuInstance.get_BoundingBox(document.ActiveView);
                        if (boxFasu != null)
                        {
                            if (!bHeightFasu) heightFasu = (boxFasu.Max.Z + boxFasu.Min.Z) / 2;
                            var locVav = new XYZ((boxFasu.Max.X + boxFasu.Min.X) / 2,
                                (boxFasu.Max.Y + boxFasu.Min.Y) / 2, heightFasu);
                            double distance = (boxFasu.Max.X - boxFasu.Min.X) / 2;
                            FamilyInstance vavInstance = document.AddVav(locVav, null);
                            vavInstance.LookupParameter("ダクト径").SetValueString("250");
                            BoundingBoxXYZ box = vavInstance.get_BoundingBox(document.ActiveView);
                            distance += (box.Max.X - box.Min.X) / 4;
                            ElementTransformUtils.MoveElement(document, vavInstance.Id, new XYZ(distance, 0, 0));
                        }
                    }
                }

                tr.Commit();
            }
            return Result.Succeeded;
        }
        private IList<Element> GetAllSpaces(Document document)
        {
            ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces);
            FilteredElementCollector collector = new(document);
            IList<Element> spaces = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            return spaces;
        }
        private bool GetHeightFasu(Document document, string nameFasu, ref double height)
        {
            bool brc = false;
            ElementCategoryFilter filter = new(BuiltInCategory.OST_DuctFitting);
            FilteredElementCollector collector = new(document);
            IList<Element> ducts = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            foreach (var duct in ducts)
            {
                if (duct.Name.IndexOf(nameFasu, 0, StringComparison.Ordinal) == -1) continue;
                var locationPoint = (duct.Location as LocationPoint)!;
                height = locationPoint.Point.Z;
                brc = true;
                break;
            }
            return brc;
        }
    }
}