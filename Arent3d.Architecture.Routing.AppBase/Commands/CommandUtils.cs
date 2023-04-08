using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters;
using Arent3d.Architecture.Routing.AppBase.Model;
using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
    public static class CommandUtils
    {
        public static void AlertDeletedElements()
        {
            TaskDialog.Show("Dialog.Commands.Routing.Common.Title.Error".GetAppStringByKeyOrDefault(null),
                            "Dialog.Commands.Routing.Common.Body.Error.DeletedSomeFailedElements".GetAppStringByKeyOrDefault(null));
        }

        public static void AlertBadConnectors(IEnumerable<Connector[]> badConnectorSet)
        {
            var body = string.Format("Dialog.Commands.Routing.Common.Body.Error.FittingCannotBeInserted".GetAppStringByKeyOrDefault(null), "・"
                + string.Join("\n・", badConnectorSet.Select(GetConnectorInfo)));
            TaskDialog.Show("Dialog.Commands.Routing.Common.Title.Error".GetAppStringByKeyOrDefault(null), body);
        }

        public static void DebugAlertException(Exception e)
        {
#if DEBUG
            TaskDialog.Show("Debug", e.ToString());
#else
      TaskDialog.Show( "Dialog.Commands.Routing.Common.Title.Error".GetAppStringByKeyOrDefault( null ), "Dialog.Commands.Routing.Common.Body.Error.ExceptionOccured".GetAppStringByKeyOrDefault( null ) ) ;
#endif
        }

        private static string GetConnectorInfo(Connector[] connectorSet)
        {
            var connectionType = connectorSet.Length switch { 2 => "Elbow", 3 => "Tee", 4 => "Cross", _ => throw new ArgumentException() };
            var connector = connectorSet.FirstOrDefault(c => c.IsValidObject);
            var coords = (null != connector) ? GetCoordValue(connector.Owner.Document, connector.Origin) : "(Deleted connectors)";
            return $"[{connectionType}] {coords}";
        }

        private static string GetCoordValue(Document document, XYZ pos)
        {
            return document.DisplayUnitSystem switch
            {
                DisplayUnit.METRIC => $"({pos.X.RevitUnitsToMeters()}, {pos.Y.RevitUnitsToMeters()}, {pos.Z.RevitUnitsToMeters()})",
                _ => $"({pos.X.RevitUnitsToFeet()}, {pos.Y.RevitUnitsToFeet()}, {pos.Z.RevitUnitsToFeet()})",
            };
        }

        public static List<ElectricalCategoryModel> LoadElectricalCategories(string sheetName, ref Dictionary<string, string> dictData)
        {
            const string ElectricalCategoryFileName = "ElectricalCategory.xlsx";
            const string ResourceFolderName = "resources";
            //Load ElectricalCategory from excel file resource
            string resourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, ResourceFolderName);
            var filePath = Path.Combine(resourcesPath, ElectricalCategoryFileName);
            return ExcelToModelConverter.GetElectricalCategories(filePath, ref dictData, sheetName);
        }
    }
}