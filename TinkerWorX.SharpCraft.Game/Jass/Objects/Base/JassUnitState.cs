using System;

namespace TinkerWorX.SharpCraft.Game.Jass
{
    [JassType("Hunitstate;")]
    [Serializable]
    public partial struct JassUnitState
    {
        public readonly IntPtr Handle;
        
        public JassUnitState(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}
