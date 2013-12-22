using System;

namespace TinkerWorX.SharpCraft.Game.Jass
{
    [JassType("Hgroup;")]
    public partial struct JassGroup
    {
        public readonly IntPtr Handle;
        
        public JassGroup(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}
