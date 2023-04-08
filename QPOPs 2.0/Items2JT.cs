using JTfy;

namespace QPOPSDesktop
{
    public class Items2JT
    {
        public static async Task<byte[]> Convert(List<dynamic[]> jtNodesJSByKey)
        {
            return await Task.Run(() =>
            {
                var roots = new List<JTNode>();

                var dataLookup = jtNodesJSByKey.ToDictionary(keyValue => (int)keyValue[0], keyValue =>
                {
                    var jtNodeJS = keyValue[1];

                    var jtNode = new JTNode()
                    {
                        Name = (string)jtNodeJS.name,
                        TransformationMatrix = jtNodeJS.transformationMatrix == null ? Array.Empty<float>() : ((List<object>)jtNodeJS.transformationMatrix).Select(v => System.Convert.ToSingle(v)).ToArray(),
                        ReferencedFile = ((string)jtNodeJS.referencedFile) ?? "",
                        ReferencedFileIsPart = jtNodeJS.referencedFileIsPart == null ? false : (bool)jtNodeJS.referencedFileIsPart,
                        Attributes = jtNodeJS.attributes == null ? new() : ((List<object>)jtNodeJS.attributes).ToDictionary(attribute => (string)((List<object>)attribute)[0], attribute => ((List<object>)attribute)[1])
                    };

                    if ((bool)jtNodeJS.isRoot) roots.Add(jtNode);

                    return new
                    {
                        JTNode = jtNode,
                        ChildredIDs = ((List<object>)jtNodeJS.childrenIDs).Cast<int>().ToArray()
                    };
                });

                foreach (var dataItem in dataLookup.Values)
                {
                    dataItem.JTNode.Children = dataItem.ChildredIDs?.Select(id => dataLookup[id].JTNode).ToList() ?? new();
                }

                var jtFile = roots[0].ToBytes();

                return jtFile;
            });
        }
    }
}
