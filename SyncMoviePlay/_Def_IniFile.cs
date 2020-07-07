using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ns_IniFile
{
    class SettingIni
    {
        public enum IniFileID
        {
            NONE,
            DISPLAY = 2,    //ディスプレイごとの動画
            OPTION = 3,     //画面の結合(1,2,3,4,横,縦) true or false
            TURMINAL,
        }


        private string _Data;
        public string Data { get { return _Data; } private set { _Data = value; } }

        private string filePath;

        //コンストラクター
        public SettingIni()
        {
            ns_Define.Define _def = new ns_Define.Define("sozai", true);
            filePath = _def.FileFolderPath + "Setting.ini";
        }

        /// <summary>
        /// Iniファイルから取得
        /// </summary>
        /// <param name="path">
        /// </param>
        public void Func_getIni(IniFileID path, out string[] GetData)
        {
            var obj = new SettingIni();
            int p = (int)path;
            obj.ParseFromIni(p.ToString("00"), filePath);
            GetData = obj.Data.Split(',');
        }

        /// <summary>
        /// Iniファイルへ反映 
        /// </summary>
        /// <param name="path">
        /// </param>
        /// <param name="s">反映する値</param>
        public void Func_SetIni(IniFileID path, string s)
        {
            var obj = new SettingIni();
            int p = (int)path;
            obj.Data = s;
            obj.ExportToIni(p.ToString("00"), filePath);
        }

    }

    static class IniFileHelper
    {
        [DllImport("KERNEL32.DLL")]
        public static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        [DllImport("KERNEL32.DLL")]
        public static extern uint GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);

        [DllImport("KERNEL32.DLL")]
        public static extern uint WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        public static void ParseFromIni<T>(this T self, string section, string filepath)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.PropertyType == typeof(int))
                {
                    prop.SetValue(self, (int)GetPrivateProfileInt(section, prop.Name, 0, Path.GetFullPath(filepath)));
                }
                else if (prop.PropertyType == typeof(uint))
                {
                    prop.SetValue(self, GetPrivateProfileInt(section, prop.Name, 0, Path.GetFullPath(filepath)));
                }
                else
                {
                    var sb = new StringBuilder(1024);
                    GetPrivateProfileString(section, prop.Name, string.Empty, sb, (uint)sb.Capacity, Path.GetFullPath(filepath));
                    prop.SetValue(self, sb.ToString());
                }
            }
        }

        public static void ExportToIni<T>(this T self, string section, string filepath)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                WritePrivateProfileString(section, prop.Name, prop.GetValue(self).ToString(), Path.GetFullPath(filepath));
            }
        }
    }
}