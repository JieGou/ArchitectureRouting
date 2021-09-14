using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowHeightSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIDocument uIDocument = commandData.Application.ActiveUIDocument;
      Document document = uIDocument.Document;
      return document.Transaction("TransactionName.Commands.Routing.HeightSetting", _ =>
      {
        // get data of height setting from snoop DB
        HeightSettingStorable settingStorables = document.GetAllStorables<HeightSettingStorable>()
                                                         .AsEnumerable()
                                                         .DefaultIfEmpty(new HeightSettingStorable(document))
                                                         .First();


        var viewModel = new ViewModel.HeightSettingViewModel(settingStorables);
        var dialog = new HeightSettingDialog(viewModel);

        dialog.ShowDialog();

        if (dialog.DialogResult ?? false)
        {
          viewModel.SettingStorable.Save();

          Dictionary<ElementId, List<FamilyInstance>> connectors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                                                                                         .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                                                                                                         .AsEnumerable()
                                                                                                         .OfType<FamilyInstance>()
                                                                                                         .GroupBy(x => x.LevelId)
                                                                                                         .ToDictionary(g => g.Key, g => g.ToList());
          var conduits = new FilteredElementCollector(document).OfClass(typeof(Conduit))
                                                               .OfCategory(BuiltInCategory.OST_Conduit)
                                                               .AsEnumerable()
                                                               .OfType<Conduit>()
                                                               .GroupBy(x => x.ReferenceLevel.Id)
                                                               .ToDictionary(g => g.Key, g => g.ToList());

          foreach (Level level in settingStorables.Levels)
          {
            var heightConnector = settingStorables[level].HeightOfConnectors.MillimetersToRevitUnits();
            // Set Elevation from floor for all connector on this floor
            if (connectors.ContainsKey(level.Id))
            {
              foreach (var connector in connectors[level.Id])
              {
                var elevationFromFloor = connector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                if (elevationFromFloor != heightConnector)
                {
                  connector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(heightConnector);
                }
              }
            }

            // Set Top Elevation for conduit
            if (conduits.ContainsKey(level.Id))
            {
              foreach (Conduit conduit in conduits[level.Id])
              {
                var elevationFromFloor = conduit.get_Parameter(BuiltInParameter.RBS_CTC_BOTTOM_ELEVATION).AsDouble();
                if (elevationFromFloor != heightConnector)
                {
                  conduit.get_Parameter(BuiltInParameter.RBS_CTC_BOTTOM_ELEVATION).Set(heightConnector);
                }
              }
            }

            // Set Elevation for level
            if (level.Elevation != settingStorables[level].Elevation.MillimetersToRevitUnits())
            {
              level.Elevation = settingStorables[level].Elevation.MillimetersToRevitUnits();
            }

          }
        }

        return Result.Succeeded;

      });
    }

  }
}