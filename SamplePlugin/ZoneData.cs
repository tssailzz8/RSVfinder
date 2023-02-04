using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RSVfinder
{

    public class ZoneData
    {
        public List<string> RSVs = new();
        public List<string> RSFs = new();

        public void Add(Plugin.RSFData rsfData)
        {

        }

        public void Add(Plugin.RSV_v62 rsvData)
        {

        }
    }
}
