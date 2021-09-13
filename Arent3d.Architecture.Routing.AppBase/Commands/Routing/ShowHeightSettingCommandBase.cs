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
          viewModel.GetStorable().Save();


          Dictionary<ElementId, List<FamilyInstance>> connectors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                                                                                         .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                                                                                                         .AsEnumerable()
                                                                                                         .OfType<FamilyInstance>()
                                                                                                         .GroupBy(x => x.LevelId)
                                                                                                         .ToDictionary(g => g.Key, g => g.ToList());
          var conduits = new FilteredElementCollector(document).OfClass(typeof(Conduit))
                                                               .OfCategory(BuiltInCategory.OST_Conduit)
                                                               .ToElements()
                                                               .OfType<Conduit>();

          foreach (Level level in settingStorables.Levels)
          {
            var heightConnector = settingStorables.HeightOfConnectorsByLevel[level.Name].MillimetersToRevitUnits();
            // Set Elevation from floor for all connector on this floor
            if (settingStorables.HeightOfConnectorsByLevel.ContainsKey(level.Name) && connectors.ContainsKey(level.Id))
            {
              foreach (var item in connectors[level.Id])
              {
                item.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(heightConnector);
              }
            }

            // Set Elevation for level
            if (settingStorables.HeightOfLevels.ContainsKey(level.Name))
            {
              level.Elevation = settingStorables.ElevationOfLevels[level.Name].MillimetersToRevitUnits();
            }

            // Set Top Elevation for conduit
            foreach (Conduit conduit in conduits)
            {
              if (conduit.ReferenceLevel.Id == level.Id)
              {
                conduit.get_Parameter(BuiltInParameter.RBS_CTC_BOTTOM_ELEVATION).Set(heightConnector);
              }
            }
          }
        }

        return Result.Succeeded;

      });
    }

  }
}