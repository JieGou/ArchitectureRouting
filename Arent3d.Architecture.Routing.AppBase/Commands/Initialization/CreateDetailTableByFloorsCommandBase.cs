using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.Selection;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Utility;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
    public abstract class CreateDetailTableByFloorsCommandBase : IExternalCommand
    {
        private const string DefaultConstructionItems = "未設定";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            const string defaultParentPlumbingType = "E";
            var doc = commandData.Application.ActiveUIDocument.Document;
            var uiDoc = commandData.Application.ActiveUIDocument;
            var csvStorable = doc.GetCsvStorable();
            var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData;
            var conduitsModelData = csvStorable.ConduitsModelData;
            var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData;
            var hiroiSetMasterEcoModelData = doc.GetCsvStorable().HiroiSetMasterEcoModelData;
            var hiroiMasterModelData = csvStorable.HiroiMasterModelData;
            var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData;
            var hiroiSetCdMasterEcoModelData = doc.GetCsvStorable().HiroiSetCdMasterEcoModelData;
            var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault();
            var detailTableModelsData = doc.GetDetailTableStorable().DetailTableModelData;
            var detailTableModels = new ObservableCollection<DetailTableModel>();
            var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable();
            var cnsStorable = doc.GetCnsSettingStorable();
            bool mixConstructionItems;
            try
            {
                var pickedObjects = doc.GetAllElements<Conduit>().ToList();
                var pickedObjectIds = pickedObjects.Select(p => p.UniqueId).ToList();
                var detailSymbolModelsByDetailSymbolId = detailSymbolStorable.DetailSymbolModelData.Where(x => pickedObjectIds.Contains(x.ConduitId)).OrderBy(x => x.DetailSymbol).ThenByDescending(x => x.DetailSymbolId).ThenByDescending(x => x.IsParentSymbol).GroupBy(x => x.DetailSymbolId, (key, p) => new { DetailSymbolId = key, DetailSymbolModels = p.ToList() });
                var detailSymbolIds = detailSymbolStorable.DetailSymbolModelData.Where(x => pickedObjectIds.Contains(x.ConduitId)).Select(d => d.DetailSymbolId).Distinct().ToHashSet();
                mixConstructionItems = CheckMixConstructionItems(detailTableModelsData, detailSymbolIds);
                foreach (var detailSymbolModelByDetailSymbolId in detailSymbolModelsByDetailSymbolId)
                {
                    var firstDetailSymbolModelByDetailSymbolId = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault();
                    var routeNames = detailSymbolModelByDetailSymbolId.DetailSymbolModels.Select(d => d.RouteName).Distinct().ToList();
                    var parentRouteName = firstDetailSymbolModelByDetailSymbolId!.CountCableSamePosition == 1 ? firstDetailSymbolModelByDetailSymbolId.RouteName : GetParentRouteName(doc, routeNames);
                    if (!string.IsNullOrEmpty(parentRouteName))
                    {
                        var parentDetailSymbolModel = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault(d => d.RouteName == parentRouteName);
                        AddDetailTableModelRow(doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, pickedObjects, parentDetailSymbolModel!, true, mixConstructionItems);
                        routeNames = routeNames.Where(n => n != parentRouteName).OrderByDescending(n => n).ToList();
                    }

                    foreach (var childDetailSymbolModel in from routeName in routeNames select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault(d => d.RouteName == routeName))
                    {
                        AddDetailTableModelRow(doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, pickedObjects, childDetailSymbolModel, false, mixConstructionItems);
                    }
                }

                SortDetailTableModel(ref detailTableModels);
                SetPlumbingData(conduitsModelData, ref detailTableModels, defaultParentPlumbingType, mixConstructionItems);
            }
            catch
            {
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }

        private static void SortDetailTableModel(ref ObservableCollection<DetailTableModel> detailTableModels, bool mixConstructionItems = false)
        {
            var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy(d => d.DetailSymbolId).ToDictionary(g => g.Key, g => g.ToList());
            List<DetailTableModel> sortedDetailTableModelsList = new();
            foreach (var detailSymbolId in detailTableModelsGroupByDetailSymbolId.Keys)
            {
                List<DetailTableModel> detailTableRowsByDetailSymbolId = detailTableModelsGroupByDetailSymbolId[detailSymbolId]!.OrderBy(x => x.DetailSymbol).ThenByDescending(x => x.DetailSymbolId).ThenByDescending(x => x.SignalType).ToList();
                var detailTableRowsBySignalTypes = detailTableRowsByDetailSymbolId.GroupBy(d => d.SignalType).OrderByDescending(g => g.ToList().Any(d => d.IsParentRoute)).Select(g => g.ToList()).ToList();
                foreach (var detailTableRowsBySignalType in detailTableRowsBySignalTypes)
                {
                    if (mixConstructionItems)
                        sortedDetailTableModelsList.AddRange(detailTableRowsBySignalType);
                    else
                    {
                        var orderedDetailTableRowsBySignalType = detailTableRowsBySignalType.GroupBy(d => d.ConstructionItems).OrderByDescending(g => g.ToList().Any(d => d.IsParentRoute)).SelectMany(g => g.ToList()).ToList();
                        sortedDetailTableModelsList.AddRange(orderedDetailTableRowsBySignalType);
                    }
                }
            }

            detailTableModels = new ObservableCollection<DetailTableModel>(sortedDetailTableModelsList);
        }

        protected internal static void SetPlumbingData(List<ConduitsModel> conduitsModelData, ref ObservableCollection<DetailTableModel> detailTableModels, string plumbingType, bool mixConstructionItems = false)
        {
            var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy(d => d.DetailSymbolId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var detailSymbolId in detailTableModelsGroupByDetailSymbolId.Keys)
            {
                List<DetailTableModel> detailTableRowsByDetailSymbolId = detailTableModelsGroupByDetailSymbolId[detailSymbolId]!;
                SetPlumbingDataForOneSymbol(conduitsModelData, detailTableRowsByDetailSymbolId, plumbingType, false, mixConstructionItems);
            }
        }

        protected internal static void SetPlumbingDataForOneSymbol(List<ConduitsModel> conduitsModelData, List<DetailTableModel> detailTableModelsByDetailSymbolId, string plumbingType, bool isPlumbingTypeHasBeenChanged, bool mixConstructionItems)
        {
            const double percentage = 0.32;
            const string defaultChildPlumbingSymbol = "↑";
            var plumbingCount = 0;

            if (!isPlumbingTypeHasBeenChanged)
            {
                var parentDetailRow = detailTableModelsByDetailSymbolId.First();
                if (parentDetailRow != null) plumbingType = string.IsNullOrEmpty(parentDetailRow.PlumbingType) ? plumbingType : parentDetailRow.PlumbingType.Replace(defaultChildPlumbingSymbol, string.Empty);
            }
            var conduitsModels = conduitsModelData.Where(c => c.PipingType == plumbingType).OrderBy(c => double.Parse(c.InnerCrossSectionalArea)).ToList();
            var maxInnerCrossSectionalArea = conduitsModels.Select(c => double.Parse(c.InnerCrossSectionalArea)).Max();
            var detailTableModelsBySignalType = mixConstructionItems ? detailTableModelsByDetailSymbolId.GroupBy(d => d.SignalType).Select(g => g.ToList()).ToList() : detailTableModelsByDetailSymbolId.GroupBy(d => new { d.SignalType, d.ConstructionItems }).Select(g => g.ToList()).ToList();

            foreach (var detailTableRows in detailTableModelsBySignalType)
            {
                Dictionary<string, List<DetailTableModel>> detailTableRowsGroupByPlumbingType = new();
                List<DetailTableModel> childDetailRows = new();
                var parentDetailRow = detailTableRows.First();
                var currentPlumbingCrossSectionalArea = 0.0;
                foreach (var currentDetailTableRow in detailTableRows)
                {
                    currentPlumbingCrossSectionalArea += currentDetailTableRow.WireCrossSectionalArea / percentage;

                    if (currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea)
                    {
                        var plumbing = conduitsModels.Last();
                        parentDetailRow.PlumbingType = parentDetailRow.IsParentRoute ? plumbingType : plumbingType + defaultChildPlumbingSymbol;
                        parentDetailRow.PlumbingSize = plumbing.Size.Replace("mm", "");
                        parentDetailRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo(parentDetailRow, mixConstructionItems);
                        parentDetailRow.IsReadOnlyPlumbingItems = !mixConstructionItems;
                        if (!detailTableRowsGroupByPlumbingType.ContainsKey(parentDetailRow.PlumbingIdentityInfo))
                            detailTableRowsGroupByPlumbingType.Add(parentDetailRow.PlumbingIdentityInfo, childDetailRows);
                        else
                        {
                            detailTableRowsGroupByPlumbingType[parentDetailRow.PlumbingIdentityInfo].AddRange(childDetailRows);
                        }
                        childDetailRows = new List<DetailTableModel>();
                        plumbingCount++;
                        parentDetailRow = currentDetailTableRow;
                        currentPlumbingCrossSectionalArea = currentDetailTableRow.WireCrossSectionalArea;
                        if (currentDetailTableRow != detailTableRows.Last()) continue;
                        plumbing = conduitsModels.FirstOrDefault(c => double.Parse(c.InnerCrossSectionalArea) >= currentPlumbingCrossSectionalArea - currentDetailTableRow.WireCrossSectionalArea);
                        currentDetailTableRow.PlumbingType = currentDetailTableRow == detailTableModelsByDetailSymbolId.First() ? plumbingType : plumbingType + defaultChildPlumbingSymbol;
                        currentDetailTableRow.PlumbingSize = plumbing!.Size.Replace("mm", "");
                        currentDetailTableRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo(currentDetailTableRow, mixConstructionItems);
                        currentDetailTableRow.IsReadOnlyPlumbingItems = !mixConstructionItems;
                        plumbingCount++;
                    }
                    else
                    {
                        if (currentDetailTableRow == detailTableRows.Last())
                        {
                            var plumbing = conduitsModels.FirstOrDefault(c => double.Parse(c.InnerCrossSectionalArea) >= currentPlumbingCrossSectionalArea);
                            parentDetailRow.PlumbingType = parentDetailRow.IsParentRoute ? plumbingType : plumbingType + defaultChildPlumbingSymbol;
                            parentDetailRow.PlumbingSize = plumbing!.Size.Replace("mm", "");
                            parentDetailRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo(parentDetailRow, mixConstructionItems);
                            parentDetailRow.IsReadOnlyPlumbingItems = !mixConstructionItems;
                            if (!detailTableRowsGroupByPlumbingType.ContainsKey(parentDetailRow.PlumbingIdentityInfo))
                                detailTableRowsGroupByPlumbingType.Add(parentDetailRow.PlumbingIdentityInfo, childDetailRows);
                            else
                            {
                                detailTableRowsGroupByPlumbingType[parentDetailRow.PlumbingIdentityInfo].AddRange(childDetailRows);
                                detailTableRowsGroupByPlumbingType[parentDetailRow.PlumbingIdentityInfo].Add(currentDetailTableRow);
                            }
                            plumbingCount++;
                        }

                        if (currentDetailTableRow == detailTableRows.First()) continue;
                        currentDetailTableRow.PlumbingType = defaultChildPlumbingSymbol;
                        currentDetailTableRow.PlumbingSize = defaultChildPlumbingSymbol;
                        currentDetailTableRow.NumberOfPlumbing = defaultChildPlumbingSymbol;
                        currentDetailTableRow.IsReadOnlyPlumbingItems = true;
                        childDetailRows.Add(currentDetailTableRow);
                    }
                }

                foreach (var (plumbingIdentityInfo, detailTableRowsWithSamePlumbing) in detailTableRowsGroupByPlumbingType)
                {
                    foreach (var detailTableRow in detailTableRowsWithSamePlumbing)
                    {
                        detailTableRow.PlumbingIdentityInfo = plumbingIdentityInfo;
                    }
                }
            }

            foreach (var detailTableRowsWithSameSignalType in detailTableModelsBySignalType)
            {
                foreach (var detailTableRow in detailTableRowsWithSameSignalType.Where(d => d.PlumbingSize != defaultChildPlumbingSymbol).ToList())
                {
                    detailTableRow.NumberOfPlumbing = plumbingCount.ToString();
                }
            }
        }

        private static string GetDetailTableRowPlumbingIdentityInfo(DetailTableModel detailTableRow, bool mixConstructionItems)
        {
            return mixConstructionItems ?
              string.Join("-", detailTableRow.PlumbingType + detailTableRow.PlumbingSize, detailTableRow.SignalType, detailTableRow.RouteName) :
              string.Join("-", detailTableRow.PlumbingType + detailTableRow.PlumbingSize, detailTableRow.SignalType, detailTableRow.RouteName, detailTableRow.ConstructionItems);
        }

        private List<Element> GetToConnectorAndConduitOfRoute(Document document, IReadOnlyCollection<Element> allConnectors, string routeName)
        {
            var conduitsAndConnectorOfRoute = document.GetAllElements<Element>().OfCategory(BuiltInCategorySets.Conduits).Where(c => c.GetRouteName() == routeName).ToList();
            foreach (var conduit in conduitsAndConnectorOfRoute)
            {
                var toEndPoint = conduit.GetNearestEndPoints(false).ToList();
                if (!toEndPoint.Any()) continue;
                var toEndPointKey = toEndPoint.First().Key;
                var toElementUniqueId = toEndPointKey.GetElementUniqueId();
                if (string.IsNullOrEmpty(toElementUniqueId)) continue;
                var toConnector = allConnectors.FirstOrDefault(c => c.UniqueId == toElementUniqueId);
                if (toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint()) continue;
                conduitsAndConnectorOfRoute.Add(toConnector);
                return conduitsAndConnectorOfRoute;
            }

            return conduitsAndConnectorOfRoute;
        }

        private string GetParentRouteName(Document document, List<string> routeNames)
        {
            foreach (var routeName in routeNames)
            {
                var route = document.CollectRoutes(AddInType.Electrical).FirstOrDefault(x => x.RouteName == routeName);
                if (route == null) continue;
                var parentRouteName = route.GetParentBranches().ToList().LastOrDefault()?.RouteName;
                if (string.IsNullOrEmpty(parentRouteName) || parentRouteName == routeName)
                {
                    return routeName;
                }
            }

            return string.Empty;
        }

        private void AddDetailTableModelRow(Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<DetailTableModel> detailTableModelsData, ICollection<DetailTableModel> detailTableModels, List<Element> pickedObjects, DetailSymbolModel detailSymbolModel, bool isParentRoute, bool mixConstructionItems)
        {
            var ceedCode = string.Empty;
            var constructionClassification = string.Empty;
            var signalType = string.Empty;
            var wireType = string.Empty;
            var wireSize = string.Empty;
            var wireStrip = string.Empty;
            var remark = string.Empty;
            var groupId = string.Empty;
            double wireCrossSectionalArea = 0;
            var element = pickedObjects.FirstOrDefault(p => p.UniqueId == detailSymbolModel.ConduitId);
            string floor = doc.GetElementById<Level>(element!.GetLevelId())?.Name ?? string.Empty;
            string constructionItem = element!.LookupParameter("Construction Item").AsString() ?? DefaultConstructionItems;
            var plumbingItems = constructionItem;
            string isEcoMode = element.LookupParameter("IsEcoMode").AsString();
            string plumbingType = detailSymbolModel.PlumbingType;

            var ceedModel = ceedStorable.CeedModelData.FirstOrDefault(x => x.CeedSetCode == detailSymbolModel.Code && x.GeneralDisplayDeviceSymbol == detailSymbolModel.DeviceSymbol);
            if (ceedModel != null && !string.IsNullOrEmpty(ceedModel.CeedSetCode) && !string.IsNullOrEmpty(ceedModel.CeedModelNumber))
            {
                ceedCode = ceedModel.CeedSetCode;
                remark = ceedModel.GeneralDisplayDeviceSymbol;
                var hiroiCdModel = !string.IsNullOrEmpty(isEcoMode) && bool.Parse(isEcoMode) ? hiroiSetCdMasterEcoModelData.FirstOrDefault(x => x.SetCode == ceedModel.CeedSetCode) : hiroiSetCdMasterNormalModelData.FirstOrDefault(x => x.SetCode == ceedModel.CeedSetCode);
                var hiroiSetModels = !string.IsNullOrEmpty(isEcoMode) && bool.Parse(isEcoMode) ? hiroiSetMasterEcoModelData.Where(x => x.ParentPartModelNumber.Contains(ceedModel.CeedModelNumber)).Skip(1) : hiroiSetMasterNormalModelData.Where(x => x.ParentPartModelNumber.Contains(ceedModel.CeedModelNumber)).Skip(1);
                constructionClassification = hiroiCdModel?.ConstructionClassification;
                foreach (var item in hiroiSetModels)
                {
                    List<string> listMaterialCode = new List<string>();
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
                        var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault(w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ((w.NumberOfHeartsOrLogarithm == "0" && wireStrip == "0") || (w.NumberOfHeartsOrLogarithm != "0" && wireStrip == w.NumberOfHeartsOrLogarithm + w.COrP)));
                        if (wiresAndCablesModel == null) continue;
                        signalType = wiresAndCablesModel.Classification;
                        wireCrossSectionalArea = double.Parse(wiresAndCablesModel.CrossSectionalArea);
                    }
                }
            }

            if (detailTableModelsData.Any())
            {
                var oldDetailTableRow = detailTableModelsData.FirstOrDefault(d => d.DetailSymbolId == detailSymbolModel.DetailSymbolId && d.RouteName == detailSymbolModel.RouteName);
                if (oldDetailTableRow != null)
                {
                    groupId = oldDetailTableRow.GroupId;
                    plumbingItems = mixConstructionItems ? oldDetailTableRow.PlumbingItems : constructionItem;
                }
            }

            var detailTableRow = new DetailTableModel(false, floor, ceedCode, detailSymbolModel.DetailSymbol, detailSymbolModel.DetailSymbolId, wireType, wireSize, wireStrip, "1", string.Empty, string.Empty, string.Empty, plumbingType, string.Empty, string.Empty, constructionClassification, signalType, constructionItem, plumbingItems, remark, wireCrossSectionalArea, detailSymbolModel.CountCableSamePosition, detailSymbolModel.RouteName, isEcoMode, isParentRoute, !isParentRoute, string.Empty, groupId, true);
            detailTableModels.Add(detailTableRow);
        }

        private bool CheckMixConstructionItems(List<DetailTableModel> detailTableModelsData, HashSet<string> detailSymbolIds)
        {
            var detailTableModelRowGroupMixConstructionItems = detailTableModelsData.FirstOrDefault(d => detailSymbolIds.Contains(d.DetailSymbolId) && !string.IsNullOrEmpty(d.GroupId) && bool.Parse(d.GroupId.Split('-').First()));
            var detailTableModelRowGroupNoMixConstructionItems = detailTableModelsData.FirstOrDefault(d => detailSymbolIds.Contains(d.DetailSymbolId) && !string.IsNullOrEmpty(d.GroupId) && !bool.Parse(d.GroupId.Split('-').First()));
            return detailTableModelRowGroupNoMixConstructionItems == null && detailTableModelRowGroupMixConstructionItems != null;
        }

        public class ComboboxItemType
        {
            public string Type { get; }
            public string Name { get; }

            public ComboboxItemType(string type, string name)
            {
                Type = type;
                Name = name;
            }
        }
    }
}
