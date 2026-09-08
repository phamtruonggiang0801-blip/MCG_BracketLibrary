using System;
using System.Collections.Generic;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>
    /// PORT của <c>ProfileService.BracketOutline</c> (repo MCG_3DPanel) — sinh biên dạng knee cho
    /// OB/OB1 · IB/IB1 · FB/FB1. 2 chế độ toe:
    ///  - FLANGED: R30 (lõm) → 18mm → Rb → đoạn dày(⊥) → bend. Rb = 2·thk, đoạn dày = thk.
    ///  - KHÔNG flanged: chỉ R30 (2 cung tiếp tuyến chung 1 free-edge thẳng) — có nhánh squareCorner + under50.
    ///
    /// Giữ NGUYÊN thuật toán + hằng số (R30 = 30 cố định, đoạn 18mm) để khớp block explode OB-OB1.dwg.
    /// Nếu sửa công thức: sửa ĐỒNG THỜI ở cả ProfileService.cs bên repo AutoCAD (2 nguồn phải khớp).
    /// </summary>
    public sealed class KneeProfileGenerator : IBracketProfileGenerator
    {
        private const double ToeR = 30.0;      // bán kính toe-end R30 (cố định mọi chiều dày)
        private const double FlangeSeg = 18.0; // đoạn thẳng R30 → Rb (∥ free-edge)

        public bool Supports(BracketFamily family) =>
            family == BracketFamily.OuterBracket ||
            family == BracketFamily.InnerBracket ||
            family == BracketFamily.FlatbarBracket;

        public Profile2D Generate(KneeSolution k)
        {
            List<ProfileVertex> verts;
            FreeEdge fe = null;

            if (!k.Flanged)
                verts = R30Only(k.S, k.Huse, k.F, k.Dtop, k.Ledge, k.SquareCorner, k.Under50, out fe);
            else if (k.Under50)
                verts = TemplateUnder50(k.S, k.Huse, k.F);
            else
                verts = FlangedToe(k.S, k.Huse, k.F, k.Thk, k.Dtop, k.Ledge, out fe);

            var p = new Profile2D { FreeEdge = fe };
            foreach (var v in verts) p.Add(v);
            return p;
        }

        // ─── TopSpan: (0,0)→(S,0)→(S,−Dtop) [nếu IB có ledge] ───
        private static void TopSpan(List<ProfileVertex> o, double S, double dtop, double ledge)
        {
            o.Add(new ProfileVertex(0, 0, 0));
            o.Add(new ProfileVertex(S, 0, 0));
            if (ledge > 0.0) o.Add(new ProfileVertex(S, -dtop, 0));
        }

        // ─── FLANGED over-50 (parametric, scale thk + Dtop/ledge) ───
        private static List<ProfileVertex> FlangedToe(
            double S, double H, double F, double thk, double dtop, double ledge, out FreeEdge fe)
        {
            fe = null;
            double R = ToeR, Rb = 2.0 * thk, seg = FlangeSeg;
            var cR30T = new Vec2(S - ledge, -dtop - R);
            var cR30B = new Vec2(F + R, -H);

            Vec2 ctr = cR30B - cR30T;
            if (ctr.Length < 2.0 * R + 2.0 * Rb + seg + thk)
                return R30Only(S, H, F, dtop, ledge, false, false, out fe);

            Vec2 u = ctr.Normal();
            var n = new Vec2(u.Y, -u.X);
            if ((new Vec2(0, 0) - cR30T).Dot(n) < 0.0) n = n.Negate();

            var legT = new Vec2(S - ledge, -dtop);
            var legB = new Vec2(F, -H);
            Vec2 tR30T = cR30T + n * R, tR30B = cR30B + n * R;
            Vec2 p1T = tR30T + u * seg, p1B = tR30B - u * seg;
            Vec2 cRbT = p1T - n * Rb, cRbB = p1B - n * Rb;
            Vec2 p2T = cRbT + u * Rb, p2B = cRbB - u * Rb;
            Vec2 p3T = p2T - n * thk, p3B = p2B - n * thk;

            double bR30T = ArcMath.Bulge(cR30T, legT, tR30T);
            double bRbT = ArcMath.Bulge(cRbT, p1T, p2T);
            double bRbB = ArcMath.Bulge(cRbB, p2B, p1B);
            double bR30B = ArcMath.Bulge(cR30B, tR30B, legB);

            var o = new List<ProfileVertex>(14);
            TopSpan(o, S, dtop, ledge);
            o.Add(new ProfileVertex(legT.X, legT.Y, bR30T));
            o.Add(new ProfileVertex(tR30T.X, tR30T.Y, 0));
            o.Add(new ProfileVertex(p1T.X, p1T.Y, bRbT));
            o.Add(new ProfileVertex(p2T.X, p2T.Y, 0));
            o.Add(new ProfileVertex(p3T.X, p3T.Y, 0));
            o.Add(new ProfileVertex(p3B.X, p3B.Y, 0));
            o.Add(new ProfileVertex(p2B.X, p2B.Y, bRbB));
            o.Add(new ProfileVertex(p1B.X, p1B.Y, 0));
            o.Add(new ProfileVertex(tR30B.X, tR30B.Y, bR30B));
            o.Add(new ProfileVertex(legB.X, legB.Y, 0));
            o.Add(new ProfileVertex(0, -H, 0));

            fe = new FreeEdge { Ax = p3T.X, Ay = p3T.Y, Bx = p3B.X, By = p3B.Y };
            return o;
        }

        // ─── KHÔNG flanged: chỉ R30 ───
        private static List<ProfileVertex> R30Only(
            double S, double H, double F, double dtop, double ledge,
            bool squareCorner, bool under50, out FreeEdge fe)
        {
            fe = null;
            double R = ToeR;
            var cTop = new Vec2(S - ledge, -dtop - R);
            var tLegTop = new Vec2(S - ledge, -dtop);

            if (squareCorner)
            {
                var tFreeSq = new Vec2(cTop.X - R, cTop.Y);
                double bTopSq = ArcMath.Bulge(cTop, tLegTop, tFreeSq);
                var osq = new List<ProfileVertex>(7);
                TopSpan(osq, S, dtop, ledge);
                osq.Add(new ProfileVertex(tLegTop.X, tLegTop.Y, bTopSq));
                osq.Add(new ProfileVertex(tFreeSq.X, tFreeSq.Y, 0));
                osq.Add(new ProfileVertex(tFreeSq.X, -H, 0));
                osq.Add(new ProfileVertex(0, -H, 0));
                fe = new FreeEdge { Ax = tFreeSq.X, Ay = tFreeSq.Y, Bx = tFreeSq.X, By = -H };
                return osq;
            }

            if (under50)
            {
                var legBot = new Vec2(F, -H);
                Vec2 refCtr = new Vec2(F + R, -H) - cTop;
                Vec2 refN = refCtr.Length > 1e-9 ? new Vec2(refCtr.Y, -refCtr.X).Normal() : new Vec2(1, 0);
                if ((new Vec2(0, 0) - cTop).Dot(refN) < 0.0) refN = refN.Negate();

                Vec2 tFree = ArcMath.TangentPointFromExternal(cTop, R, legBot, refN);
                double bTop = ArcMath.Bulge(cTop, tLegTop, tFree);

                var o50 = new List<ProfileVertex>(7);
                TopSpan(o50, S, dtop, ledge);
                o50.Add(new ProfileVertex(tLegTop.X, tLegTop.Y, bTop));
                o50.Add(new ProfileVertex(tFree.X, tFree.Y, 0));
                o50.Add(new ProfileVertex(legBot.X, legBot.Y, 0));
                o50.Add(new ProfileVertex(0, -H, 0));
                fe = new FreeEdge { Ax = tFree.X, Ay = tFree.Y, Bx = legBot.X, By = legBot.Y };
                return o50;
            }

            var cBot = new Vec2(F + R, -H);
            Vec2 c2 = cBot - cTop;
            if (c2.Length < 2.0 * R + 1.0)
                return SharpOutline(S, H, F, dtop, ledge);

            Vec2 u = c2.Normal();
            var n = new Vec2(u.Y, -u.X);
            if ((new Vec2(0, 0) - cTop).Dot(n) < 0.0) n = n.Negate();

            Vec2 tFreeTop = cTop + n * R, tFreeBot = cBot + n * R;
            var tLegBot = new Vec2(F, -H);
            double bT = ArcMath.Bulge(cTop, tLegTop, tFreeTop);
            double bB = ArcMath.Bulge(cBot, tFreeBot, tLegBot);

            var o = new List<ProfileVertex>(8);
            TopSpan(o, S, dtop, ledge);
            o.Add(new ProfileVertex(tLegTop.X, tLegTop.Y, bT));
            o.Add(new ProfileVertex(tFreeTop.X, tFreeTop.Y, 0));
            o.Add(new ProfileVertex(tFreeBot.X, tFreeBot.Y, bB));
            o.Add(new ProfileVertex(tLegBot.X, tLegBot.Y, 0));
            o.Add(new ProfileVertex(0, -H, 0));
            fe = new FreeEdge { Ax = tFreeTop.X, Ay = tFreeTop.Y, Bx = tFreeBot.X, By = tFreeBot.Y };
            return o;
        }

        // ─── UNDER-50 flanged (hiếm): template block OB_type2 (thk6) + giãn leg ───
        private static readonly double[][] UNDER50 =
        {
            new[] {  15.0,   -344.0,    0.0    }, new[] {  31.385, -336.548, -0.414  },
            new[] {  47.276, -342.503,  0.0    }, new[] {  49.76,  -347.965,  0.0    },
            new[] { 514.724, -136.497,  0.0    }, new[] { 512.24,  -131.035, -0.414  },
            new[] { 518.195, -115.144,  0.0    }, new[] { 534.58,  -107.692, -0.107  },
            new[] { 547.0,   -105.0,    0.0    }, new[] { 547.0,      0.0,    0.0    },
            new[] {   0.0,      0.0,    0.0    }, new[] {   0.0,   -344.0,    0.0    },
        };

        private static List<ProfileVertex> TemplateUnder50(double S, double H, double F)
        {
            const double refS = 547.0, refH = 344.0, refF = 15.0;
            double dS = S - refS, dH = H - refH, dF = F - refF;
            var o = new List<ProfileVertex>(UNDER50.Length);
            foreach (var v in UNDER50)
            {
                double x = v[0], y = v[1];
                if (y > -refH / 2.0) { if (x > refS / 2.0) x += dS; }
                else { y -= dH; if (x > 50.0) x += dF; }
                o.Add(new ProfileVertex(x, y, v[2]));
            }
            return o;
        }

        private static List<ProfileVertex> SharpOutline(double S, double H, double F, double dtop, double ledge)
        {
            return new List<ProfileVertex>(6)
            {
                new ProfileVertex(0, 0, 0),
                new ProfileVertex(S, 0, 0),
                new ProfileVertex(S, -dtop, 0),
                new ProfileVertex(S - ledge, -dtop, 0),
                new ProfileVertex(F, -H, 0),
                new ProfileVertex(0, -H, 0),
            };
        }
    }
}
