using System ;
using System.Windows.Forms ;
using Autodesk.Revit.DB ;
using Form = System.Windows.Forms.Form ;
using Point = System.Drawing.Point ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CustomMsgBox : Form {
    private string _msg = string.Empty;
    public CustomMsgBox(string msg) {
      InitializeComponent();
      this.MinimizeBox = false;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      message.Text = msg ;
      this.StartPosition = FormStartPosition.CenterScreen ;
    }
  }

  public static class MyMessageBox
  {
    public static DialogResult Show(string message)
    {
      return Show(message, string.Empty);
    }
    public static DialogResult Show(string message, string title)
    {
      CustomMsgBox msg =new CustomMsgBox(message);
      msg.Text = title;
      return msg.ShowDialog();
    }
  }
}