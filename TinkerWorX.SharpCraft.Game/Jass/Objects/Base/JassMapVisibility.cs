using System;

namespace TinkerWorX.SharpCraft.Game.Jass
{
    [JassType("Hmapvisibility;")]
    [Serializable]
    public partial struct JassMapVisibility
    {
        public readonly IntPtr Handle;
        
        public JassMapVisibility(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}
