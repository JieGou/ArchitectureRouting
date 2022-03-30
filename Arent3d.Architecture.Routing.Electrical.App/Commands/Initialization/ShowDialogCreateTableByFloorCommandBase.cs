using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using static Arent3d.Architecture.Routing.AppBase.Forms.GetLevel;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    public class ShowDialogCreateTableByFloorCommandBase : IExternalCommand
    {
        private readonly struct ConnectorInfo
        {
            public ConnectorInfo(string ceedSetCode, string deviceSymbol, string modelNumber)
            {
                CeedSetCode = ceedSetCode;
                DeviceSymbol = deviceSymbol;
                ModelNumber = modelNumber;
            }

            public string CeedSetCode { get; }
            public string DeviceSymbol { get; }
            public string ModelNumber { get; }
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var uiDoc = commandData.Application.ActiveUIDocument;

            var dialog = new CreateTableByFloors(doc);
            dialog.ShowDialog();
            string tableType = dialog.SelectedTableType;

            List<ElementId> levelIds = dialog.LevelList.Select(p => p.LevelId).ToList();

            switch (tableType)
            {
                case "Detail Table":
                    CreateDetailTableSchedule();
                    break;
                case "Electrical Symbol Table":
                    CreateElectricalSchedule(doc, uiDoc, levelIds);
                    break;
                default:
                    break;
            }
            return Result.Succeeded;
        }

        private void CreateElectricalSchedule(Document doc, UIDocument uiDoc, List<ElementId> levelIds)
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

            var conduits = doc.GetAllElements<Element>().OfCategory(BuiltInCategory.OST_Conduit).ToList();
            var routePicked = conduits.Select(e => e.GetRouteName()).Distinct().ToList();

            foreach (var routeName in routePicked)
            {
                var fromConnectorInfoAndToConnectorInfo = GetFromConnectorInfoAndToConnectorInfo(doc, allConnectors, routeName!, ref errorMess);
                if (!string.IsNullOrEmpty(errorMess))
                {
                    return;
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

        private static (string fromConnectorUniqueId, ConnectorInfo fromConnectorInfo, string toConnectorUniqueId, ConnectorInfo toConnectorInfo) GetFromConnectorInfoAndToConnectorInfo(Document document, IReadOnlyCollection<Element> allConnectors, string routeName, ref string errorMess)
        {
            var conduitsOfRoute = document.GetAllElements<Element>().OfCategory(BuiltInCategorySets.Conduits).Where(c => c.GetRouteName() == routeName);
            Element? fromConnector = null;
            Element? toConnector = null;
            foreach (var conduit in conduitsOfRoute)
            {
                var fromEndPoint = conduit.GetNearestEndPoints(true).ToList();
                if (!fromEndPoint.Any()) continue;
                var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key;
                var fromUniqueId = fromEndPointKey?.GetElementUniqueId();
                if (string.IsNullOrEmpty(fromUniqueId)) continue;
                var fromElement = allConnectors.FirstOrDefault(c => c.UniqueId == fromUniqueId);
                if (fromElement != null && !fromElement.IsTerminatePoint() && !fromElement.IsPassPoint())
                    fromConnector = fromElement;

                var toEndPoint = conduit.GetNearestEndPoints(false).ToList();
                if (!toEndPoint.Any()) continue;
                var toEndPointKey = toEndPoint.FirstOrDefault()?.Key;
                var toUniqueId = toEndPointKey?.GetElementUniqueId();
                if (string.IsNullOrEmpty(toUniqueId)) continue;
                var toElement = allConnectors.FirstOrDefault(c => c.UniqueId == toUniqueId);
                if (toElement == null || toElement.IsTerminatePoint() || toElement.IsPassPoint()) continue;
                toConnector = toElement;
            }

            if (fromConnector == null || toConnector == null)
            {
                errorMess = routeName + " is not connected.";
                return (string.Empty, new ConnectorInfo(string.Empty, string.Empty, string.Empty), string.Empty, new ConnectorInfo(string.Empty, string.Empty, string.Empty));
            }

            var fromConnectorInfo = GetConnectorCeedCodeInfo(fromConnector);
            var toConnectorInfo = GetConnectorCeedCodeInfo(toConnector);
            return (fromConnector.UniqueId, fromConnectorInfo, toConnector.UniqueId, toConnectorInfo);
        }

        private static void CreateDetailTableSchedule()
        {
        }

        private static ConnectorInfo GetConnectorCeedCodeInfo(Element connector)
        {
            connector.TryGetProperty(ElectricalRoutingElementParameter.CeedCode, out string? ceedCode);
            if (string.IsNullOrEmpty(ceedCode)) return new ConnectorInfo(string.Empty, string.Empty, string.Empty);
            var ceedCodeInfo = ceedCode!.Split('-').ToList();
            var ceedSetCode = ceedCodeInfo.First();
            var deviceSymbol = ceedCodeInfo.Count > 1 ? ceedCodeInfo.ElementAt(1) : string.Empty;
            var modelNumber = ceedCodeInfo.Count > 2 ? ceedCodeInfo.ElementAt(2) : string.Empty;
            return new ConnectorInfo(ceedSetCode, deviceSymbol, modelNumber);
        }

        private void InsertDataFromDetailTableModelIntoElectricalSymbolModel(List<ElectricalSymbolModel> electricalSymbolModels, List<DetailTableModel> detailTableModelsByRouteName, CeedModel? fromConnectorCeedModel, CeedModel? toConnectorCeedModel, string fromConnectorUniqueId, string toConnectorUniqueId)
        {
            const string defaultChildPlumbingSymbol = "↑";
            foreach (var element in detailTableModelsByRouteName)
            {
                var wireType = element.WireType;
                var wireSize = element.WireSize;
                var wireStrip = element.WireStrip;
                string plumbingType;
                var plumbingSize = string.Empty;
                if (element.IsParentRoute)
                {
                    plumbingType = element.PlumbingType;
                    plumbingSize = element.PlumbingSize;
                }
                else
                {
                    plumbingType = element.PlumbingIdentityInfo.Split('-').First().Replace(defaultChildPlumbingSymbol, "");
                }

                if (fromConnectorCeedModel != null)
                {
                    var startElectricalSymbolModel = new ElectricalSymbolModel(fromConnectorUniqueId, fromConnectorCeedModel.FloorPlanType, fromConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, plumbingType, plumbingSize);
                    electricalSymbolModels.Add(startElectricalSymbolModel);
                }

                if (toConnectorCeedModel == null) continue;
                var endElectricalSymbolModel = new ElectricalSymbolModel(toConnectorUniqueId, toConnectorCeedModel.FloorPlanType, toConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, plumbingType, plumbingSize);
                electricalSymbolModels.Add(endElectricalSymbolModel);
            }
        }

        private void InsertDataFromRegularDatabaseIntoElectricalSymbolModel(List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiMasterModel> hiroiMasterModelData, List<Element> allConnectors, List<ElectricalSymbolModel> electricalSymbolModels, CeedModel? fromConnectorCeedModel, CeedModel? toConnectorCeedModel, string fromConnectorUniqueId, string toConnectorUniqueId)
        {
            const string defaultPlumbingType = "配管なし";
            if (toConnectorCeedModel == null) return;
            var isEcoMode = string.Empty;
            var wireType = string.Empty;
            var wireSize = string.Empty;
            var wireStrip = string.Empty;
            var endConnector = allConnectors.First(c => c.UniqueId == toConnectorUniqueId);
            endConnector?.TryGetProperty(ElectricalRoutingElementParameter.IsEcoMode, out isEcoMode);
            var hiroiSetModels =
              !string.IsNullOrEmpty(isEcoMode) && bool.Parse(isEcoMode!)
                ? hiroiSetMasterEcoModelData.Where(x => x.ParentPartModelNumber.Contains(toConnectorCeedModel.CeedModelNumber)).Skip(1)
                : hiroiSetMasterNormalModelData.Where(x => x.ParentPartModelNumber.Contains(toConnectorCeedModel.CeedModelNumber)).Skip(1);
            foreach (var item in hiroiSetModels)
            {
                List<string> listMaterialCode = new();
                if (!string.IsNullOrWhiteSpace(item.MaterialCode1))
                {
                    listMaterialCode.Add(int.Parse(item.MaterialCode1).ToString());
                }

                if (!listMaterialCode.Any()) continue;
                var masterModels = hiroiMasterModelData.Where(x => listMaterialCode.Contains(int.Parse(x.Buzaicd).ToString()));
                foreach (var master in masterModels)
                {
                    wireType = master.Type;
                    wireSize = master.Size1;
                    wireStrip = master.Size2;
                }
            }

            if (fromConnectorCeedModel != null)
            {
                var startElectricalSymbolModel = new ElectricalSymbolModel(fromConnectorUniqueId, fromConnectorCeedModel.FloorPlanType, fromConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, defaultPlumbingType, string.Empty);
                electricalSymbolModels.Add(startElectricalSymbolModel);
            }

            var endElectricalSymbolModel = new ElectricalSymbolModel(toConnectorUniqueId, toConnectorCeedModel.FloorPlanType, toConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, defaultPlumbingType, string.Empty);
            electricalSymbolModels.Add(endElectricalSymbolModel);
        }
    }
}
