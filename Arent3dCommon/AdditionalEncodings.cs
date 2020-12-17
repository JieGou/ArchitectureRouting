using System;
using System.Text;

#nullable enable

namespace Arent3d
{
  public static class AdditionalEncodings
  {
    public static Encoding UTF8NoBOM { get; } = new UTF8Encoding( false );
  }
}
