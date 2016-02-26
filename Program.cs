using System;
using engenious.Content.Serialization;
using engenious.Content;
using engenious.Graphics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using OpenTK;
using OpenTK.Graphics;

namespace ContentTool
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (frmMain mainForm = new frmMain())
                System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
