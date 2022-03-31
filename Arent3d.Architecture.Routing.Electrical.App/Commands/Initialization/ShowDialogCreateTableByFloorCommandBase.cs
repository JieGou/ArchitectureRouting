using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Arent3d.Architecture;
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
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.ShowElectricSymbolsCommandBase;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.CreateDetailTableCommandBase;
using Arent3d.Utility;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    public class ShowDialogCreateTableByFloorCommandBase : IExternalCommand
    {
        private const string DefaultConstructionItems = "未設定";
        private const string DefaultChildPlumbingSymbol = "↑";
        private const string NoPlumping = "配管なし";
        private const string DetailTableType = "Detail Table";
        private const string ElectricalSymbolTableType = "Electrical Symbol Table";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var uiDoc = commandData.Application.ActiveUIDocument;

            var dialog = new CreateTableByFloors(doc);
            dialog.ShowDialog();
            string tableType = dialog.SelectedTableType;

            List<ElementId> levelIds = dialog.LevelList.Where(t => t.IsSelected == true).Select(p => p.LevelId).ToList();

            switch (tableType)
            {
                case DetailTableType:
                    return CreateDetailTable(doc, uiDoc, levelIds);
                case ElectricalSymbolTableType:
                    return CreateElectricalTable(doc, uiDoc, levelIds);
                default:
                    return Result.Succeeded;
            }
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

        private Result CreateDetailTable(Document doc, UIDocument uiDoc, List<ElementId> levelIds)
        {
            const string defaultParentPlumbingType = "E";
            var csvStorable = doc.GetCsvStorable();
            var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData;
            var conduitsModelData = csvStorable.ConduitsModelData;
            var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData;
            var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData;
            var hiroiMasterModelData = csvStorable.HiroiMasterModelData;
            var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData;
            var hiroiSetCdMasterEcoModelData = csvStorable.HiroiSetCdMasterEcoModelData;
            var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault();
            var detailTableModelsData = doc.GetDetailTableStorable().DetailTableModelData;
            var detailTableModels = new ObservableCollection<DetailTableModel>();
            var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable();
            var cnsStorable = doc.GetCnsSettingStorable();
            bool mixConstructionItems;
            try
            {
                var allConduits = new FilteredElementCollector(doc).OfClass(typeof(Conduit)).OfCategory(BuiltInCategory.OST_Conduit).AsEnumerable().OfType<Conduit>();
                var conduitsByFloors = allConduits.Where(x => levelIds.Contains(x.ReferenceLevel.Id)).ToList();
                var elementsByFloors = conduitsByFloors.Cast<Element>().ToList();
                var pickedObjectIds = conduitsByFloors.Select(p => p.UniqueId).ToList();
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
                        AddDetailTableModelRow(doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, elementsByFloors, parentDetailSymbolModel!, true, mixConstructionItems);
                        routeNames = routeNames.Where(n => n != parentRouteName).OrderByDescending(n => n).ToList();
                    }

                    foreach (var childDetailSymbolModel in from routeName in routeNames select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault(d => d.RouteName == routeName))
                    {
                        AddDetailTableModelRow(doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, elementsByFloors, childDetailSymbolModel, false, mixConstructionItems);
                    }
                }

                SortDetailTableModel(ref detailTableModels);
                SetPlumbingData(conduitsModelData, ref detailTableModels, defaultParentPlumbingType, mixConstructionItems);
            }
            catch
            {
                return Result.Cancelled;
            }

            return doc.Transaction("TransactionName.Commands.Routing.CreateDetailTable".GetAppStringByKeyOrDefault("Set detail table"), _ =>
            {
                var level = uiDoc.ActiveView.GenLevel;
                CreateDetailTableSchedule(doc, detailTableModels, level.Name);
                return Result.Succeeded;
            });
        }

        private bool CheckMixConstructionItems(List<DetailTableModel> detailTableModelsData, HashSet<string> detailSymbolIds)
        {
            var detailTableModelRowGroupMixConstructionItems = detailTableModelsData.FirstOrDefault(d => detailSymbolIds.Contains(d.DetailSymbolId) && !string.IsNullOrEmpty(d.GroupId) && bool.Parse(d.GroupId.Split('-').First()));
            var detailTableModelRowGroupNoMixConstructionItems = detailTableModelsData.FirstOrDefault(d => detailSymbolIds.Contains(d.DetailSymbolId) && !string.IsNullOrEmpty(d.GroupId) && !bool.Parse(d.GroupId.Split('-').First()));
            return detailTableModelRowGroupNoMixConstructionItems == null && detailTableModelRowGroupMixConstructionItems != null;
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

        private static void CreateDetailTableSchedule(Document document, IReadOnlyCollection<DetailTableModel> detailTableModels, string level)
        {
            string scheduleName = "Revit.Detail.Table.Name".GetDocumentStringByKeyOrDefault(document, "Detail Table") + DateTime.Now.ToString(" yyyy-MM-dd HH-mm-ss");
            var detailTable = document.GetAllElements<ViewSchedule>().SingleOrDefault(v => v.Name.Contains(scheduleName));
            if (detailTable == null)
            {
                detailTable = ViewSchedule.CreateSchedule(document, new ElementId(BuiltInCategory.OST_Conduit));
                detailTable.Name = scheduleName;
            }

            InsertDetailTableDataIntoSchedule(detailTable, detailTableModels, level);
            MessageBox.Show("集計表 \"" + scheduleName + "\" を作成しました", "Message");
        }

        private static void InsertDetailTableDataIntoSchedule(ViewSchedule viewSchedule, IReadOnlyCollection<DetailTableModel> detailTableModels, string level)
        {
            const int columnCount = 5;
            const int maxCharOfCell = 4;
            const double minColumnWidth = 0.05;
            var rowData = 1;
            var maxCharOfPlumbingTypeCell = 0;
            var maxCharOfRemarkCell = 0;

            TableCellStyleOverrideOptions tableStyleOverride = new()
            {
                HorizontalAlignment = true,
                BorderLineStyle = true,
                BorderLeftLineStyle = true,
                BorderRightLineStyle = true,
                BorderTopLineStyle = false,
                BorderBottomLineStyle = false
            };
            TableCellStyle cellStyle = new();
            cellStyle.SetCellStyleOverrideOptions(tableStyleOverride);
            cellStyle.FontHorizontalAlignment = HorizontalAlignmentStyle.Left;

            TableData tableData = viewSchedule.GetTableData();
            TableSectionData tsdHeader = tableData.GetSectionData(SectionType.Header);

            for (var i = 0; i <= columnCount; i++)
            {
                if (i != 2) tsdHeader.InsertColumn(i);
            }

            var detailTableModelsGroupByDetailSymbol = detailTableModels.GroupBy(d => d.DetailSymbol).ToDictionary(g => g.Key, g => g.ToList());
            var rowCount = detailTableModels.Count + detailTableModelsGroupByDetailSymbol.Count;
            for (var i = 0; i < rowCount; i++)
            {
                tsdHeader.InsertRow(tsdHeader.FirstRowNumber);
            }

            tsdHeader.MergeCells(new TableMergedCell(0, 0, 0, columnCount));
            tsdHeader.SetCellText(0, 0, level + "階平面図");
            tsdHeader.SetCellStyle(0, 0, cellStyle);

            var isSetPipeForCoWindingWiring = false;
            foreach (var (detailSymbol, detailTableModelsSameWithDetailSymbol) in detailTableModelsGroupByDetailSymbol)
            {
                tsdHeader.MergeCells(new TableMergedCell(rowData, 0, rowData, columnCount));
                tsdHeader.SetCellText(rowData, 0, detailSymbol);
                tsdHeader.SetCellStyle(rowData, 0, cellStyle);
                rowData++;
                foreach (var rowDetailTableModel in detailTableModelsSameWithDetailSymbol)
                {
                    var wireType = rowDetailTableModel.WireType + rowDetailTableModel.WireSize;
                    var wireStrip = string.IsNullOrEmpty(rowDetailTableModel.WireStrip) ? string.Empty : "－" + rowDetailTableModel.WireStrip;
                    var (plumbingType, numberOfPlumbing) = GetPlumbingType(rowDetailTableModel.ConstructionClassification, rowDetailTableModel.PlumbingType, rowDetailTableModel.PlumbingSize, rowDetailTableModel.NumberOfPlumbing, ref isSetPipeForCoWindingWiring);
                    tsdHeader.SetCellText(rowData, 0, wireType);
                    tsdHeader.SetCellText(rowData, 1, wireStrip);
                    tsdHeader.SetCellText(rowData, 2, "x" + rowDetailTableModel.WireBook);
                    tsdHeader.SetCellText(rowData, 3, plumbingType);
                    tsdHeader.SetCellText(rowData, 4, numberOfPlumbing);
                    tsdHeader.SetCellText(rowData, 5, rowDetailTableModel.Remark);

                    if (plumbingType.Length > maxCharOfPlumbingTypeCell) maxCharOfPlumbingTypeCell = plumbingType.Length;
                    if (rowDetailTableModel.Remark.Length > maxCharOfRemarkCell) maxCharOfRemarkCell = rowDetailTableModel.Remark.Length;
                    rowData++;
                }
            }

            for (var i = 0; i <= columnCount; i++)
            {
                var columnWidth = i switch
                {
                    0 => minColumnWidth * 2,
                    3 when maxCharOfPlumbingTypeCell > maxCharOfCell => minColumnWidth * Math.Ceiling((double)maxCharOfPlumbingTypeCell / maxCharOfCell),
                    5 when maxCharOfRemarkCell > maxCharOfCell => minColumnWidth * Math.Ceiling((double)maxCharOfRemarkCell / maxCharOfCell),
                    _ => minColumnWidth
                };
                tsdHeader.SetColumnWidth(i, columnWidth);
                tsdHeader.SetCellStyle(i, cellStyle);
            }

            if (isSetPipeForCoWindingWiring)
                MessageBox.Show("施工区分「冷媒管共巻配線」の電線は配管が設定されているので、再度ご確認ください。", "Error");
        }
        private static (string, string) GetPlumbingType(string constructionClassification, string plumbingType, string plumbingSize, string numberOfPlumbing, ref bool isSetPipeForCoWindingWiring)
        {
            const string korogashi = "コロガシ";
            const string rack = "ラック";
            const string coil = "共巻";
            if (plumbingType == DefaultChildPlumbingSymbol)
            {
                plumbingSize = string.Empty;
                numberOfPlumbing = string.Empty;
            }
            else
            {
                plumbingType = plumbingType.Replace(DefaultChildPlumbingSymbol, "");
            }

            if (constructionClassification == ConstructionClassificationType.天井隠蔽.GetFieldName() || constructionClassification == ConstructionClassificationType.打ち込み.GetFieldName() || constructionClassification == ConstructionClassificationType.露出.GetFieldName() || constructionClassification == ConstructionClassificationType.地中埋設.GetFieldName())
            {
                plumbingType = "(" + plumbingType + plumbingSize + ")";
                numberOfPlumbing = string.IsNullOrEmpty(numberOfPlumbing) || numberOfPlumbing == "1" ? string.Empty : "x" + numberOfPlumbing;
            }
            else if (constructionClassification == ConstructionClassificationType.天井コロガシ.GetFieldName() || constructionClassification == ConstructionClassificationType.フリーアクセス.GetFieldName())
            {
                plumbingType = "(" + korogashi + ")";
                numberOfPlumbing = string.Empty;
            }
            else if (constructionClassification == ConstructionClassificationType.ケーブルラック配線.GetFieldName())
            {
                plumbingType = "(" + rack + ")";
                numberOfPlumbing = string.Empty;
            }
            else if (constructionClassification == ConstructionClassificationType.冷媒管共巻配線.GetFieldName())
            {
                if (plumbingType != NoPlumping)
                {
                    isSetPipeForCoWindingWiring = true;
                }
                plumbingType = "(" + coil + ")";
                numberOfPlumbing = string.Empty;
            }
            else if (constructionClassification == ConstructionClassificationType.導圧管類.GetFieldName())
            {
                plumbingType = string.IsNullOrEmpty(plumbingType) ? string.Empty : "(" + plumbingType + plumbingSize + ")";
                numberOfPlumbing = string.IsNullOrEmpty(numberOfPlumbing) || numberOfPlumbing == "1" ? string.Empty : "x" + numberOfPlumbing;
            }
            else
            {
                plumbingType = string.Empty;
                numberOfPlumbing = string.Empty;
            }

            return (plumbingType, numberOfPlumbing);
        }
        private enum ConstructionClassificationType
        {
            天井隠蔽,
            天井コロガシ,
            打ち込み,
            フリーアクセス,
            露出,
            地中埋設,
            ケーブルラック配線,
            冷媒管共巻配線,
            漏水帯コロガシ,
            漏水帯配管巻,
            導圧管類
        }
    }
}
