using System.ComponentModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  partial class CustomMsgBox
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null ;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) ) {
        components.Dispose() ;
      }

      base.Dispose( disposing ) ;
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.message = new System.Windows.Forms.Label() ;
      this.SuspendLayout() ;
      // 
      // message
      // 
      this.message.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte) ( 0 ) ) ) ;
      this.message.Location = new System.Drawing.Point( 12, 9 ) ;
      this.message.Name = "message" ;
      this.message.Size = new System.Drawing.Size( 320, 80 ) ;
      this.message.TabIndex = 0 ;
      this.message.Text = "message" ;
      // 
      // CustomMsgBox
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F ) ;
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font ;
      this.AutoScroll = true ;
      this.BackColor = System.Drawing.Color.White ;
      this.ClientSize = new System.Drawing.Size( 345, 90 ) ;
      this.Controls.Add( this.message ) ;
      this.Name = "CustomMsgBox" ;
      this.ShowIcon = false ;
      this.Text = "CustomMsgBox" ;
      this.ResumeLayout( false ) ;
    }

    private System.Windows.Forms.Label message ;

    #endregion
  }
}