using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SSystem.NVelocity
{
    public class NVelocityCreate
    {
        public delegate void DAddNews(Velocities vs,NVelocityContext context);

        public event DAddNews OnAddNews;

        public delegate void DAddNewsList(ref NVelocityContext context, NVelocityContext right,string fileName);
        public event DAddNewsList OnAddNewsList;
        private static string Md5(string msg)
        {
            byte[] source = Encoding.UTF8.GetBytes(msg.ToCharArray());
            System.Security.Cryptography.MD5 md = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bye = md.ComputeHash(source);
            string res = "";

            for (int i = 0; i < bye.Length; i++)
            {
                string temp = "";
                temp = String.Format("{0:x}", bye[i]);
                while (temp.Length < 2)
                {
                    temp = temp.Insert(0, "0");
                }
                res += temp;
            }
            return res;
        }

        #region 公开属性

        public string OutputDir;
        public string ModelDir;
        public int PageSize = 2;
        public string ListModelName = "";
        public string DetailModelName = "";
        public string ListTag = "";
        #endregion

        public void CreateStaticPages(NVelocityContext[] contexts, string indexListName,bool deletePrevFiles)
        {
            if (ListModelName == "") {
                throw new Exception("需要给listModelName赋值，标明列表页面所用的模板");
            }

            string[] names = new string[2];
            if (indexListName.IndexOf('.') == -1)
            {
                names[0] = indexListName;
                names[1] = ".htm";
            }
            else
            {
                names = indexListName.Split('.');
            }


            if (deletePrevFiles)
            {
                string[] files = Directory.GetFiles(OutputDir);
                foreach (string f in files)
                {
                    File.Delete(f);
                }
            }


            Velocities vlist = new Velocities(ModelDir);
            vlist.GetTemplate(ListModelName);

            List<NVelocityContext> buf = new List<NVelocityContext>();

            int row = 0;

            int count = contexts.Length;
            int allPage = count / PageSize;
            if (count % PageSize != 0)
                allPage++;

            foreach (NVelocityContext c in contexts)
            {
                row++;
                string id = c.ID;

                Velocities vs = new Velocities(ModelDir);
                vs.GetTemplate(DetailModelName);

                OnAddNews(vs, c);
                if (string.IsNullOrEmpty(id))
                {
                    id = new Random().Next().ToString();
                    c.ID = id;
                }
                string filename = Md5(id) + ".htm";
                NVelocityContext sn = new NVelocityContext();

                OnAddNewsList(ref sn, c,filename);
                

                buf.Add(sn);

                if (row % PageSize == 0)
                {
                    vlist.Put("myTable", buf);
                    vlist.Put("title", "生成所有新闻");
                    if (row == PageSize)
                    {
                        vlist.Put("first", false);
                        vlist.Put("prev", false);
                        if (allPage > 1)
                        {
                            vlist.Put("next", names[0] + "2." + names[1]);
                            vlist.Put("last", names[0] + allPage + "." + names[1]);
                        }
                        else
                        {
                            vlist.Put("next", false);
                            vlist.Put("last", false);
                        }
                        vlist.Save(OutputDir + names[0] + "." + names[1]);

                    }
                    else
                    {
                        int page = row / PageSize;
                        string sprev = names[0] + "." + names[1];

                        if (page > 1)
                        {
                            int tpage = page - 1;
                            if (tpage > 1)
                                sprev = names[0] + tpage + "." + names[1];
                        }
                        string pm = names[0] + page + "." + names[1];

                        vlist.Put("first", names[0] + "." + names[1]);
                        vlist.Put("prev", sprev);

                        int ttpage = page + 1;
                        if (ttpage <= allPage)
                        {
                            vlist.Put("next", names[0] + ttpage + "." + names[1]);
                        }
                        else
                        {
                            vlist.Put("next", false);
                            vlist.Put("last", false);
                        }



                        vlist.Save(OutputDir + pm);

                    }
                    buf.Clear();
                }

                string parentPath = names[0] + "." + names[1];
                int irow = row / (PageSize+1) + 1;
                if (irow > 1)
                {
                    parentPath = names[0] + irow + "." + names[1];
                }

                vs.Put("href", parentPath);


                string sfilename = OutputDir + filename;

                vs.Save(sfilename);
            }

            if (buf.Count > 0&&!string.IsNullOrEmpty(ListTag))
            {
                
                vlist.Put(ListTag, buf);

                vlist.Save(OutputDir + indexListName);
            }
        }
    }
}
