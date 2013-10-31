using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//load and represent a GraphML file
//this class is basically a LINQ-to-Objects wrapper around a deserialized XSD.exe data contract class
//for GraphML file format, see: http://graphml.graphdrawing.org/primer/graphml-primer.html
public class Graph
{
    //root document element: contains a graph
    private YGraphML.graphmltype graphml;

    //main graph element: contains nodes and edges
    public YGraphML.graphtype GraphElement
    {
        get
        {
            if (graphml == null) return new YGraphML.graphtype();
            return graphml.Items.OfType<YGraphML.graphtype>().FirstOrDefault();
        }
    }

    //get all labelled nodes in the graph
    public IList<YGraphML.nodetype> Nodes
    {
        get
        {
            return GraphElement.Items.OfType<YGraphML.nodetype>()
                .Where(n => !string.IsNullOrWhiteSpace(n.GetLabelText())).ToList();
        }
    }

    //get the node with the given id
    public YGraphML.nodetype GetNodeById(string id)
    {
        return Nodes.Where(n => n.id == id).FirstOrDefault();
    }

    //get all labelled edges in the graph
    public IList<YGraphML.edgetype> Edges
    {
        get
        {
            return GraphElement.Items.OfType<YGraphML.edgetype>().ToList();
            //    .Where(e => !string.IsNullOrWhiteSpace(e.GetLabelText())).ToList();
        }
    }

    //get the edge with the given id
    public YGraphML.edgetype GetEdgeById(string id)
    {
        return Edges.Where(e => e.id == id).FirstOrDefault();
    }

    //get the edge going from node1 to node2 (by ids)
    public YGraphML.edgetype GetEdgeBetween(string node1, string node2)
    {
        return Edges.Where(e => e.source == node1 && e.target == node2).FirstOrDefault();
    }

    //ctor: deserialize the given GraphML file into data contract objects
    public Graph(string graphMLFile)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(graphMLFile);

        XmlSerializer serializer = new XmlSerializer(typeof(YGraphML.graphmltype));
        serializer.UnknownElement += new XmlElementEventHandler(serializer_UnknownElement);

        using (StringReader reader = new StringReader(doc.OuterXml))
        {
            graphml = (YGraphML.graphmltype)serializer.Deserialize(reader);
        }
    }

    private void serializer_UnknownElement(object sender, XmlElementEventArgs e)
    {
        Console.WriteLine("Unknown Element");
        Console.WriteLine("\t" + e.Element.Name + " " + e.Element.OuterXml);
        Console.WriteLine("\t LineNumber: " + e.LineNumber);
        Console.WriteLine("\t LinePosition: " + e.LinePosition);
    }
}

public static class GraphMLExtensions
{
    //get a node's label text
    public static string GetLabelText(this YGraphML.nodetype node)
    {
        if (node.Items == null || node.Items.Length <= 0) return "";

        //flatten the node's descendants to find any /data/ShapeNode/NodeLabel elements that have text
        IList<YGraphML.NodeLabeltype> nodeLabels =
            node.Items.OfType<YGraphML.datatype>().Where(d => d.Items != null)
            .SelectMany(d => d.Items).OfType<YGraphML.ShapeNodetype>()
            .Where(s => s.NodeLabel != null)
            .SelectMany(s => s.NodeLabel)
            .Where(l => l.Text != null && l.Text.Length > 0)
            .ToList();

        if (nodeLabels.Count <= 0) return "";
        return string.Join("\n", nodeLabels.First().Text);
    }

    //get an edge's label text
    public static string GetLabelText(this YGraphML.edgetype edge)
    {
        if (edge.data == null || edge.data.Length <= 0) return "";

        //flatten the edges's descendants to find any /data/Edgetype/EdgeLabel elements that have text
        IList<YGraphML.EdgeLabeltype> edgeLabels =
            edge.data.Where(d => d.Items != null)
            .SelectMany(d => d.Items).OfType<YGraphML.Edgetype>()
            .Where(a => a.EdgeLabel != null)
            .SelectMany(a => a.EdgeLabel)
            .Where(l => l.Text != null && l.Text.Length > 0)
            .ToList();

        if (edgeLabels.Count <= 0) return "";
        return string.Join("\n", edgeLabels.First().Text);
    }
}