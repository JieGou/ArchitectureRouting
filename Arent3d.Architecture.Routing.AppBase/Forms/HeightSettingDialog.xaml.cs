﻿using Arent3d.Architecture.Routing.AppBase.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// Interaction logic for HeightSettingDialog.xaml
  /// </summary>
  public partial class HeightSettingDialog : Window
  {
    public HeightSettingDialog()
    {
      InitializeComponent();
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      // TODO apply data and save to db
      DialogResult = true;
      Close();
    }

    private void TextBox_GotFocus( object sender, RoutedEventArgs e )
    {
      var textBox = (TextBox)sender;
      textBox.SelectAll();
    }

    private void Window_ContentRendered( object sender, EventArgs e )
    {
      HeightOfLv1.Focus();
    }
  }
}
