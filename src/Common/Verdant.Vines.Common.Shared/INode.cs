using System;
using System.Collections;
using System.Text;

namespace Verdant.Vines.Common
{
    public delegate void NotifyPropertyChangedEventHandler(object sender, object foo);

    interface INode : IDisposable
    {
        ServiceDescription ServiceDescription { get; }

        void ExecuteMethod(string method, Hashtable inputs, Hashtable outputs);

        event NotifyPropertyChangedEventHandler PropertyChanged;        
    }
}
