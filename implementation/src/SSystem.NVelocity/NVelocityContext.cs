using System;
using System.Collections.Generic;
using System.Text;

namespace SSystem.NVelocity
{
    /// <summary>
    /// NVelocity内容类，所有内容类均要继承此类
    /// </summary>
    public class NVelocityContext
    {
        private string _id;
        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }
    }
}
