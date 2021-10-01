using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
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
                List<XYZ> pointsSelected = new();
                // Pick start point 
                XYZ firstPoint = selection.PickPoint("Pick first point");
                pointsSelected.Add(firstPoint);

                XYZ? nextPoint = null;
                LineExternal? lineExternal = new LineExternal(uiApp);
                try
                {
                    lineExternal.PickedPoints.Add(firstPoint);
                    var tmp = firstPoint;
                    while (firstPoint != nextPoint)
                    {
                        lineExternal.DrawingServer.BasePoint = tmp;
                        lineExternal.DrawExternal();

                        // Pick next point 
                        nextPoint = selection.PickPoint("Pick next point");
                        if (firstPoint.DistanceTo(nextPoint) > 0.1)
                            pointsSelected.Add(nextPoint);
                        else
                        {
                            pointsSelected.Add(firstPoint);
                            break;
                        }
                        tmp = nextPoint;
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (lineExternal != null)
                        lineExternal.Dispose();
                }

                if (nextPoint == null) return Result.Succeeded;
                if (pointsSelected.Count == 2 && nextPoint.DistanceTo(firstPoint) <= 0.1)
                {
                    if (lineExternal != null)
                        lineExternal.Dispose();
                    TaskDialog.Show("Dialog.Commands.Draw.Common.Title.Error".GetAppStringByKeyOrDefault("エラー"),
                                    "Dialog.Commands.Draw.Common.Body.Error".GetAppStringByKeyOrDefault("始点と終点が同じです。"));
                    return Result.Cancelled;
                }
                if (pointsSelected.First().DistanceTo(pointsSelected.Last()) <= 0.1)
                {
                    HeightSettingStorable heightSetting = document.GetHeightSettingStorable();
                    var levels = heightSetting.Levels.OrderBy(x => x.Elevation).ToList();
                    Level? lowestLevel = levels.FirstOrDefault();
                    Level? highestLevel = levels.LastOrDefault();
                    if (lowestLevel == null && highestLevel == null) return Result.Failed;

                    var result = document.Transaction("Create Arent Shaft", _ =>
                    {
                        CurveArray shaftProfile = app.Create.NewCurveArray();
                        var firstP = pointsSelected[0];
                        for (var i = 1; i < pointsSelected.Count; i++)
                        {
                            var nextP = pointsSelected[i];
                            if (firstP.DistanceTo(nextP) > 0.001)
                            {
                                Curve curve = Line.CreateBound(firstP, nextP);
                                shaftProfile.Append(curve);
                            }

                            firstP = nextP;
                        }
                        Opening shaftOpening = document.Create.NewOpening(lowestLevel, highestLevel, shaftProfile);
                        shaftOpening.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(0);
                        shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);
                        shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).Set(lowestLevel!.Id);
                        shaftOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(highestLevel!.Id);

                        return Result.Succeeded;
                    });

                    return result;
                }
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
        }
    }
}
