using System;
using System.Collections.Generic;
using System.Text;
using NVelocity.App;
using NVelocity;
using Commons.Collections;
using NVelocity.Runtime;
using NVelocity.Context;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace SSystem.NVelocity
{
    /// <summary>
    /// Velocity的封装类
    /// </summary>
    public class Velocities:IDisposable
    {
        private VelocityEngine _velocity = new VelocityEngine();
        private Template _template = null;
        private IContext _context = new VelocityContext();
        private string _templateStr = null;

        public List<string> AttachMessageList = new List<string>();
        private bool _onlyOnce = true;

        private string _outputDir = "";


        /// <summary>
        /// 获取NVelocity的操作引擎对象
        /// </summary>
        /// <returns></returns>
        public VelocityEngine GetVelocityEngine()
        {

            return _velocity;
        }

        /// <summary>
        /// 获取关键字
        /// </summary>
        public object[] Keys
        {
            get
            {
                return _context.Keys;
            }
        }

        /// <summary>
        /// 获取或设置模板字符串数据
        /// </summary>
        public string TemplateStr
        {
            get { return _templateStr; }
            set { _templateStr = value; }
        }

        /// <summary>
        /// 获取或设置输出生成页面的目录
        /// </summary>
        public string OutputDirectory
        {
            get { return _outputDir; }
            set
            {
                if (value[value.Length - 1] == '\\')
                    _outputDir = value;
                else
                    _outputDir = value + "\\";

            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modelDirectory">模板所在的目录</param>
        public Velocities(string modelDirectory)
        {
            ExtendedProperties props = new ExtendedProperties();
            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "file");
            props.AddProperty(RuntimeConstants.FILE_RESOURCE_LOADER_PATH, modelDirectory);

            _velocity.Init(props);

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modelDirectory">模板所在的目录</param>
        /// <param name="outputEncoding">输入/输出所使用的编码</param>
        public Velocities(string modelDirectory, string encoding)
            : this(modelDirectory)
        {
            _velocity.AddProperty(RuntimeConstants.OUTPUT_ENCODING, encoding);
            _velocity.AddProperty(RuntimeConstants.INPUT_ENCODING, encoding);
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modelDirectory">模板所在的目录</param>
        /// <param name="inputEncoding">输入所使用的编码，如果为空则不设置</param>
        /// <param name="outputEncoding">输出所使用的编码，如果为空则不设置</param>
        public Velocities(string modelDirectory, string inputEncoding, string outputEncoding)
            : this(modelDirectory)
        {
            if (!string.IsNullOrEmpty(inputEncoding))
            {
                _velocity.AddProperty(RuntimeConstants.OUTPUT_ENCODING, inputEncoding);
            }

            if (!string.IsNullOrEmpty(outputEncoding))
            {
                _velocity.AddProperty(RuntimeConstants.INPUT_ENCODING, outputEncoding);
            }
        }

        public Velocities()
        {
            ExtendedProperties props = new ExtendedProperties();
            _velocity.Init(props);
        }

        /// <summary>
        /// 获取一个模板页面
        /// </summary>
        /// <param name="name"></param>
        public void GetTemplate(string name)
        {

            _template = _velocity.GetTemplate(name);

        }

        /// <summary>
        /// 为模板添加参数，调用此方法前必须先调用GetTemplate
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Put(string name, object value)
        {
            _context.Put(name, value);

        }

        /// <summary>
        /// 保存到一个文件中。使用UTF-8编码
        /// </summary>
        /// <param name="fileName">要保存的目标文件</param>
        public void Save(string fileName)
        {
            Save(fileName, Encoding.UTF8);
        }

        /// <summary>
        /// 保存到一个文件中
        /// </summary>
        /// <param name="fileName">要保存的目标文件</param>
        public void SaveDefaultEncoding(string fileName)
        {
            StringWriter writer = new StringWriter();
            _template.Merge(_context, writer);
            if (!string.IsNullOrEmpty(OutputDirectory))
                fileName = OutputDirectory + fileName;
            using (StreamWriter w = new StreamWriter(fileName, false))
            {
                w.Write(writer);
                w.Flush();
                w.Close();
            }
        }

        /// <summary>
        /// 清除所有项
        /// </summary>
        public void ClearItems()
        {
            _context = new VelocityContext();
        }

        public void ClearAndRestoreItems()
        {
            ClearItems();
        }

        /// <summary>
        /// 清除某一项
        /// </summary>
        /// <param name="key"></param>
        public void ClearItem(object key)
        {
            _context.Remove(key);
        }


        /// <summary>
        /// 保存到一个文件中。
        /// </summary>
        /// <param name="fileName">要保存的目标文件</param>
        /// <param name="encoder">保存的编码</param>
        public void Save(string fileName, Encoding encoder)
        {
            if (_template == null)
            {
                throw new Exception("can not find template");
            }

            if (!string.IsNullOrEmpty(OutputDirectory))
                fileName = OutputDirectory + fileName;

            StringWriter writer = new StringWriter();
            _template.Merge(_context, writer);

            using (StreamWriter w = new StreamWriter(fileName, false, encoder, 2000))
            {
                w.Write(writer);
                w.Flush();
                w.Close();
            }
        }
        /// <summary>
        /// 把字节流输出来
        /// </summary>
        /// <returns></returns>
        public StringWriter Output()
        {
            if (_template == null && _templateStr == null)
            {
                throw new Exception("请调用GetTemplate()接口指定一个模板或者为TemplateStr属性指定模板字符串资源");
            }
            StringWriter writer = new StringWriter();

            if (_template != null)
                _template.Merge(_context, writer);
            else
            {
                _velocity.Evaluate(_context, writer, null, _templateStr);
            }
            foreach (string item in AttachMessageList)
            {
                writer.WriteLine(item);
            }
            if (_onlyOnce)
                AttachMessageList.Clear();
            return writer;
        }

        public override string ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(Output());

            return sb.ToString();
        }


        /// <summary>
        /// 清除指定目录的所有文件，如果目录不存在，则创建目录
        /// </summary>
        /// <param name="dir">物理目录</param>
        public static void ClearFiles(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            else
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string f in files)
                {
                    File.Delete(f);
                }
            }
        }

        /// <summary>
        /// 获取某一项的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetProperty(string key)
        {
            return _velocity.GetProperty(key);
        }

        public void SetOtherTextIn(string message, bool onlyOnce)
        {
            if (!string.IsNullOrEmpty(message))
            {
                AttachMessageList.Add(message);
                _onlyOnce = onlyOnce;
            }
        }

        /// <summary>
        /// 判断是否存在变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsContainsKey(string key)
        {
            return _context.ContainsKey(key);
        }

        /// <summary>
        /// 从模板中自动初始化变量
        /// </summary>
        public void InitKeysFromTemplate(bool IsForce)
        {
            string text = this.ToString();
            MatchCollection mc = Regex.Matches(this.ToString(), @"\$(\w+)");
            foreach (Match m in mc)
            {
                if (!m.Success) continue;
                if (GetValue(m.Groups[1].Value) == null||IsForce)
                    this.Put(m.Groups[1].Value, "");
            }
        }

        /// <summary>
        /// 从模板中自动初始化变量
        /// </summary>
        public void InitKeysFromTemplate()
        {
            InitKeysFromTemplate(false);
        }
        public object GetValue(string key)
        {
            return _context.Get(key);
        }

        /// <summary>
        /// 保持住原来的值
        /// </summary>
        /// <param name="NameValues">原来各个变量的值的集合</param>
        public void KeepValues(NameValueCollection NameValues)
        {
            object v;
            foreach (object item in Keys)
            {
                v = NameValues[item.ToString()];

                if (v == null)
                {
                    v = GetValue(item.ToString());
                }
                Put(item.ToString(), v == null ? "" : v);
            }
        }

        public void Dispose()
        {
            ClearItems();
        }
    }
}
