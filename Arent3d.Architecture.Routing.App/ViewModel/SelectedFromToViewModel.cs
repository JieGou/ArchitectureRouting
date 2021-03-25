using System;
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
        public static IList<double>? Diameters { get; set; }
        
        //SystemType 
        public static int SelectedSystemTypeIndex { get; set; }
        public static IList<MEPSystemType>? SystemTypes { get; set; }
        
        //CurveType
        public static  int SelectedCurveTypeIndex { get; set; }
        public static IList<MEPCurveType>? CurveTypes { get; set; }
        
        //Direct
        public static bool IsDirect { get; set; }
        
        //Dialog
        private static SelectedFromTo? OpenedDialog ;




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
        public static void ShowSelectedFromToDialog(UIDocument uiDocument, int diameterIndex, IList<double> diameters
            , int systemTypeIndex, IList<MEPSystemType> systemTypes, int curveTypeIndex, IList<MEPCurveType> curveTypes
            , Type type, bool direct, PointOnRoutePicker.PickInfo selectedPickInfo)
        {
            UiDoc = uiDocument;
            TargetPickInfo = selectedPickInfo;
            Diameters = diameters;
            SystemTypes = systemTypes;
            CurveTypes = curveTypes;
            IsDirect = direct;

            if ( OpenedDialog != null )
            {
                OpenedDialog.Close();    
            }
            var dialog = new SelectedFromTo(uiDocument.Document, diameters ,diameterIndex, 
                systemTypes, systemTypeIndex, CurveTypes, curveTypeIndex, type ,direct);
            dialog.Show();
            OpenedDialog = dialog ;
        }

        /// <summary>
        /// Set Dilaog Parameters and send PostCommand
        /// </summary>
        /// <param name="selectedDiameter"></param>
        /// <param name="selectedSystemType"></param>
        /// <param name="selectedDirect"></param>
        /// <returns></returns>
        public static bool ApplySelectedDiameter(int selectedDiameter, int selectedSystemType ,int selectedCurveType,bool selectedDirect)
        {
            if (UiDoc != null)
            {
                SelectedDiameterIndex = selectedDiameter;
                SelectedSystemTypeIndex = selectedSystemType;
                SelectedCurveTypeIndex = selectedCurveType;
                IsDirect = selectedDirect;
                UiDoc.Application.PostCommand<Commands.PostCommands.ApplySelectedFromToChangesCommand>() ;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reset Diameter List by Curve Type
        /// </summary>
        /// <param name="curveTypeIndex"></param>
        /// <returns></returns>
        public static IList<double> ResetNominalDiameters(int curveTypeIndex)
        {
            IList<double> resultDiameters = new List<double>();
        
            if ( UiDoc != null && TargetPickInfo != null && CurveTypes != null) 
            {
                RouteMEPSystem routeMepSystem = new RouteMEPSystem(UiDoc.Document, TargetPickInfo.Route);
                resultDiameters = routeMepSystem.GetNominalDiameters( CurveTypes[curveTypeIndex] ) ;
            }

            return resultDiameters ;
        }
        
    }
}