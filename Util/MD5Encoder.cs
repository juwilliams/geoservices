using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using gbc.Interfaces;

namespace gbc.Util
{
    public class MD5Encoder
    {
        /// <summary>
        /// Appends an MD5 Hash ID to the input element
        /// </summary>
        /// <param name="x">XElement x (The string to append to)</param>
        /// <returns></returns>
        public static XElement AppendMD5HashId(XElement x)
        {
            //	encode the element to MD5
            string md5 = encode(MD5.Create(), x.ToString());
            x.Add(new XAttribute("id", md5));

            //	return the newly attributed XElement
            return x;
        }

        /// <summary>
        /// Converts an input XDocument into a MD5 Hash representation and returns
        /// this value as a string.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static string AppendMD5HashId(XDocument doc)
        {
            return encode(MD5.Create(), doc.ToString());
        }

        /// <summary>
        /// Returns a hash for the input string
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string GetMD5HashId(string x)
        {
            return encode(MD5.Create(), x);
        }

        public static string GetMD5HashId(IGeoRecord record)
        {
            return encode(record);
        }

        private static string encode(IGeoRecord record)
        {
            var sb = new StringBuilder();
            foreach (var geoField in record.fields)
            {
                sb.Append(geoField.Value);
            }
            return encode(MD5.Create(), sb.ToString());
        }

        /// <summary>
        /// Accepts an input string and converts it to an MD5 encoded string
        /// which is then returned to be added to the UID attribute.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static string encode(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
    }
}
