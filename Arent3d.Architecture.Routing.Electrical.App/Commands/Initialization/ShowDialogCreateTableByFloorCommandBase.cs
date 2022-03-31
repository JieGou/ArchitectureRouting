using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.ShowElectricSymbolsCommandBase;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    public class ShowDialogCreateTableByFloorCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var uiDoc = commandData.Application.ActiveUIDocument;

            var dialog = new CreateTableByFloors(doc);
            dialog.ShowDialog();
            string tableType = dialog.SelectedTableType;

            List<ElementId> levelIds = dialog.LevelList.Where(t => t.IsSelected == true).Select (p => p.LevelId).ToList();

            switch (tableType)
            {
                case "Detail Table":
                    CreateDetailTableSchedule();
                    break;
                case "Electrical Symbol Table":
                    CreateElectricalTable(doc, uiDoc, levelIds);
                    break;
                default:
                    break;
            }
            return Result.Succeeded;
        }

        private Result CreateElectricalTable(Document doc, UIDocument uiDoc, List<ElementId> levelIds)
        {
            var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault();
            var csvStorable = doc.GetCsvStorable();
            var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData;
            var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData;
            var hiroiMasterModelData = csvStorable.HiroiMasterModelData;
            var detailTableModelData = doc.GetDetailTableStorable().DetailTableModelData;
            var allConnectors = doc.GetAllElements<Element>().OfCategory(BuiltInCategorySets.OtherElectricalElements).ToList();
            var electricalSymbolModels = new List<ElectricalSymbolModel>();
            var errorMess = string.Empty;
            try
            {
                var allConduits = new FilteredElementCollector(doc).OfClass(typeof(Conduit)).OfCategory(BuiltInCategory.OST_Conduit).AsEnumerable().OfType<Conduit>();
                var conduitsByFloors = allConduits.Where(x => levelIds.Contains(x.ReferenceLevel.Id)).ToList();
                var routePicked = conduitsByFloors.Select(e => e.GetRouteName()).Distinct().ToList();

                foreach (var routeName in routePicked)
                {
                    var fromConnectorInfoAndToConnectorInfo = GetFromConnectorInfoAndToConnectorInfo(doc, allConnectors, routeName!, ref errorMess);
                    if (!string.IsNullOrEmpty(errorMess))
                    {
                        return Result.Cancelled;
                    }

                    var fromConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault(x => x.CeedSetCode == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.CeedSetCode
                                                                                                && x.GeneralDisplayDeviceSymbol == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.DeviceSymbol
                                                                                                && x.ModelNumber == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.ModelNumber);
                    var toConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault(x => x.CeedSetCode == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.CeedSetCode
                                                                                              && x.GeneralDisplayDeviceSymbol == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.DeviceSymbol
                                                                                              && x.ModelNumber == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.ModelNumber);
                    if (fromConnectorCeedModel == null && toConnectorCeedModel == null) continue;
                    var detailTableModelsByRouteName = detailTableModelData.Where(d => d.RouteName == routeName).ToList();
                    if (detailTableModelsByRouteName.Any())
                    {
                        InsertDataFromDetailTableModelIntoElectricalSymbolModel(electricalSymbolModels, detailTableModelsByRouteName, fromConnectorCeedModel, toConnectorCeedModel, fromConnectorInfoAndToConnectorInfo.fromConnectorUniqueId, fromConnectorInfoAndToConnectorInfo.toConnectorUniqueId);
                    }
                    else
                    {
                        InsertDataFromRegularDatabaseIntoElectricalSymbolModel(hiroiSetMasterEcoModelData, hiroiSetMasterNormalModelData, hiroiMasterModelData, allConnectors, electricalSymbolModels, fromConnectorCeedModel, toConnectorCeedModel, fromConnectorInfoAndToConnectorInfo.fromConnectorUniqueId, fromConnectorInfoAndToConnectorInfo.toConnectorUniqueId);
                    }
                }
            }
            catch
            {
                return Result.Cancelled;
            }

            return doc.Transaction("TransactionName.Commands.Initialization.ShowElectricSymbolsCommand".GetAppStringByKeyOrDefault("Create electrical schedule"), _ =>
            {
                CreateElectricalSchedule(doc, electricalSymbolModels);
                return Result.Succeeded;
            });
        }
    
        private static void CreateDetailTableSchedule()
        {
        }
    }
}
