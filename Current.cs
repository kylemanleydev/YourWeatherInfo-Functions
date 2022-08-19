using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourWeatherInfo_Functions
{
    public class Current
    {
        public double Temp_f { get; set; }
        public Condition Condition { get; set; }
        public double Wind_mph { get; set; }
        public string Wind_dir { get; set; }
        public double Humidity { get; set; }
        public double Cloud { get; set; }
        public double Uv { get; set; }
    }
}
