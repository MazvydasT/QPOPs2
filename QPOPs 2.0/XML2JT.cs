using JTfy;
using System.Collections.Concurrent;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace QPOPs2
{
    public static partial class XML2JT
    {
        private enum ContentType
        {
            Product,
            Resource
        }

        private class Item
        {
            public required string ExternalId { get; set; }
            public required ContentType Type { get; set; }
            public required string Title { get; set; }
            public Item[] Children { get; set; } = Array.Empty<Item>();
            public string FilePath { get; set; } = string.Empty;
            public required bool FileIsPart { get; set; }
            public ConcurrentBag<Item> Parents { get; set; } = new();
            public float[] TransformationMatrix { get; set; } = Array.Empty<float>();
            public Dictionary<string, object> Attributes { get; set; } = new();
        }

        const string externalIdAttributeName = "ExternalId";

        [GeneratedRegex(@"<[0-9A-Za-z\s]+?/>\s*", RegexOptions.Compiled)]
        private static partial Regex BlankElementRegex();
        private static readonly Regex blankElementsRegex = BlankElementRegex();

        public static JTNode Convert(Stream xmlFileInputStream, XML2JTConfiguration configuration)
        {
            var excludedNodes = new HashSet<string>
            {
                "Human",
                "PmAttachment",
                "PmImage",
                "PmVariantSet",
                "ScoreableOperation"
            };

            var requiredForProduct = new HashSet<string>
            {
                "CompoundProcess",
                "HumanOperation",
                "PmCompoundOperation",
                "PmCompoundPart",
                "PmOperation",
                "PmPartInstance",
                "PmPartPrototype",
                "PmPartPrototype",
                "PrLineProcess",
                "PrPlantProcess",
                "PrStationProcess",
                "PrZoneProcess"
            };

            var requiredForResources = new HashSet<string>
            {
                "PmCompoundResource",
                "PmPartPrototypeUsage",
                "PmResourcePlaceholder",
                "PmToolInstance",
                "PmToolPrototype",
                "PrLine",
                "PrPlant",
                "PrStation",
                "PrZone",
                "Station_Geometry"
            };

            if (!configuration.IncludeProduct) excludedNodes.UnionWith(requiredForProduct);
            if (!configuration.IncludeResource) excludedNodes.UnionWith(requiredForResources);

            ConcurrentDictionary<string, Item> items = new();
            ConcurrentDictionary<string, XElement> supportingObjects = new();

            Parallel.ForEach(GetXMLElements(xmlFileInputStream, excludedNodes), data =>
            {
                var outerXML = data.outerXML;
                var elementName = data.elementName;
                var externalId = data.externalId;

                var isProduct = requiredForProduct.Contains(elementName);
                var isResource = requiredForResources.Contains(elementName);

                var element = XElement.Parse(outerXML.Replace(blankElementsRegex, ""));

                if (elementName != "PmSource" && elementName != "PmLayout" && !elementName.EndsWith("Prototype") && (isProduct || isResource))
                    items.TryAdd(externalId, new Item
                    {
                        ExternalId = externalId,
                        Type = isProduct ? ContentType.Product : ContentType.Resource,
                        Title = GetTitle(element.Element("number")?.Value ?? string.Empty, element.Element("name")?.Value ?? string.Empty),
                        // Children,
                        //FilePath = string.Empty,
                        FileIsPart = isProduct,
                        // Parent,
                        //TransformationMatrix = Array.Empty<float>(),
                        //Attributes = new Dictionary<string, object>()
                    });

                supportingObjects.TryAdd(externalId, element);
            });

            Parallel.ForEach(items, externalIdItemPair =>
            {
                var itemExternalId = externalIdItemPair.Key;
                var item = externalIdItemPair.Value;

                var itemElement = supportingObjects[itemExternalId];

                var childrenItemElements = itemElement.Element("children")?.Elements("item") ?? Enumerable.Empty<XElement>();

                var partsItemElements = itemElement.Element("inputFlows")?.Elements("item")
                    .SelectMany(element => supportingObjects.TryGetValue(element.Value, out var supportingObject) ? supportingObject.Yield() : Enumerable.Empty<XElement>())
                    .Elements("parts").Elements("item") ?? Enumerable.Empty<XElement>();

                item.Children = childrenItemElements.Concat(partsItemElements)
                    .SelectMany(element => items.TryGetValue(element.Value, out var childItem) ? childItem.Yield() : Enumerable.Empty<Item>())
                    .Select(childItem =>
                    {
                        childItem.Parents.Add(item);

                        return childItem;
                    })
                    .ToArray();

                var tceRevision = string.Empty;
                var activeInCurrentVersion = string.Empty;

                var prototypeExternalId = itemElement.Element("prototype")?.Value;
                if (prototypeExternalId != null)
                {
                    if (supportingObjects.TryGetValue(prototypeExternalId, out var prototypeElement))
                    {
                        item.Title = GetTitle(
                            prototypeElement.Element("catalogNumber")?.Value ?? string.Empty,
                            prototypeElement.Element("name")?.Value ?? string.Empty
                        );

                        var threeDRepExternalId = prototypeElement.Element("threeDRep")?.Value;
                        if (threeDRepExternalId != null && supportingObjects.TryGetValue(threeDRepExternalId, out var threeDRepElement))
                        {
                            var fileExternalId = threeDRepElement.Element("file")?.Value;
                            if (fileExternalId != null && supportingObjects.TryGetValue(fileExternalId, out var fileElement))
                            {
                                var fileName = fileElement.Element("fileName")?.Value;
                                if (fileName != null)
                                    item.FilePath = GetFullFilePath(configuration.SysRootPath, fileName);
                            }
                        }

                        if (item.Type == ContentType.Resource && configuration.ResourceSysRootJTFilesAreAssemblies && !item.FilePath.StartsWith(configuration.SysRootPath))
                            item.FileIsPart = true;

                        var tceRevisionValue = prototypeElement.Element("TCe_Revision")?.Value;
                        if (tceRevisionValue != null)
                            tceRevision = tceRevisionValue;

                        var activeInCurrentVersionValue = prototypeElement.Element("ActiveInCurrentVersion")?.Value;
                        if (activeInCurrentVersionValue != null)
                            activeInCurrentVersion = $"{activeInCurrentVersionValue} (prototype)";

                    }

                    var layoutExternalId = itemElement.Element("layout")?.Value;
                    if (layoutExternalId != null && supportingObjects.TryGetValue(layoutExternalId, out var layoutElement))
                    {
                        var absoluteLocationElement = layoutElement.Element("NodeInfo")?.Element("absoluteLocation");
                        if (absoluteLocationElement != null)
                        {
                            var x = float.TryParse(absoluteLocationElement.Element("x")?.Value, out var xValue) ? xValue : 0;
                            var y = float.TryParse(absoluteLocationElement.Element("y")?.Value, out var yValue) ? yValue : 0;
                            var z = float.TryParse(absoluteLocationElement.Element("z")?.Value, out var zValue) ? zValue : 0;

                            var rx = float.TryParse(absoluteLocationElement.Element("rx")?.Value, out var rxValue) ? rxValue : 0;
                            var ry = float.TryParse(absoluteLocationElement.Element("ry")?.Value, out var ryValue) ? ryValue : 0;
                            var rz = float.TryParse(absoluteLocationElement.Element("rz")?.Value, out var rzValue) ? rzValue : 0;

                            if (x != 0 || y != 0 || z != 0 || rx != 0 || ry != 0 || rz != 0)
                                item.TransformationMatrix = EulerZYXAndPosition2Matrix(x, y, z, rx, ry, rz);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(item.FilePath)) item.Attributes.Add("3D file", item.FilePath);

                var statusElement = itemElement.Element("NodeInfo")?.Element("status");

                var createdBy = statusElement?.Element("createdBy")?.Value;
                if (!string.IsNullOrEmpty(createdBy)) item.Attributes.Add("Created by", createdBy);

                var lastModifiedBy = statusElement?.Element("lastModifiedBy")?.Value;
                if (!string.IsNullOrEmpty(lastModifiedBy)) item.Attributes.Add("Last modified by", lastModifiedBy);

                var modificationDate = statusElement?.Element("modificationDate")?.Value;
                if (!string.IsNullOrEmpty(modificationDate)) item.Attributes.Add("Modification date", modificationDate);

                if (string.IsNullOrEmpty(tceRevision)) tceRevision = itemElement.Element("TCe_Revision")?.Value;
                if (!string.IsNullOrEmpty(tceRevision)) item.Attributes.Add("TCe_Revision", tceRevision);

                var comment = itemElement.Element("comment")?.Value;
                if (!string.IsNullOrEmpty(comment)) item.Attributes.Add("Comment", comment);

                var comment1 = itemElement.Element("Comment1")?.Value;
                if (!string.IsNullOrEmpty(comment1)) item.Attributes.Add("Comment1", comment1);

                var comment2 = itemElement.Element("Comment2")?.Value;
                if (!string.IsNullOrEmpty(comment2)) item.Attributes.Add("Comment2", comment2);

                activeInCurrentVersion = itemElement.Element("ActiveInCurrentVersion")?.Value ?? activeInCurrentVersion;
                if (!string.IsNullOrEmpty(activeInCurrentVersion)) item.Attributes.Add("ActiveInCurrentVersion", activeInCurrentVersion);

                foreach (var keyValuePair in configuration.AdditionalAttributes)
                {
                    item.Attributes.TryAdd(keyValuePair.Key, keyValuePair.Value);
                }
            });

            Parallel.ForEach(supportingObjects.Values, element =>
            {
                var partExternalIds = element.Element("outputFlows")?.Elements("item")
                    .Select(outputFlowItemElement => supportingObjects.TryGetValue(outputFlowItemElement.Value, out var pmFlowElement) ? pmFlowElement : null)
                    .Select(pmFlowElement => pmFlowElement?.Element("parts")?.Elements("item"))
                    .SelectMany(partsItemElements => partsItemElements?.Select(partsItemElement => partsItemElement.Value) ?? Enumerable.Empty<string>());

                if (partExternalIds == null) return;

                foreach (var partExternalId in partExternalIds)
                {
                    if (!items.TryGetValue(partExternalId, out var partItem)) continue;

                    if (!partItem.Parents.IsEmpty) continue;

                    items.TryRemove(partExternalId, out var _);
                }
            });

            if (!configuration.IncludeBranchesWithoutCAD)
            {
                var emptyItems = items.Values.Where(item => item.FilePath.Length == 0 && item.Children.Length == 0);

                foreach (var item in emptyItems)
                {
                    DeleteEmptyItem(item, items);
                }
            }

            var rootItem = new Item
            {
                ExternalId = $"{DateTime.UtcNow}-root",
                Title = configuration.RootName,
                FileIsPart = false,
                Type = ContentType.Product
            };

            rootItem.Children = items.Values.Where(item => item.Parents.IsEmpty).Select(item =>
            {
                item.Parents.Add(rootItem);

                if (item.Type == ContentType.Resource) item.Title = $"Resource: {item.Title}";

                return item;
            }).ToArray();

            items.TryAdd(rootItem.ExternalId, rootItem);

            var jtNodesByItem = new ConcurrentDictionary<Item, JTNode>();

            Parallel.ForEach(items.Values, item =>
            {
                var jtNode = new JTNode()
                {
                    Name = item.Title,
                    TransformationMatrix = item.TransformationMatrix,
                    ReferencedFile = item.FilePath,
                    ReferencedFileIsPart = item.FileIsPart,
                    Attributes = item.Attributes
                };

                jtNodesByItem.TryAdd(item, jtNode);
            });

            Parallel.ForEach(jtNodesByItem, itemJTNode =>
            {
                var item = itemJTNode.Key;
                var jtNode = itemJTNode.Value;

                jtNode.Children = item.Children.Select(childItem => jtNodesByItem[childItem]).ToList();
            });

            return jtNodesByItem[rootItem];
        }

        static string GetTitle(string number, string name)
        {
            return string.Join(" - ", new[] { number, name }
                .Where(value => !string.IsNullOrEmpty(value))
                .Select(value => value.StartsWith("\"") && value.EndsWith("\"") ? value[1..^1] : value));
        }

        [GeneratedRegex("(?<!^)\\{2,}", RegexOptions.Compiled)]
        private static partial Regex DoubleBackSlashRegex();
        private static readonly Regex doubleBackSlashRegex = DoubleBackSlashRegex();

        [GeneratedRegex("/", RegexOptions.Compiled)]
        private static partial Regex ForwardSlashRegex();
        private static readonly Regex forwardSlashRegex = ForwardSlashRegex();

        static string GetFullFilePath(string sysRootPath, string filePath)
        {
            var fullFilePath = string.Empty;

            if (!string.IsNullOrEmpty(filePath))
            {
                if (filePath.StartsWith("\"") && filePath.EndsWith("\"")) { filePath = filePath[1..^1]; }

                fullFilePath = filePath.Replace("#", sysRootPath).Replace(forwardSlashRegex, "\\").Replace(doubleBackSlashRegex, "\\");

                if (Path.GetExtension(fullFilePath).ToLower() == ".cojt")
                {
                    fullFilePath = Path.Combine(fullFilePath, Path.GetFileNameWithoutExtension(fullFilePath) + ".jt");
                }
            }

            return fullFilePath;
        }

        static float[] EulerZYXAndPosition2Matrix(float x, float y, float z, float rx, float ry, float rz)
        {
            // https://github.com/mrdoob/three.js/blob/124e7cc8d9a0a73e07b536d18e6779d702a6bf29/src/math/Matrix4.js#L9

            var matrixElements = new float[]
            {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            };


            // https://github.com/mrdoob/three.js/blob/124e7cc8d9a0a73e07b536d18e6779d702a6bf29/src/math/Matrix4.js#L159

            var a = MathF.Cos(rx);
            var b = MathF.Sin(rx);
            var c = MathF.Cos(ry);
            var d = MathF.Sin(ry);
            var e = MathF.Cos(rz);
            var f = MathF.Sin(rz);


            // https://github.com/mrdoob/three.js/blob/124e7cc8d9a0a73e07b536d18e6779d702a6bf29/src/math/Matrix4.js#L214

            var ae = a * e;
            var af = a * f;
            var be = b * e;
            var bf = b * f;

            matrixElements[0] = c * e;
            matrixElements[4] = be * d - af;
            matrixElements[8] = ae * d + bf;

            matrixElements[1] = c * f;
            matrixElements[5] = bf * d + ae;
            matrixElements[9] = af * d - be;

            matrixElements[2] = -d;
            matrixElements[6] = b * c;
            matrixElements[10] = a * c;


            // https://github.com/mrdoob/three.js/blob/124e7cc8d9a0a73e07b536d18e6779d702a6bf29/src/math/Matrix4.js#L474

            matrixElements[12] = x;
            matrixElements[13] = y;
            matrixElements[14] = z;

            return matrixElements;
        }

        static IEnumerable<(string outerXML, string elementName, string externalId)> GetXMLElements(Stream xmlFileInputStream, HashSet<string>? excludedElements = null)
        {
            using var xmlReader = XmlReader.Create(xmlFileInputStream, new XmlReaderSettings
            {
                CloseInput = false
            });

            var itemsOuterXMLs = new List<string>();
            var supportingObjectsOuterXMLs = new List<string>();

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType != XmlNodeType.Element) continue;

                var elementName = xmlReader.Name;

                if (excludedElements?.Contains(elementName) ?? false) continue;

                if (!xmlReader.HasAttributes) continue;

                if (!xmlReader.MoveToAttribute(externalIdAttributeName)) continue;

                var externalId = xmlReader.Value;

                if (!xmlReader.MoveToElement()) continue;

                using var subtreeReader = xmlReader.ReadSubtree();
                subtreeReader.Read();
                var outerXML = subtreeReader.ReadOuterXml();

                yield return (outerXML, elementName, externalId);
            }
        }

        static void DeleteEmptyItem(Item item, IDictionary<string, Item> items)
        {
            if (item.FilePath.Length > 0 || item.Children.Length > 0) return;

            items.Remove(item.ExternalId);

            var parents = item.Parents;

            if (parents.IsEmpty) return;

            foreach (var parent in parents)
            {
                parent.Children = parent.Children.Where(childItem => childItem != item).ToArray();

                DeleteEmptyItem(parent, items);
            }
        }
    }
}
