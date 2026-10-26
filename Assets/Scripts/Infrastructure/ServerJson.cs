using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LangTroTan.Infrastructure
{
    // Preserve complete server snapshots, including fields added by future systems.
    public static class ServerJson
    {
        public static object Parse(string text){return new Reader(text).Document();}
        public static string Stringify(object value){var b=new StringBuilder();Write(b,value,0);return b.ToString();}
        public static Dictionary<string,object> Map(object value){return value as Dictionary<string,object>??throw new FormatException("Phản hồi không phải đối tượng JSON.");}
        static void Write(StringBuilder b,object v,int depth)
        {
            if(depth>128)throw new FormatException("JSON quá sâu.");
            if(v==null){b.Append("null");return;}if(v is string s){Quote(b,s);return;}if(v is bool flag){b.Append(flag?"true":"false");return;}
            if(v is IDictionary<string,object> map){b.Append('{');bool first=true;foreach(var p in map){if(!first)b.Append(',');first=false;Quote(b,p.Key);b.Append(':');Write(b,p.Value,depth+1);}b.Append('}');return;}
            if(v is IList list){b.Append('[');for(int i=0;i<list.Count;i++){if(i>0)b.Append(',');Write(b,list[i],depth+1);}b.Append(']');return;}
            if(v is byte||v is short||v is int||v is long||v is uint||v is ulong||v is float||v is double||v is decimal)
            {string n=Convert.ToString(v,CultureInfo.InvariantCulture);if(n=="NaN"||n.Contains("Infinity"))throw new FormatException("JSON sai số.");b.Append(n);return;}
            throw new FormatException("Kiểu không hỗ trợ: "+v.GetType().Name);
        }
        static void Quote(StringBuilder b,string s)
        {b.Append('"');foreach(char c in s){switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}b.Append('"');}
        sealed class Reader
        {
            readonly string s;int p;
            public Reader(string s){this.s=s??throw new ArgumentNullException(nameof(s));if(s.Length>32*1024*1024)throw new FormatException("Phản hồi quá lớn.");}
            void Space(){while(p<s.Length&&(s[p]==' '||s[p]=='\n'||s[p]=='\r'||s[p]=='\t'))p++;}
            bool Match(char c){Space();if(p<s.Length&&s[p]==c){p++;return true;}return false;}
            void Need(char c){if(!Match(c))throw new FormatException("Thiếu "+c+" tại "+p);}
            public object Document(){var value=Value(0);Space();if(p!=s.Length)throw new FormatException("JSON có phần dư.");return value;}
            object Value(int depth)
            {
                if(depth>128)throw new FormatException("JSON quá sâu.");Space();if(p>=s.Length)throw new FormatException("JSON kết thúc sớm.");char c=s[p];
                if(c=='"')return Text();
                if(c=='{'){p++;var map=new Dictionary<string,object>();if(Match('}'))return map;do{Space();if(p>=s.Length||s[p]!='"')throw new FormatException("Thiếu tên trường.");string key=Text();Need(':');if(map.ContainsKey(key))throw new FormatException("Trùng tên trường.");map.Add(key,Value(depth+1));if(Match('}'))return map;Need(',');}while(true);}
                if(c=='['){p++;var list=new List<object>();if(Match(']'))return list;do{list.Add(Value(depth+1));if(Match(']'))return list;Need(',');}while(true);}
                foreach(var pair in new[]{new KeyValuePair<string,object>("true",true),new KeyValuePair<string,object>("false",false),new KeyValuePair<string,object>("null",null)})
                    if(p+pair.Key.Length<=s.Length&&string.CompareOrdinal(s,p,pair.Key,0,pair.Key.Length)==0){p+=pair.Key.Length;return pair.Value;}
                int start=p;if(c=='-')p++;if(p>=s.Length)throw new FormatException("Thiếu số.");
                if(s[p]=='0')p++;else{if(s[p]<'1'||s[p]>'9')throw new FormatException("Sai giá trị.");while(p<s.Length&&s[p]>='0'&&s[p]<='9')p++;}
                if(p<s.Length&&s[p]=='.'){p++;int begin=p;while(p<s.Length&&s[p]>='0'&&s[p]<='9')p++;if(p==begin)throw new FormatException("Thiếu phần thập phân.");}
                if(p<s.Length&&(s[p]=='e'||s[p]=='E')){p++;if(p<s.Length&&(s[p]=='+'||s[p]=='-'))p++;int begin=p;while(p<s.Length&&s[p]>='0'&&s[p]<='9')p++;if(p==begin)throw new FormatException("Thiếu số mũ.");}
                string token=s.Substring(start,p-start);if(long.TryParse(token,NumberStyles.Integer,CultureInfo.InvariantCulture,out long integer))return integer;
                if(decimal.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out decimal exact))return exact;
                if(double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out double number)&&!double.IsNaN(number)&&!double.IsInfinity(number))return number;
                throw new FormatException("Số không hợp lệ.");
            }
            string Text()
            {
                Need('"');var b=new StringBuilder();while(p<s.Length){char c=s[p++];if(c=='"')return b.ToString();if(c<32)throw new FormatException("Ký tự điều khiển trong chuỗi.");if(c!='\\'){b.Append(c);continue;}if(p>=s.Length)break;
                    switch(s[p++]){case '"':b.Append('"');break;case '\\':b.Append('\\');break;case '/':b.Append('/');break;case 'b':b.Append('\b');break;case 'f':b.Append('\f');break;case 'n':b.Append('\n');break;case 'r':b.Append('\r');break;case 't':b.Append('\t');break;case 'u':if(p+4>s.Length||!ushort.TryParse(s.Substring(p,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out ushort code))throw new FormatException("Sai Unicode.");b.Append((char)code);p+=4;break;default:throw new FormatException("Escape không hợp lệ.");}}
                throw new FormatException("Chuỗi chưa đóng.");
            }
        }
    }
}
