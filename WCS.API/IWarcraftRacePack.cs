using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS.API
{
    public interface IWarcraftRacePack
    {
        public string name { get; }
        public string author { get; }
        public string version { get; }

        public string Uuid => new Guid().ToString();
    }
}
