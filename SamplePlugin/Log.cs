using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSVfinder
{
   
    public class Log : IDisposable
    {
        private StreamWriter? _logger = null;
        private DirectoryInfo _logDir;

        public Log(DirectoryInfo logDir)
        {
            _logDir = logDir;
            if (_logger == null)
            {
                try
                {
                    _logDir.Create();
                    _logger = new StreamWriter($"{_logDir.FullName}/World_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.log");
                }
                catch (IOException e)
                {

                }
            }
        }
        public void WriteLog(string type)
        {
            if (_logger != null)
                _logger.WriteLine($"{DateTime.Now:O}|{type}");
        }
        public void Dispose()
        {
            if (_logger != null)
            {
                _logger.Dispose();
                _logger = null;
            }
        }
    }
}
