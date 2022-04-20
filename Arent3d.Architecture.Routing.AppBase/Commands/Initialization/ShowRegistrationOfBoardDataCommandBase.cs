using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
    public abstract class ShowRegistrationOfBoardDataCommandBase : IExternalCommand
    {
        private const string DefaultConstructionItem = "未設定" ;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document ;

            var dlgRegistrationOfBoardDataModel = new RegistrationOfBoardDataDialog( commandData.Application ) ;

            dlgRegistrationOfBoardDataModel.ShowDialog() ;
            if ( ! ( dlgRegistrationOfBoardDataModel.DialogResult ?? false ) ) return Result.Cancelled ;
            ICollection<ElementId> groupIds = new List<ElementId>() ;
            if ( string.IsNullOrEmpty( dlgRegistrationOfBoardDataModel.SelectedSignalDestination ) && string.IsNullOrEmpty( dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel ) ) return Result.Succeeded ;
            Element? element = null ;

            var result = doc.Transaction(
              "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault(
                "Placement Device Symbol"), _ =>
              {
                var uiDoc = commandData.Application.ActiveUIDocument;

                var (originX, originY, originZ) = uiDoc.Selection.PickPoint("Connectorの配置場所を選択して下さい。");
                var level = uiDoc.ActiveView.GenLevel;
                var heightOfConnector =
                  doc.GetHeightSettingStorable()[level].HeightOfConnectors.MillimetersToRevitUnits();
                element = GenerateConnector(uiDoc, originX, originY, heightOfConnector, level, dlgRegistrationOfBoardDataModel.IsFromPowerConnector);

                var registrationCode = dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel + "-" +
                                       dlgRegistrationOfBoardDataModel.SelectedSignalDestination;

                if (element is FamilyInstance familyInstance)
                {
                  element.SetProperty(ElectricalRoutingElementParameter.CeedCode, registrationCode);
                  element.SetProperty(ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem);
                  familyInstance.SetConnectorFamilyType(ConnectorFamilyType.Power);
                }

                ElementId defaultTextTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                var noteWidth = .05;

                // make sure note width works for the text type
                var minWidth = TextElement.GetMinimumAllowedWidth(doc, defaultTextTypeId);
                var maxWidth = TextElement.GetMaximumAllowedWidth(doc, defaultTextTypeId);
                if (noteWidth < minWidth)
                {
                  noteWidth = minWidth;
                }
                else if (noteWidth > maxWidth)
                {
                  noteWidth = maxWidth;
                }

                TextNoteOptions opts = new(defaultTextTypeId) {HorizontalAlignment = HorizontalTextAlignment.Left};


                var text = dlgRegistrationOfBoardDataModel.IsFromPowerConnector
                  ? dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel
                  : dlgRegistrationOfBoardDataModel.SelectedSignalDestination;
                var txtPosition = new XYZ(originX - 2, originY + 4, heightOfConnector);
                var textNote = TextNote.Create(doc, doc.ActiveView.Id, txtPosition, noteWidth,
                  text, opts);

                // create group of selected element and new text note
                groupIds.Add(element.Id);
                groupIds.Add(textNote.Id);
              

                return Result.Succeeded;
              });

            if (!groupIds.Any()) return result;
            using Transaction t = new(doc, "Create connector group.");
            t.Start();
            doc.Create.NewGroup(groupIds);
            t.Commit();

            return result;
        }

        private Element GenerateConnector(UIDocument uiDocument, double originX, double originY, double originZ, Level level, bool isFromPowerConnector)
        {
          var familySymbols = uiDocument.Document.GetAllElements<FamilySymbol>()
            .OfCategory(BuiltInCategory.OST_ElectricalEquipment).ToList();

          ElementId id;

          id = isFromPowerConnector ? new ElementId(196166) : new ElementId(175106);
          
          var symbol = uiDocument.Document.GetElement(id) as FamilySymbol;

          var routingSymbol = symbol ?? throw new InvalidOperationException();

          if (isFromPowerConnector)
          {
            routingSymbol.Instantiate(new XYZ(originX + 2, originY + 2, originZ + 50), level, StructuralType.NonStructural);
          }
          return routingSymbol.Instantiate(new XYZ(originX, originY, originZ), level, StructuralType.NonStructural);
        }

    }
}