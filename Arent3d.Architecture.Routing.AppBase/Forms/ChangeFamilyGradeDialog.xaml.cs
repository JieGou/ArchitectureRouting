using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Arent3d.Revit.I18n;
using Arent3d.Utility;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
    public enum GradeMode
    {
        GradeFrom3To7,
        Grade1Grade2
    }

    public partial class ChangeFamilyGradeDialog
    {
        private const string GradeKey = "Dialog.Electrical.ChangeFamilyGradeDialog.GradeMode.Grade";
        private const string GradeDefaultString = "Grade ";

        public static readonly DependencyProperty GradeModeComboBoxIndexProperty
            = DependencyProperty.Register("GradeModeComboBoxIndex",
                                          typeof(int),
                                          typeof(ChangeFamilyGradeDialog),
                                          new PropertyMetadata(1));

        public ChangeFamilyGradeDialog(UIApplication uiApplication, bool? isInGrade3) : base(uiApplication)
        {
            InitializeComponent();
            if (isInGrade3 != null)
                GradeModeComboBox.SelectedIndex = isInGrade3 == true ? 0 : 1;
        }

        public IReadOnlyDictionary<GradeMode, string> GradeModes { get; } = new Dictionary<GradeMode, string>
        {
            [GradeMode.GradeFrom3To7] = $"{GradeKey.GetAppStringByKeyOrDefault(GradeDefaultString)}3~",
            [GradeMode.Grade1Grade2] = $"{GradeKey.GetAppStringByKeyOrDefault(GradeDefaultString)}1-2",
        };

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public GradeMode? SelectedMode
        {
            get => GetGradeModeOnIndex(GradeModes.Keys, (int)GetValue(GradeModeComboBoxIndexProperty));
            private set => SetValue(GradeModeComboBoxIndexProperty, GradeModeIndex(GradeModes.Keys, value));
        }

        private static GradeMode? GetGradeModeOnIndex(IEnumerable<GradeMode> gradeModes, int index)
        {
            if (index < 0) return null;
            return gradeModes.ElementAtOrDefault(index);
        }

        private static int GradeModeIndex(IEnumerable<GradeMode> gradeModes, GradeMode? gradeMode)
        {
            return (gradeMode is { } type ? gradeModes.IndexOf(type) : -1);
        }

        private void GradeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnValueChanged(EventArgs.Empty);
            var newValue = e.AddedItems.OfType<KeyValuePair<GradeMode, string>>().FirstOrDefault();
            SelectedMode = newValue.Key;
        }

        private void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        public event EventHandler? ValueChanged;
    }
}