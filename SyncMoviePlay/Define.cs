using System;
using System.Text;
using System.Windows.Forms;

namespace ns_Define
{
    class Define
    {
        public string FileFolderPath;

        /// <summary>
        /// コンストラクター
        /// <para>フォルダパスの指定</para>
        /// </summary>
        /// <param name="PlusPath">
        /// 追加するフォルダー(\を除く)
        /// </param>
        /// <param name="OutFolder">
        /// true…exeファイルが入っているフォルダーよりも一階層上
        /// <para>false…exeファイルと同層</para>
        /// </param>
        public Define(string PlusPath , bool OutFolder)
        {
            //++++++++++++++++++++++++++++
            //画像のフォルダパスを設定する
            //++++++++++++++++++++++++++++
            string temp_path = "";
            int i = 0;
            //①　exeファイルまでのパス　※exeファイル名まで含む
            temp_path = Application.ExecutablePath;

            //②　exeファイル名を除去
            i = temp_path.LastIndexOf("/");
            if (i <= 0)
            {
                i = temp_path.LastIndexOf("//");
            }
            if (i <= 0)
            {
                i = temp_path.LastIndexOf("\\");
            }

            FileFolderPath = temp_path.Substring(0, i);

            //③　exeファイルの更に上の階層を除去
            if (OutFolder)
            {
                i = FileFolderPath.LastIndexOf("/");
                if (i <= 0)
                {
                    i = FileFolderPath.LastIndexOf("//");
                }
                if (i <= 0)
                {
                    i = FileFolderPath.LastIndexOf("\\");
                }
            }
            FileFolderPath = FileFolderPath.Substring(0, i);

            //④　exeファイルの入っているフォルダ＋指定フォルダ
            FileFolderPath += "/" + PlusPath + "/";
        }        
    }
}
