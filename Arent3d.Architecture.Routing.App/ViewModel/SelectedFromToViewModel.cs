using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Arent3d.Architecture.Routing.App.Commands.Selecting;
using Arent3d.Architecture.Routing.App.Forms;
using Arent3d.Architecture.Routing.App.Commands;
using Arent3d.Architecture.Routing.App.Commands.PostCommands;
using Arent3d.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
    static class SelectedFromToViewModel 
    {
        private static UIDocument? UiDoc { get; set; }
        
        //Selecting PickInfo 
        public static PointOnRoutePicker.PickInfo? TargetPickInfo {get; set; }
        
        public static int CurrentIndex { get; set; }
        public static int SelectedIndex { get; set; }
        public static IList<double>? DiameterList { get; set; }
        
        public static bool IsDirect { get; set; }




        static SelectedFromToViewModel()
        {
            
        }

        /// <summary>
        /// Show SelectedFromTo.xaml
        /// </summary>
        /// <param name="uiDocument"></param>
        /// <param name="targetIndex"></param>
        /// <param name="diameterList"></param>
        /// <param name="direct"></param>
        /// <param name="selectedPickInfo"></param>
        public static void ShowSelectedFromToDialog(UIDocument uiDocument, int targetIndex, IList<double> diameterList,
            bool direct, PointOnRoutePicker.PickInfo selectedPickInfo)
        {
            UiDoc = uiDocument;
            TargetPickInfo = selectedPickInfo;
            DiameterList = diameterList;
            IsDirect = direct;

            var dialog = new SelectedFromTo(uiDocument.Document, diameterList, targetIndex, direct);
            dialog.Show();
        }

        /// <summary>
        /// Set Dilaog Parameters and send PostCommand
        /// </summary>
        /// <param name="selectedIndex"></param>
        /// <param name="selectedDirect"></param>
        /// <returns></returns>
        public static bool ApplySelectedDiameter(int selectedIndex, bool selectedDirect)
        {
            if (UiDoc != null)
            {
                SelectedIndex = selectedIndex;
                IsDirect = selectedDirect;
                UiDoc.Application.PostCommand<Commands.PostCommands.ApplySelectedFromToChangesCommand>() ;
                return true;
            }
            else
            {
                return false;
            }
        }
        
    }
}