using System;
using System.Collections.Generic;
using System.Linq;
using Arent3d.Architecture.Routing.AppBase.Commands;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;


namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    public class ShowDialogCreateTableCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var document = commandData.Application.ActiveUIDocument.Document;

            var dialog = new GetLevel(document);
            dialog.ShowDialog();
            return Result.Succeeded;
        }
    }
}
