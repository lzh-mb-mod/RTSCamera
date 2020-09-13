using DBSCAN;
using KdTree;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class KdTreeSpatialIndex: ISpatialIndex<PointInfo<AgentPointInfo>>
    {
        public KdTree<float, PointInfo<AgentPointInfo>> Tree { get; }

        public KdTreeSpatialIndex(KdTree<float, PointInfo<AgentPointInfo>> tree)
        {
            Tree = tree;
        }
        public IReadOnlyList<PointInfo<AgentPointInfo>> Search()
        {
            return Tree.Select(node => node.Value).ToList().AsReadOnly();
        }

        public IReadOnlyList<PointInfo<AgentPointInfo>> Search(in Point p, double epsilon)
        {
            var result = Tree.RadialSearch(new float[2] {(float) p.X, (float) p.Y}, (float) epsilon)
                .Select(node => node.Value).ToList().AsReadOnly();
            return result;
        }
    }
}
