using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Point2D = Microsoft.Msagl.Core.Geometry.Point;
using WinFormsApp.DataAccess;
using WinFormsApp.Entities;
using System.Drawing.Drawing2D;

namespace WinFormsApp
{
    public partial class Form1 : Form
    {
        readonly ToolTip toolTip = new();
        object selectedObject;
        AttributeBase selectedObjectAttr;
        GViewer graphView = new();

        public Form1()
        {
            InitializeComponent();
            SuspendLayout();
            this.Controls.Add(graphView);
            graphView.Dock = DockStyle.Fill;
            ResumeLayout();
            graphView.LayoutAlgorithmSettingsButtonVisible = false;
            graphView.AsyncLayout = true;
            toolTip.Active = true;
            toolTip.AutoPopDelay = 2500;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 300;
            InitGraph();
        }

        ICurve GetNodeBoundary(Node node)
        {
            int widthOffset = node.LabelText.Split('\n').Max(substring => substring.Length) * 17;
            int heightOffset = node.LabelText.Split('\n').Length * 40;

            return CurveFactory.CreateRectangle(widthOffset, heightOffset, new Point2D());
        }

        bool DrawNode(Node node, object graphics)
        {
            Graphics g = (Graphics)graphics;
            using Matrix m = g.Transform;
            using Matrix saveM = m.Clone();
            using (var m2 = new Matrix(1, 0, 0, -1, 0, 2 * (float)node.GeometryNode.Center.Y)) m.Multiply(m2);

            g.Transform = m;
            var x = (int)((int)node.GeometryNode.Center.X - node.GeometryNode.Width / 2);
            var y = (int)((int)node.GeometryNode.Center.Y - node.GeometryNode.Height / 2);

            SolidBrush darkBlueBrush = new(System.Drawing.Color.DarkBlue);
            SolidBrush biegeBrush = new(System.Drawing.Color.Cornsilk);
            var font = new Font("Arial", 20);

            int widthOffset = (node.LabelText.Split('\n').Max(substring => substring.Length) - 10) * 10;
            var tableName = new Rectangle(x, y, (int)node.GeometryNode.Width, (int)node.GeometryNode.Height);
            var keys = new Rectangle(x, y, (int)node.GeometryNode.Width, 30);

            g.FillRectangle(biegeBrush, tableName);
            g.FillRectangle(darkBlueBrush, keys);

            g.DrawString(node.LabelText.Split('\n')[0], font, biegeBrush, keys);
            string str = "\n";
            for (int i = 1; i < node.LabelText.Split('\n').Length; i++)
            {
                str += node.LabelText.Split('\n')[i] + "\n";
                Console.WriteLine(i);
            }
            g.DrawString(str, font, darkBlueBrush, tableName);

            g.Transform = saveM;
            g.ResetClip();

            return true;
        }

        void GViewerObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e)
        {
            selectedObject = e.OldObject != null ? e.OldObject.DrawingObject : null;

            if (selectedObject != null)
            {
                RestoreSelectedObjAttr();
                graphView.Invalidate(e.OldObject);
                selectedObject = null;
            }

            if (graphView.ObjectUnderMouseCursor == null)
            {
                graphView.SetToolTip(toolTip, "");
            }
            else
            {
                selectedObject = graphView.ObjectUnderMouseCursor.DrawingObject;
                if (selectedObject is Edge edge)
                {
                    selectedObjectAttr = edge.Attr.Clone();
                    edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Blue;
                    graphView.Invalidate(e.NewObject);
                    graphView.SetToolTip(toolTip, string.Format("Each {0} may have multiple {1}", edge.Source, edge.Target));
                }
                else if (selectedObject is Node)
                {
                    selectedObjectAttr = (selectedObject as Node).Attr.Clone();
                    (selectedObject as Node).Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
                    graphView.SetToolTip(toolTip,
                                       string.Format("Table {0}",
                                                     (selectedObject as Node).Attr.Id));
                    graphView.Invalidate(e.NewObject);
                }
            }
        }
        void RestoreSelectedObjAttr()
        {
            if (selectedObject is Edge edge && selectedObjectAttr is EdgeAttr atr)
            {
                edge.Attr = atr;
            }
            else
            {
                if (selectedObject is Node node && selectedObjectAttr is NodeAttr attr)
                    node.Attr = attr;
            }
        }

        static internal PointF PointF(Point2D p) { return new PointF((float)p.X, (float)p.Y); }

        private void InitGraph()
        {
            Graph graph = new("Database Relationships");

            //PUT YOUR CONNECTION STRING HERE!
            string connectionString = "Data Source=localhost;Initial Catalog=AdventureWorks2022;Integrated Security=True";

            SqlDatabaseSchemaReader schemaReader = new();
            TableExtractor analyzer = new(schemaReader);

            List<Table> tables = analyzer.GetTables(connectionString);

            foreach (Table table in tables)
            {
                Node node = new(table.TableName)
                {
                    LabelText = table.TableName + "\nPK:" + table.PrimaryKey
                };
                node.Attr.Shape = Shape.Box;
                graph.AddNode(node);
            }

            foreach (Table table in tables)
            {
                if (table.ForeignKeys.Count > 0)
                {
                    foreach (Tuple<string, Table> foreignKey in table.ForeignKeys)
                    {
                        graph.FindNode(foreignKey.Item2.TableName).LabelText += "\nFK: " + foreignKey.Item1;
                        graph.AddEdge(table.TableName, foreignKey.Item2.TableName).Attr.ArrowheadAtTarget = ArrowStyle.ODiamond;
                    }
                }
            }

            foreach (var node in graph.Nodes)
            {
                node.Label.FontColor = Microsoft.Msagl.Drawing.Color.Navy;
                node.Attr.Shape = Shape.DrawFromGeometry;
                node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Beige;
                node.DrawNodeDelegate = new DelegateToOverrideNodeRendering(DrawNode);
                node.NodeBoundaryDelegate = new DelegateToSetNodeBoundary(GetNodeBoundary);
            }
            double width = 150;
            double height = 150;

            graph.Attr.LayerSeparation = 500;
            graph.Attr.NodeSeparation = 500;
            double arrowHeadLenght = width / 10;
            foreach (Edge e in graph.Edges)
                e.Attr.ArrowheadLength = (float)arrowHeadLenght;
            graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings();
            graphView.Graph = graph;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            graphView.ObjectUnderMouseCursorChanged += GViewerObjectUnderMouseCursorChanged;
        }
    }
}
