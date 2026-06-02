using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Path
{
    public struct PathSegment<T> where T : new()
    {
        public T p0 = new T();
        public T p1 = new T();
        public bool laserOn;

        public PathSegment() { }

        public PathSegment(T p0, T p1, bool laserOn)
        {
            this.p0 = p0;
            this.p1 = p1;
            this.laserOn = laserOn;
        }

        public PathSegment<T> GetReversed()
        {
            return new PathSegment<T>(this.p1, this.p0, this.laserOn);
        }

        public override string ToString()
        {
            return $"P0={p0.ToString()} P1={p1.ToString()}";
        }
    }
}
