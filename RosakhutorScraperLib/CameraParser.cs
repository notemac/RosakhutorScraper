using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RosakhutorScraperLib
{
    internal class CameraParser
    {
        #region Fields
        private readonly Regex _regexId;
        private readonly Regex _regexName;
        private readonly Regex _regexWidget;
        #endregion

        #region Methods
        internal CameraParser()
        {
            _regexId = new Regex(@"data-camera-id=""(?<id>[0-9]+)""", RegexOptions.Compiled); // data-camera-id="111"
            _regexName = new Regex(@"<h3 class=""webcams_name"">(?<name>.+?)<\/h3>", RegexOptions.Compiled); // <h3 class=""webcams_name"">XXX</h3>
            _regexWidget = new Regex(@"src=""\/\/sochi\.camera\/widget\/widget\.js\?(?<widget>.+?)"">", RegexOptions.Compiled); // src="//sochi.camera/widget/widget.js?s68">
        }
        internal List<string> GetNames(string html) => GetItems(html, _regexName, "name");
        internal List<string> GetIds(string html) => GetItems(html, _regexId, "id");
        internal string GetWidget(string html) => GetItem(html, _regexWidget, "widget");
        private List<string> GetItems(string html, Regex regex, string regexGroupName)
        {
            MatchCollection matchCollection = regex.Matches(html);
            var items = new List<string>(matchCollection.Count);
            foreach (Match match in matchCollection)
            {
                items.Add(match.Groups[regexGroupName].Value);
            }
            return items;
        }
        private string GetItem(string html, Regex regex, string regexGroupName)
            => regex.Match(html).Groups[regexGroupName].Value;
        #endregion
    }
}
