using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
    public abstract class CreateArentShaftCommandBase : IExternalCommand
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
                // Pick start point 
                XYZ point1 = selection.PickPoint("Pick start point");
                XYZ? point3 = null;
                RectangleExternal? rectangleExternal = null;
                try
                {
                    rectangleExternal = new RectangleExternal(uiApp) { DrawingServer = { BasePoint = point1 } };
                    rectangleExternal.DrawExternal();
                    // Pick end point 
                    point3 = selection.PickPoint("Pick end point");
                }
                catch (Exception)
                {
                    //
                }
                finally
                {
                    if (rectangleExternal != null)
                        rectangleExternal.Dispose();
                }

                if (point3 == null) return Result.Succeeded;
                if (point3.DistanceTo(point1) <= 0.1)
                {
                    if (rectangleExternal != null)
                        rectangleExternal.Dispose();
                    TaskDialog.Show("Dialog.Commands.Draw.Common.Title.Error".GetAppStringByKeyOrDefault("エラー"),
                                    "Dialog.Commands.Draw.Common.Body.Error".GetAppStringByKeyOrDefault("始点と終点が一致します。"));
                    return Result.Cancelled;
                }

                var midPoint = (point1 + point3) * 0.5;
                var currView = document.ActiveView;
                var plane = Plane.CreateByNormalAndOrigin(currView.RightDirection, midPoint);
                var mirrorMat = Transform.CreateReflection(plane);

                var point2 = mirrorMat.OfPoint(point1);
                var point4 = mirrorMat.OfPoint(point3);

                IList<Element> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).ToElements();
                Level? lowestLevel = levels.ElementAt(0) as Level;
                Level? highestLevel = levels.ElementAt(levels.Count - 1) as Level;
                if (lowestLevel == null && highestLevel == null) return Result.Failed;

                var result = document.Transaction("Create Arent Shaft", _ =>
                {
                    Curve left = Line.CreateBound(point1, point4);
                    Curve upper = Line.CreateBound(point1, point2);
                    Curve right = Line.CreateBound(point2, point3);
                    Curve lower = Line.CreateBound(point3, point4);
                    CurveArray shaftProfile = app.Create.NewCurveArray();
                    shaftProfile.Append(left);
                    shaftProfile.Append(upper);
                    shaftProfile.Append(right);
                    shaftProfile.Append(lower);
                    Opening shaftOpening = document.Create.NewOpening(lowestLevel, highestLevel, shaftProfile);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(0);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).Set(lowestLevel!.Id);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(highestLevel!.Id);

                    double width = point1.DistanceTo(point2);
                    double height = point2.DistanceTo(point3);

                    FamilyInstance fi = document.AddRackGuid(midPoint);
                    fi.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(0.0);
                    fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).Set(0.0);
                    fi.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).Set(lowestLevel!.Id);
                    fi.LookupParameter("幅").Set(width);
                    fi.LookupParameter("奥行き").Set(height);
                    fi.LookupParameter("高さ").Set(highestLevel!.Elevation);
                    fi.LookupParameter("Arent-Offset").Set(lowestLevel!.Elevation);
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
    }
}
