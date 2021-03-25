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
        
        //Diameter
        public static int CurrentIndex { get; set; }
        public static int SelectedDiameterIndex { get; set; }
        public static IList<double>? DiameterList { get; set; }
        
        //SystemTypd 
        public static int SelectedSystemTypeIndex { get; set; }
        public static IList<MEPSystemType>? SystemTypeList { get; set; }
        
        //Direct
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
        public static void ShowSelectedFromToDialog(UIDocument uiDocument, int diameterIndex, IList<double> diameterList
            , IList<MEPSystemType> systemTypeList, int systemTypeIndex,
            bool direct, PointOnRoutePicker.PickInfo selectedPickInfo)
        {
            UiDoc = uiDocument;
            TargetPickInfo = selectedPickInfo;
            DiameterList = diameterList;
            SystemTypeList = systemTypeList;
            IsDirect = direct;

            var dialog = new SelectedFromTo(uiDocument.Document, diameterList ,diameterIndex, 
                systemTypeList, systemTypeIndex, direct);
            dialog.Show();
        }

        /// <summary>
        /// Set Dilaog Parameters and send PostCommand
        /// </summary>
        /// <param name="selectedDiameter"></param>
        /// <param name="selectedSystemType"></param>
        /// <param name="selectedDirect"></param>
        /// <returns></returns>
        public static bool ApplySelectedDiameter(int selectedDiameter, int selectedSystemType ,bool selectedDirect)
        {
            if (UiDoc != null)
            {
                SelectedDiameterIndex = selectedDiameter;
                SelectedSystemTypeIndex = selectedSystemType;
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