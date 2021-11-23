using System.Linq;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
    public abstract class ShowCeeDDetailInformationCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            string pickedText = "";
            var uiDoc = commandData.Application.ActiveUIDocument;
            TextNotePickFilter textNoteFilter = new TextNotePickFilter();
            try
            {
                var element = uiDoc.Selection.PickObject(ObjectType.Element, textNoteFilter);
                var textNote = doc.GetAllElements<TextNote>().ToList().FirstOrDefault(x => x.Id == element.ElementId);
                if (textNote != null)
                {
                    pickedText = textNote.Text.Trim();
                }
            }
            catch
            {
                return Result.Cancelled;
            }

            var dialog = new CeedDetailInformationDialog(doc, pickedText);
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

        private class TextNotePickFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
            {
                return (e.Category.Id.IntegerValue.Equals((int) BuiltInCategory.OST_TextNotes));
            }

            public bool AllowReference(Reference r, XYZ p)
            {
                return false;
            }
        }
    }
}
