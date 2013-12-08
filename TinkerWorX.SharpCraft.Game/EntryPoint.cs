using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EasyHook;
using TinkerWorX.SharpCraft.Core;
using TinkerWorX.SharpCraft.Game.Core;
using TinkerWorX.SharpCraft.Game.Jass;
using Assembly = System.Reflection.Assembly;

namespace TinkerWorX.SharpCraft.Game
{
    public class EntryPoint : IEntryPoint
    {
        private PluginManager pluginManager;

        public EntryPoint(RemoteHooking.IContext context, String hackPath, String installPath)
        {
            try
            {
                // Settings passed from the launcher.
                WarcraftIII.HackPath = hackPath;
                WarcraftIII.InstallPath = installPath;

                Settings.Load(Path.Combine(WarcraftIII.HackPath, "settings.xml"));

                if (Settings.Current.IsDebugging)
                {
                    Kernel32.AllocConsole();
                    Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
                }
                Trace.AutoFlush = true;
                Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(WarcraftIII.HackPath, "debug.log")));
                Trace.WriteLine("-------------------");
                Trace.WriteLine(DateTime.Now);

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Low-Level exception!" + Environment.NewLine +
                    exception + Environment.NewLine +
                    "Aborting execution!",
                    this.GetType() + " (Constructor)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        public Assembly CurrentDomain_AssemblyResolve(Object sender, ResolveEventArgs args)
        {
            // Convert name to filename
            var file = args.Name.Split(',').First() + ".dll";

            // Search root directory
            if (File.Exists(Path.Combine(WarcraftIII.HackPath, file)))
                return Assembly.LoadFrom(Path.Combine(WarcraftIII.HackPath, file));

            // Search plugins directory
            if (File.Exists(Path.Combine(Path.Combine(WarcraftIII.HackPath, "plugins"), file)))
                return Assembly.LoadFrom(Path.Combine(Path.Combine(WarcraftIII.HackPath, "plugins"), file));

            return null;
        }

        public void Run(RemoteHooking.IContext context, String hackPath, String installPath)
        {
            try
            {
                this.pluginManager = new PluginManager();
                Trace.WriteLine("Loading plugins . . . ");
                this.pluginManager.LoadPlugins();
                Trace.WriteLine(" - Done!");
                Trace.WriteLine(String.Empty);

                Trace.WriteLine("Initializing WarcraftIII . . . ");
                WarcraftIII.Initialize();
                Trace.WriteLine(" - Done!");
                Trace.WriteLine(String.Empty);

                Trace.WriteLine("Initializing plugins . . . ");
                this.pluginManager.InitializePlugins();
                Trace.WriteLine(" - Done!");
                Trace.WriteLine(String.Empty);

                Trace.WriteLine("Initializing dev . . . ");
                Dev.Initialize();
                Trace.WriteLine(" - Done!");

                RemoteHooking.WakeUpProcess();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Low-Level exception!" + Environment.NewLine +
                    exception + Environment.NewLine +
                    "Aborting execution!",
                    this.GetType() + " (Run)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }
    }

    internal static class Dev
    {
        //native Cheat takes string cheatStr returns nothing
        private delegate void CheatDelegate(JassStringArg cheatStr);
        private static CheatDelegate _Cheat;

        //native SetUnitAnimation takes unit whichUnit, string whichAnimation returns nothing 
        private delegate void SetUnitAnimationDelegate(JassUnit whichUnit, JassStringArg whichAnimation);
        private static SetUnitAnimationDelegate _SetUnitAnimation;

        // native CreateTrigger takes nothing returns trigger
        private delegate JassTrigger CreateTriggerDelegate();
        private static CreateTriggerDelegate _CreateTrigger;

        // native TriggerExecute takes trigger whichTrigger returns nothing 
        private delegate void TriggerExecuteDelegate(JassTrigger trigger);
        private static TriggerExecuteDelegate _TriggerExecute;

        // native TriggerAddAction takes trigger whichTrigger, code actionFunc returns triggeraction
        private delegate JassTriggerAction TriggerAddActionDelegate(JassTrigger trigger, JassCode function);
        private static TriggerAddActionDelegate _TriggerAddAction;

        private static JassTrigger trigger;

        private static void CheatHook(JassStringArg cheat)
        {
            //if (cheat == "order_point")
            //    MessageBox.Show(cheat);
            //Trace.WriteLine(cheat);
            switch (cheat)
            {
                case "init":
                    //trigger = _CreateTrigger();
                    break;

                case "tick":
                    //Trace.WriteLine("WarcraftIII.IsMouseOverUI: " + WarcraftIII.IsMouseOverUI);
                    //Trace.WriteLine("Tick");
                    break;

                case "esc":
                    //Trace.WriteLine("ESC");
                    //_TriggerExecute(trigger);
                    break;

                default:
                    _Cheat(cheat);
                    break;
            }
        }

        private static void SetUnitAnimationHook(JassUnit whichUnit, JassStringArg whichAnimation)
        {
            switch (whichAnimation)
            {
                case "unit_debug":
                    Trace.WriteLine("unit_debug: 0x" + whichUnit.ToString("X8"));
                    Trace.WriteLine("unit_debug: 0x" + sub_6F3BDCB0(whichUnit).ToString("X8"));
                    MessageBox.Show(whichAnimation);
                    break;

                default:
                    _SetUnitAnimation(whichUnit, whichAnimation);
                    break;
            }
        }

        //BOOL __stdcall SRegLoadData(char *keyname, char *valuename, HKEY phkResult, LPBYTE lpData, int a5, LPDWORD lpcbData)
        [DllImport("storm.dll", EntryPoint = "#421")]
        private static extern Boolean SRegLoadData(String keyname, String valuename, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6);
        private delegate Boolean SRegLoadDataDelegate(String keyname, String valuename, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6);
        private static LocalHook SRegLoadDataLocalHook;
        private static Boolean SRegLoadDataHook(String keyname, String valuename, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6)
        {
            var result = SRegLoadData(keyname, valuename, a3, a4, a5, a6);

            Trace.WriteLine(String.Format("SRegLoadData({0}, {1}, ...) = {2}", keyname, valuename, result));

            return result;
        }

        //BOOL __stdcall SRegLoadString(char *keyname, char *valuename, int a3, char *buffer, size_t buffersize)
        [DllImport("storm.dll", EntryPoint = "#422")]
        private static extern Boolean SRegLoadString(String keyname, String valuename, IntPtr a3, IntPtr a4, IntPtr a5);
        private delegate Boolean SRegLoadStringDelegate(String keyname, String valuename, IntPtr a3, IntPtr a4, IntPtr a5);
        private static LocalHook SRegLoadStringLocalHook;
        private static Boolean SRegLoadStringHook(String keyname, String valuename, IntPtr a3, IntPtr a4, IntPtr a5)
        {
            var result = SRegLoadString(keyname, valuename, a3, a4, a5);

            Trace.WriteLine(String.Format("SRegLoadValue({0}, {1}, ...) = {2}", keyname, valuename, result));

            return result;
        }

        //BOOL __stdcall SRegLoadValue(char *keyname, char *valuename, int a3, int value)
        [DllImport("storm.dll", EntryPoint = "#423")]
        private static extern Boolean SRegLoadValue(String keyname, String valuename, IntPtr a3, IntPtr a4);
        private delegate Boolean SRegLoadValueDelegate(String keyname, String valuename, IntPtr a3, IntPtr a4);
        private static LocalHook SRegLoadValueLocalHook;
        private static Boolean SRegLoadValueHook(String keyname, String valuename, IntPtr a3, IntPtr a4)
        {
            var result = SRegLoadValue(keyname, valuename, a3, a4);

            Trace.WriteLine(String.Format("SRegLoadValue({0}, {1}, ...) = {2}", keyname, valuename, result));

            return result;
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A cheat for __fastcall when there is only one argument.
        private delegate IntPtr TriggerToPtrDelegate(JassTrigger trigger);
        private static TriggerToPtrDelegate TriggerToPtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr CodeToPtrDelegate(IntPtr triggerPtr, JassCode code);
        private static CodeToPtrDelegate CodeToPtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr DataConstructorDelegate(IntPtr a1);
        private static DataConstructorDelegate DataConstructor;

        //int __thiscall sub_6F39F5B0(int this, int a2)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr sub_6F39F5B0Delegate(IntPtr _this, String function_name);
        private static sub_6F39F5B0Delegate sub_6F39F5B0;

        //int __fastcall sub_6F3BDCB0(int unit_handle)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A cheat for __fastcall when there is only one argument.
        private delegate IntPtr sub_6F3BDCB0Delegate(JassUnit unit);
        private static sub_6F3BDCB0Delegate sub_6F3BDCB0;

        //int __thiscall sub_6F2860B0(int this, int a2, int a3, int a4, int a5, int a6, int a7)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr sub_6F2860B0Delegate(IntPtr _this, JassOrder a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, IntPtr a7);
        private static sub_6F2860B0Delegate sub_6F2860B0;
        private static LocalHook sub_6F2860B0LocalHook;

        private static IntPtr sub_6F2860B0Hook(IntPtr _this, JassOrder a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, IntPtr a7)
        {
            var result = sub_6F2860B0(_this, a2, a3, a4, a5, a6, a7);

            Trace.WriteLine(String.Format("sub_6F2860B0(...) = {0}", "0x" + result.ToString("X8")));
            Trace.WriteLine(" - a2: " + a2.ToString()); // order
            Trace.WriteLine(" - a3: 0x" + a3.ToString("X8")); // only observed as zero
            Trace.WriteLine(" - a4: 0x" + a4.ToString("X8")); // this contains a pointer to a unit if it's caused by a trigger, if not, it's just blank.
            Trace.WriteLine(" - a5: " + WarcraftIII.Memory.Read<Single>(a5)); // float* x
            Trace.WriteLine(" - a6: " + WarcraftIII.Memory.Read<Single>(a6)); // float* y
            Trace.WriteLine(" - a7: 0x" + a7.ToString("X8")); // only observed as zero
            //MessageBox.Show("Hackit!");
            return result;
        }

        //void *__thiscall sub_6F3A1D00(void *this, int a2)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr sub_6F3A1D00Delegate(IntPtr _this, IntPtr a2);
        private static sub_6F3A1D00Delegate sub_6F3A1D00;
        private static LocalHook sub_6F3A1D00LocalHook;

        private static IntPtr sub_6F3A1D00Hook(IntPtr _this, IntPtr a2)
        {
            var result = sub_6F3A1D00(_this, a2);

            //Trace.WriteLine(String.Format("sub_6F3A1D00({0}, {1}) = {2}", "0x" + Marshal.ReadIntPtr(_this).ToString("X8"), a2, "0x" + result.ToString("X8")));

            return result;
        }

        //void *__thiscall sub_6F4C1AB0(int this, int a2, int a3, signed int a4)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr sub_6F4C1AB0Delegate(IntPtr _this, IntPtr a2, String a3, IntPtr a4);
        private static sub_6F4C1AB0Delegate sub_6F4C1AB0;
        private static LocalHook sub_6F4C1AB0LocalHook;

        private static List<String> ignore = new List<String>()
        {
            // Interesting stuff
            ".?AUNativeFunc@@", // appears to be called once for each native added
            "Jass2/Nodes.h", 

            // likely jass related
            ".?AVCTriggerRegion@@",

            // The following seems related to game UI
            "HTEXTBLOCK",
            "HMODEL",
            "HMODELDATA",
            "HMATERIAL",
            "HMATERIALDATA",
            "HGEOSET",
            "HGEOSETDATA",
            "HDBFIELD",
            "HDBENTRY",
            "HSPRITEUBER",
            "HANIM",
            "HANIMDATA",
            "HSOUND",

            //
            ".?AUBATCHEDRENDERFONTDESC@@",
            ".$$BY0A@V?$TSLiteList@UObserverEventReg@@@@",
            ".?AULAYERNODE@CLayer@@",
            ".?AUSECTION@ProfileInternal@@",
            ".?AUPrefetchNode@@",
            ".?AUSTATUSENTRY@CStatus@@",
            ".?AUSTRINGHASHNODE@@",
            ".?AUFRAMETOKENHANDLERDESC@@",
            ".?AVCFramePointRelative@@",
            ".?AUFRAMENODE@CLayoutFrame@@",
            ".?AVCStringRep@@",
            ".?AUUncachableNode@@",
            ".?AUKEYVALUE@ProfileInternal@@",
            ".?AUCHILDNODE@@",
            ".?AUANIMSOUND@@",
            ".?AUAGILE_TYPE_DATA@@",
            ".?AUAGILE_TYPE_DESCENDANTS@AGILE_TYPE_DATA@@",
            ".?AVCRlAgentDef@NIpse@@",
            ".?AUMASTERSOUNDENTRY@@",   // sound loading?
            ".?AVObserverRegistry@@",
            ".?AUObserverEventReg@@",
            ".?AVCPlaneParticleEmitter@@",
            ".?AUFRAMEREGHASH@@",
            ".?AUKERNNODE@@",
            ".?AUHashedString@@",
            ".?AUSymbol@@",
            ".?AVCFramePointAbsolute@@",
            ".?AUSIMPLEFRAMENODE@@",
            ".?AUREGIONNODE@@",
            ".?AUAGILE_TYPE_RELATIONSHIP@AGILE_TYPE_DATA@@",
            ".?AVCPoPoVelocityMod@NIpse@@",
            ".?AV?$CSiRequest@VCPrRelation@NIpse@@",
        };

        private static IntPtr LastOrderPointPtr;
        private static IntPtr LastOrderTargetPtr;
        private static IntPtr sub_6F4C1AB0Hook(IntPtr _this, IntPtr a2, String a3, IntPtr a4)
        {
            if (a3 == ".?AVCOrderTarget@@")
            {
                //MessageBox.Show("orderPoint: prepare");
            }
            var result = sub_6F4C1AB0(_this, a2, a3, a4);

            //if (!ignore.Contains(a3))
            //    Trace.WriteLine(String.Format(DateTime.Now.ToLongTimeString() + ": sub_6F4C1AB0(..., {2}, ...) = {4}", "_this", a2, a3, a4, "0x" + result.ToString("X8")));

            if (a3 == ".?AVCOrderPoint@@")
            {
                LastOrderPointPtr = result;
                var orderPoint = (OrderPoint)Marshal.PtrToStructure(LastOrderPointPtr, typeof(OrderPoint));
                Trace.WriteLine(String.Format("A unit was issued a '{0}' order to ({1}; {2}).", orderPoint.Order, orderPoint.X, orderPoint.Y));
            }

            if (a3 == ".?AVCOrderTarget@@")
            {
                LastOrderTargetPtr = result;
                var orderPoint = (OrderPoint)Marshal.PtrToStructure(LastOrderTargetPtr, typeof(OrderPoint));
                Trace.WriteLine(String.Format("A unit was issued a '{0}' order to ({1}; {2}).", orderPoint.Order, orderPoint.X, orderPoint.Y));
                //MessageBox.Show("orderPoint: 0x" + result.ToString("X8"));
            }

            if (a3 == ".?AVCPlayerUnitPointOrderEventData@@")
            {
                var orderPoint = (OrderPoint)Marshal.PtrToStructure(LastOrderPointPtr, typeof(OrderPoint));

                //MessageBox.Show("PlayerUnitPointOrder detected!" + Environment.NewLine +
                //    "OrderPoint: 0x" + LastOrderPointPtr.ToString("X8") + "(" + orderPoint.X + "; " + orderPoint.Y + ") " + orderPoint.Order + Environment.NewLine +
                //    "OrderTarget: 0x" + LastOrderTargetPtr.ToString("X8"));
            }

            return result;
        }

        private static JassTriggerAction TriggerAddActionHook(JassTrigger trigger, JassCode code)
        {
            //var triggerPtr = TriggerToPtr(trigger);
            //Console.WriteLine("TriggerPtr: 0x" + triggerPtr.ToString("X8"));
            //var codePtr = CodeToPtr(triggerPtr, code);
            //Console.WriteLine("CodePtr: 0x" + codePtr.ToString("X8"));
            //Console.WriteLine("_TriggerAddActionHook({0}, {1}", "0x" + TriggerToPtr(trigger).ToString("X8"), "0x" + CodeToPtr(code).ToString("X8"));
            //Console.WriteLine("config: " + sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "config").ToString("X8"));
            //Console.WriteLine("main: " + sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "main").ToString("X8"));
            //Console.WriteLine("Trig_mapinit_Actions: " + sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "Trig_mapinit_Actions").ToString("X8"));
            //Console.WriteLine("InitCustomTeams: " + sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "InitCustomTeams").ToString("X8"));
            //Console.WriteLine("InitCustomTeams: " + sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "InitCustomTeams").ToString("X8"));
            //Console.WriteLine("InitCustomTeams: " + sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "InitCustomTeams").ToString("X8"));
            //var esc = sub_6F39F5B0(Marshal.ReadIntPtr((IntPtr)(0x6FAB65F4)), "Trig_esc_Actions");

            return _TriggerAddAction(trigger, code);
        }

        public static void Initialize()
        {
            Console.WriteLine("Fetching TriggerToPtr . . . ");
            TriggerToPtr = (TriggerToPtrDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x3BDEF0), typeof(TriggerToPtrDelegate));
            Console.WriteLine("Fetching CodeToPtr . . . ");
            CodeToPtr = (CodeToPtrDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x00447C30), typeof(CodeToPtrDelegate));
            Console.WriteLine("Fetching DataConstructor . . . ");
            DataConstructor = (DataConstructorDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x000074F0), typeof(DataConstructorDelegate));
            Console.WriteLine("Fetching sub_6F39F5B0 . . . ");
            sub_6F39F5B0 = (sub_6F39F5B0Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x0039F5B0), typeof(sub_6F39F5B0Delegate));
            Console.WriteLine("Fetching sub_6F3A1D00 . . . ");
            sub_6F3A1D00 = (sub_6F3A1D00Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x003A1D00), typeof(sub_6F3A1D00Delegate));
            Console.WriteLine("Fetching sub_6F3BDCB0 . . . ");
            sub_6F3BDCB0 = (sub_6F3BDCB0Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x003BDCB0), typeof(sub_6F3BDCB0Delegate));
            Console.WriteLine("Fetching sub_6F4C1AB0 . . . ");
            sub_6F4C1AB0 = (sub_6F4C1AB0Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x004C1AB0), typeof(sub_6F4C1AB0Delegate));
            Console.WriteLine("Fetching sub_6F2860B0 . . . ");
            sub_6F2860B0 = (sub_6F2860B0Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)(WarcraftIII.Module + 0x002860B0), typeof(sub_6F2860B0Delegate));

            Console.Write("Fetching Cheat . . . ");
            _Cheat = WarcraftIII.Jass.GetNative("Cheat").ToDelegate<CheatDelegate>();
            Console.WriteLine("Done!");

            Console.Write("Fetching SetUnitAnimation . . . ");
            _SetUnitAnimation = WarcraftIII.Jass.GetNative("SetUnitAnimation").ToDelegate<SetUnitAnimationDelegate>();
            Console.WriteLine("Done!");

            Console.Write("Fetching CreateTrigger . . . ");
            _CreateTrigger = WarcraftIII.Jass.GetNative("CreateTrigger").ToDelegate<CreateTriggerDelegate>();
            Console.WriteLine("Done!");

            Console.Write("Fetching TriggerExecute . . . ");
            _TriggerExecute = WarcraftIII.Jass.GetNative("TriggerExecute").ToDelegate<TriggerExecuteDelegate>();
            Console.WriteLine("Done!");

            Console.Write("Fetching TriggerAddAction . . . ");
            _TriggerAddAction = WarcraftIII.Jass.GetNative("TriggerAddAction").ToDelegate<TriggerAddActionDelegate>();
            Console.WriteLine("Done!");

            Console.Write("Replacing Cheat . . . ");
            WarcraftIII.Jass.AddNative(new CheatDelegate(CheatHook), "Cheat");
            Console.WriteLine("Done!");

            Console.Write("Replacing TriggerAddAction . . . ");
            WarcraftIII.Jass.AddNative(new TriggerAddActionDelegate(TriggerAddActionHook), "TriggerAddAction");
            Console.WriteLine("Done!");

            Console.Write("Replacing SetUnitAnimation . . . ");
            WarcraftIII.Jass.AddNative(new SetUnitAnimationDelegate(SetUnitAnimationHook), "SetUnitAnimation");
            Console.WriteLine("Done!");

            Console.Write("Installing sub_6F3A1D00 hook . . . ");
            sub_6F3A1D00LocalHook = LocalHook.Create((IntPtr)(WarcraftIII.Module + 0x003A1D00), new sub_6F3A1D00Delegate(sub_6F3A1D00Hook), null);
            sub_6F3A1D00LocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Console.WriteLine("Done!");

            Console.Write("Installing sub_6F4C1AB0 hook . . . ");
            sub_6F4C1AB0LocalHook = LocalHook.Create((IntPtr)(WarcraftIII.Module + 0x004C1AB0), new sub_6F4C1AB0Delegate(sub_6F4C1AB0Hook), null);
            sub_6F4C1AB0LocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Console.WriteLine("Done!");

            Console.Write("Installing sub_6F2860B0 hook . . . ");
            sub_6F2860B0LocalHook = LocalHook.Create((IntPtr)(WarcraftIII.Module + 0x002860B0), new sub_6F2860B0Delegate(sub_6F2860B0Hook), null);
            sub_6F2860B0LocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Console.WriteLine("Done!");

            Console.Write("Installing SRegLoadData hook . . . ");
            SRegLoadDataLocalHook = LocalHook.Create(Kernel32.GetProcAddress(Kernel32.GetModuleHandle("storm.dll"), 421), new SRegLoadDataDelegate(SRegLoadDataHook), null);
            SRegLoadDataLocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Console.WriteLine("Done!");

            Console.Write("Installing SRegLoadString hook . . . ");
            SRegLoadStringLocalHook = LocalHook.Create(Kernel32.GetProcAddress(Kernel32.GetModuleHandle("storm.dll"), 422), new SRegLoadStringDelegate(SRegLoadStringHook), null);
            SRegLoadStringLocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Console.WriteLine("Done!");

            Console.Write("Installing SRegLoadValue hook . . . ");
            SRegLoadValueLocalHook = LocalHook.Create(Kernel32.GetProcAddress(Kernel32.GetModuleHandle("storm.dll"), 423), new SRegLoadValueDelegate(SRegLoadValueHook), null);
            SRegLoadValueLocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Console.WriteLine("Done!");
        }
    }
}
