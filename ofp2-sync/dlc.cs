using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using System.Net;
using System.IO;
namespace ofp2_sync
{
    class dlc
    {
        public string name;
        public double revision;
        //public string notes;
        public string filename;
        public string foundMirror;
        public int uid;
        public bool installed = false;
        //private List<string> mirror;
        ArrayList mirror = new ArrayList();
        public ArrayList remove = new ArrayList();
        public bool isThereAMirror = false;
        public string notes;

        public void mirrors(string current_mirror)
        {
            this.filename = Path.GetFileName(current_mirror);
            mirror.Add(current_mirror);
        }

        public void removePath(string current_path){
            remove.Add(current_path);
        }


        public ArrayList getMirrors()
        {
            return mirror;
        }
        public string first_mirror()
        {
            return mirror[0].ToString();
        }
        public bool findMirror()
        {
            foreach (string currentmirror in mirror)
            {
                if (checkUrlLink(currentmirror))
                {
                    this.foundMirror = currentmirror;
                    this.isThereAMirror = true;
                    return true;
                }
            }
            return false;
        }

        //check for valid mirrors
        public bool checkUrlLink(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = null;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    response.Close();
                    return true;
                }
                response.Close();
                return false;
            }
            catch
            {

                return false;
            }
        }
    }
}
