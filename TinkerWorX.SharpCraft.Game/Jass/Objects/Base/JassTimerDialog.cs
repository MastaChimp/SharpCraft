using System;

namespace TinkerWorX.SharpCraft.Game.Jass
{
    [JassType("Htimerdialog;")]
    [Serializable]
    public partial struct JassTimerDialog
    {
        public readonly IntPtr Handle;
        
        public JassTimerDialog(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}
