using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace pyRevitLabs.CommonWPF
{
    public class ConsoleProvider
    {
        private const string    KEREL32_DLLNAME = "kernel32.dll";
        private const int       ATTACH_PARENT_PROCESS = -1;

        [DllImport(KEREL32_DLLNAME)]
        private static extern bool AttachConsole(int processId);

        [DllImport(KEREL32_DLLNAME)]
        private static extern bool AllocConsole();

        [DllImport(KEREL32_DLLNAME)]
        private static extern bool FreeConsole();

        [DllImport(KEREL32_DLLNAME)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport(KEREL32_DLLNAME)]
        private static extern int GetConsoleOutputCP();

        public static bool HasConsole
        {
            get { return GetConsoleWindow() != IntPtr.Zero; }
        }

        /// <summary>
        /// Creates a new console instance if the process is not attached to a console already.
        /// </summary>
        public static void Show()
        {
            //#if DEBUG
            if (!HasConsole)
            {
                AllocConsole();
                InvalidateOutAndError();
            }
            //#endif
        }

        /// <summary>
        /// If the process has a console attached to it, it will be detached and no longer visible. Writing to the System.Console is still possible, but no output will be shown.
        /// </summary>
        public static void Hide()
        {
            //#if DEBUG
            if (HasConsole)
            {
                SetOutAndErrorNull();
                FreeConsole();
            }
            //#endif
        }

        /// <summary>
        /// Attach to existing console. This is helpful when running program from existing console.
        /// </summary>
        public static void Attach()
        {
            //#if DEBUG
            if (!HasConsole)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
            //#endif
        }

        /// <summary>
        /// Detach from existing console. This is helpful when running program from existing console.
        /// </summary>
        public static void Detach()
        {
            Hide();
        }

        public static void Toggle()
        {
            if (HasConsole)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        static void InvalidateOutAndError()
        {
            Type type = typeof(System.Console);

            System.Reflection.FieldInfo _out = type.GetField("_out",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.FieldInfo _error = type.GetField("_error",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.MethodInfo _InitializeStdOutError = type.GetMethod("InitializeStdOutError",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Debug.Assert(_out != null);
            Debug.Assert(_error != null);

            Debug.Assert(_InitializeStdOutError != null);

            _out.SetValue(null, null);
            _error.SetValue(null, null);

            _InitializeStdOutError.Invoke(null, new object[] { true });
        }

        static void SetOutAndErrorNull()
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }
    }
}
