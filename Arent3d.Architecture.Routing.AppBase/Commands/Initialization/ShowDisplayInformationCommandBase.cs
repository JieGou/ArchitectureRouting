using System.Collections.ObjectModel;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Storable.Model;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowDisplayInformationCommandBase : IExternalCommand
  {
    protected UIDocument UiDocument { get; private set; } = null!;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument;
      Document document = UiDocument.Document;

      // get data of Cns Category from snoop DB
      ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>();
      queryData.Add(new QueryData("ST_1","abc_1","123_a","def_1"));
      queryData.Add(new QueryData("ST_2","abc_2","123_b","def_5"));
      queryData.Add(new QueryData("ST_3","abc_3","123_c","def_11"));
      queryData.Add(new QueryData("ST_4","abc_4","123_v","def_15"));
      queryData.Add(new QueryData("ST_5","abc_5","123_v","def_16"));
      queryData.Add(new QueryData("ST_6","abc_6","123_d","def_17"));
      queryData.Add(new QueryData("ST_7","abc_7","123_z","def_18"));
      queryData.Add(new QueryData("ST_8","abc_8","123_dd","def_1"));
      string symbolLink = "https://www.publicdomainpictures.net/pictures/260000/velka/loading-symbol.jpg";
      DisplayInformationModel displayInformationModels = new DisplayInformationModel(queryData,symbolLink);
      DisplayInformationViewModel viewModel = new DisplayInformationViewModel(displayInformationModels);
      var dialog = new DisplayInformationDialog(viewModel);
      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }
  }
}