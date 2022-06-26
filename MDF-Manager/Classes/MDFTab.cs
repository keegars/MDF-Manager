using System.Windows.Controls;

namespace MDF_Manager.Classes
{
    public class MDFTab : TabItem
    {
        public MDFFile mdf;
        public MDFTab(MDFFile mdfFile)
        {
            mdf = mdfFile;
            Header = mdf.Header;
        }
    }
}