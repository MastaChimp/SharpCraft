using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TinkerWorX.SharpCraft.Game.Jass;

namespace TinkerWorX.SharpCraft.Game.Core
{
    /// <summary>
    /// This is the internal COrderTarget type.
    /// The size has not been verified.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OrderTarget // sizeof = 96 / 0x60
    {
        public IntPtr VTable;
        public Int32 Field04;
        public Int32 Field08;
        public Int32 Field0C;

        public Int32 Field10;
        public IntPtr Field14;
        public Int32 Field18;
        public Int32 Field1C;

        public Int32 Field20;
        public JassOrder Order;
        public Int32 Field28;
        public Int32 Field2C;

        public Int32 Field30;
        public Int32 Field34;
        public Int32 Field38;
        public Int32 Field3C;

        public Int32 Field40;
        public IntPtr Field44;
        public Single X;
        public IntPtr Field4C;

        public Single Y;
        public Int32 Field54;
        public Int32 Field58;
        public Int32 Field5C;
    }
}
