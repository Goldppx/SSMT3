using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public partial class ReversePage
    {

        private void Button_ClearAllList_Click(object sender, RoutedEventArgs e)
        {
            IndexBufferItemList.Clear();
            CategoryBufferItemList.Clear();
            ShapeKeyPositionBufferItemList.Clear();
        }





    }
}
