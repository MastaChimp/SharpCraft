using System;

namespace TinkerWorX.SharpCraft.Game.Jass
{
    [JassType("Htrackable;")]
    public partial struct JassTrackable
    {
        public readonly IntPtr Handle;
        
        public JassTrackable(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}