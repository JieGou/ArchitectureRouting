using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
    public abstract class PickRoutingDifferenceFloorCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDocument = uiApp.ActiveUIDocument;
            Document document = uiDocument.Document;
            Application app = uiApp.Application;
            Selection selection = uiDocument.Selection;
            try
            {
                Reference fromRef = selection.PickObject(ObjectType.Element, FamilyElectricalInstanceFilter.Instance, "Pick an element");
                Element fromEle = document.GetElement(fromRef);
                FamilyInstance pickFrom = (FamilyInstance)fromEle;

                Reference toRef = selection.PickObject(ObjectType.Element, FamilyElectricalInstanceFilter.Instance, "Pick an element");
                Element toEle = document.GetElement(toRef);
                FamilyInstance pickTo = (FamilyInstance)toEle;

                Level? levelOfPickFrom = fromEle.Document.GetElement(fromEle.LevelId) as Level;
                Level? levelOfPickTo = toEle.Document.GetElement(toEle.LevelId) as Level;

                var rackGuids = document.GetAllFamilyInstances(RoutingFamilyType.RackGuide).FirstOrDefault();

                FilteredElementCollector collector = new FilteredElementCollector(document).OfClass(typeof(ConduitType));
                ConduitType? type = collector.FirstElement() as ConduitType;

                var result = document.Transaction("Routing Difference Floor", _ =>
                {
                    var pickFromPoint = GetPoint(pickFrom)!;
                    var pickToPoint = GetPoint(pickTo)!;
                    var rackPoint = GetPoint(rackGuids)!;
                    var distanceFromPickFrom = pickFromPoint.DistanceTo(rackPoint);
                    var distanceFromPickTo = pickToPoint.DistanceTo(rackPoint);
                    var pickFromElevation = pickFrom.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                    var pickToElevation = pickTo.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                    double heightOfPickFromLvl = 4700;
                    double heightOfPickToLvl = 5000;
                    var pointCeilingOfPickFrom = new XYZ(pickFromPoint.X, pickFromPoint.Y, pickFromPoint.Z + heightOfPickFromLvl.MillimetersToRevitUnits() - pickFromElevation);
                    Conduit.Create(document, type!.Id, pickFromPoint, pointCeilingOfPickFrom, levelOfPickFrom!.Id);

                    var pointRackOfPickFrom = new XYZ(rackPoint.X, rackPoint.Y, pointCeilingOfPickFrom.Z);
                    Conduit.Create(document, type!.Id, pointCeilingOfPickFrom, pointRackOfPickFrom, levelOfPickFrom.Id);

                    var pointCeilingOfPickTo = new XYZ(pickToPoint.X, pickToPoint.Y, pickToPoint.Z + heightOfPickToLvl.MillimetersToRevitUnits() - pickToElevation);
                    Conduit.Create(document, type!.Id, pickToPoint, pointCeilingOfPickTo, levelOfPickTo!.Id);

                    var pointRackOfPickTo = new XYZ(rackPoint.X, rackPoint.Y, pointCeilingOfPickTo.Z);
                    Conduit.Create(document, type!.Id, pointCeilingOfPickTo, pointRackOfPickTo, levelOfPickTo.Id);

                    Conduit.Create(document, type!.Id, pointRackOfPickFrom, pointRackOfPickTo, levelOfPickFrom.Id);
                    return Result.Succeeded;
                });

                return result;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception e)
            {
                CommandUtils.DebugAlertException(e);
                return Result.Failed;
            }
        }
        private XYZ? GetPoint(FamilyInstance element)
        {
            LocationPoint? LP = element.Location as LocationPoint;
            return LP?.Point;
        }
    }
    public class FamilyElectricalInstanceFilter : ISelectionFilter
    {
        public static ISelectionFilter Instance { get; } = new FamilyElectricalInstanceFilter();

        public bool AllowElement(Element element)
        {
            if (element is FamilyInstance fi && (fi.Category.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures))
            {
                return true;
            }

            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }
}
