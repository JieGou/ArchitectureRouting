using System.Collections.ObjectModel;
using System.Linq;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable.Model;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
    public abstract class ShowConduitInformationCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var uiDoc = commandData.Application.ActiveUIDocument;
            var wiresAndCablesModelData = doc.GetCsvStorable().WiresAndCablesModelData;
            var conduitsModelData = doc.GetCsvStorable().ConduitsModelData;
            ObservableCollection<ConduitInformationModel> conduitInformationModels = new ObservableCollection<ConduitInformationModel>();
            try
            {
                ConduitPickFilter conduitFilter = new ConduitPickFilter();
                var pickedObjects = uiDoc.Selection.PickObjects(ObjectType.Element, conduitFilter);
                foreach (var pickedObject in pickedObjects)
                {
                    var element = doc.GetElement(pickedObject.ElementId);
                    string diameter = element.LookupParameter("Diameter(Trade Size)").AsValueString().Replace(" ", "");
                    var conduitModel = conduitsModelData.FirstOrDefault(x => x.Size.Equals(diameter));
                    var wireType = wiresAndCablesModelData.FirstOrDefault();
                    if (conduitModel == null) continue;
                    if (wireType != null)
                        conduitInformationModels.Add(new ConduitInformationModel(true,
                            element.LookupParameter("Reference Level").AsValueString(),
                            conduitModel.PipingType,
                            wireType.WireType,
                            wireType.DiameterOrNominal,
                            wireType.COrP,
                            wireType.NumberOfConnections,
                            "8",
                            "9",
                            "10",
                            wireType.DOrA,
                            wireType.FinishedOuterDiameter,
                            wireType.NumberOfHeartsOrLogarithm,
                            "14",
                            "15",
                            wireType.Classification,
                            conduitModel.Name,
                            wireType.Name,
                            "19"));
                }
            }
            catch
            {
                return Result.Cancelled;
            }

            ConduitInformationViewModel viewModel = new ConduitInformationViewModel(conduitInformationModels);
            var dialog = new ConduitInformationDialog(viewModel);
            dialog.ShowDialog();

            if (dialog.DialogResult ?? false)
            {
                return Result.Succeeded;
            }
            else
            {
                return Result.Cancelled;
            }
        }

        private class ConduitPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
            {
                return (e.Category.Id.IntegerValue.Equals((int) BuiltInCategory.OST_Conduit));
            }

            public bool AllowReference(Reference r, XYZ p)
            {
                return false;
            }
        }
    }
}