using System.Linq;
using System.Windows.Forms;
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
            var element = uiDoc.Selection.PickObject(ObjectType.Element, textNoteFilter);
            if (null == element)
            {
                return Result.Cancelled;
            }

            var textNote = doc.GetAllElements<TextNote>().ToList().FirstOrDefault(x => x.Id == element.ElementId);
            if (textNote != null)
            {
                pickedText = textNote.Text.Trim();
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
