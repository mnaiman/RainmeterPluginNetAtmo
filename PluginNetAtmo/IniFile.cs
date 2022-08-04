using System.Runtime.InteropServices;
using System.Text;

namespace PluginNetAtmo
{
    public class IniFile
    {
        readonly string Path;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath) => Path = IniPath;

        public string Read(string Section, string Key)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Section, string Key, string Value) => WritePrivateProfileString(Section, Key, Value, Path);

        public void DeleteKey(string Section, string Key) => Write(Key, null, Section);

        public void DeleteSection(string Section) => Write(null, null, Section);

        public bool KeyExists(string Section, string Key) => Read(Key, Section).Length > 0;
    }
}
