using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTransfer.Common
{
    public class MissionItem:BindableBase
    {
        public MissionItem(string name,double max)
        {
            this.Name = name;
            this.Max = max;
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(Max));
        }
        public string Name { get;private set; }
        public double Max { get; private set; }
        private double _value;
        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }
    }
}
