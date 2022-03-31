using System ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.SwitchEcoNormalModeCommand", DefaultString = "Switch EcoNormal Mode" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class SwitchEcoNormalModeCommand: ChangeConduitModeCommandBase
  {
    private const string SchemaGuid = "DA4AAE5A-4EE1-45A8-B3E8-F790C84CC44F" ;
    private const string IsEcoFieldName = "IsEco" ;
    private const string EcoNormalModeSchema = "EcoNormalModeSchema" ;
    private void SetEcoNormalModeForProject(Document document,bool isEco)
    {
      var schemaBuilder = new SchemaBuilder(new Guid(SchemaGuid));
      schemaBuilder.SetSchemaName(EcoNormalModeSchema);
      schemaBuilder.AddSimpleField(IsEcoFieldName, typeof(bool)) ;
      var schema = schemaBuilder.Finish();
      var entity = new Entity(schema);
      entity.Set(IsEcoFieldName, isEco);
      document.ProjectInformation.SetEntity(entity);
    }
    private bool? IsProjectInEcoMode(Document document)
    {
      try {
        Schema schema = Schema.Lookup(new Guid(SchemaGuid));
        var entity = document.ProjectInformation.GetEntity(schema);
        return entity?.Get<bool>(IsEcoFieldName);
      }
      catch {
        return null ;
      }
    }
    public override Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;
      var isProjectInEcoMode = IsProjectInEcoMode( document ) ;
      var dialog = new SwitchEcoNormalModeDialog( commandData.Application, isProjectInEcoMode ) ;
      dialog.ShowDialog() ;
      if ( dialog.DialogResult == false ) return Result.Cancelled ;
      if ( dialog.ApplyForProject == true ) {
        isProjectInEcoMode = dialog.EcoNormalMode == EcoNormalMode.EcoMode ;
        FilteredElementCollector collector = new FilteredElementCollector(document);
        collector = collector.OfClass(typeof(FamilyInstance)) ;
        var conduitList = collector.ToElements().ToList();
        collector = new FilteredElementCollector(document);
        conduitList.AddRange( collector.OfClass(typeof( Conduit )).ToElements()) ;
        conduitList = conduitList.Where( elem => BuiltInCategorySets.ConnectorsAndConduits.Contains( elem.GetBuiltInCategory() )).ToList();
        collector = new FilteredElementCollector(document);
        collector = collector.OfClass(typeof(FamilyInstance)) ;
        var connectorList = collector.ToElements().ToList();
        collector = new FilteredElementCollector(document);
        connectorList.AddRange( collector.OfClass(typeof( TextNote )).ToElements()) ;
        connectorList = connectorList.Where( elem => ( elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures || elem.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalEquipment )).ToList() ;
        
        var listApplyConduit = ConduitUtil.GetConduitRelated(document, conduitList) ;
        SetModeForConduit( listApplyConduit, (bool) isProjectInEcoMode, document ) ;
        SetModeForConnector( connectorList, (bool) isProjectInEcoMode, document ) ;
        SetEcoNormalModeForProject( document, (bool)isProjectInEcoMode );
        MessageBox.Show(
          string.IsNullOrEmpty( message )
            ? "Dialog.Electrical.ChangeMode.Success".GetAppStringByKeyOrDefault( UPDATE_DATA_SUCCESS_MESSAGE )
            : message,
          "Dialog.Electrical.ChangeMode.Title".GetAppStringByKeyOrDefault( ELECTRICAL_CHANGE_MODE_TITLE ),
          MessageBoxButtons.OK ) ;
      }
      else {
        return base.Execute( commandData, ref message, elements );
      }
      return Result.Succeeded ;
    }
  }
}