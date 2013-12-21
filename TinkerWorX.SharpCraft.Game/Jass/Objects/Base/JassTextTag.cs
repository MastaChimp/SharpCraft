using System;

namespace TinkerWorX.SharpCraft.Game.Jass
{
    [JassType("Htexttag;")]
    public partial struct JassTextTag
    {
        public readonly IntPtr Handle;
        
        public JassTextTag(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}